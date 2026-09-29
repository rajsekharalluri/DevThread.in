---
id: comparisons-events-rest
slug: events-rest
title: Event-Driven vs Request/Response Architecture
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "Distributed systems concepts, .NET 8+"
prerequisites: [architecture-event-driven, architecture-api-gateway-communication]
tags: [events, rest, synchronous, asynchronous, comparison]
relatedTopics: [architecture-outbox-inbox, architecture-resilience]
order: 110
status: published
---
# Event-Driven vs Request/Response Architecture

## Introduction

Every interaction between components answers one question: **does the caller need to wait for the result?**

- **Request/response** (HTTP, gRPC): the caller sends a request and blocks until it receives an answer. Simple to reason about, immediately consistent for the caller, but the caller is coupled to the callee's latency and availability.
- **Command queue**: the caller hands a unit of work to exactly one owner via a queue and continues. The work happens later with retries.
- **Event-driven**: a component publishes a fact ("OrderPaid") and any number of consumers react independently. The producer knows nothing about them.

Most real systems combine all three. The skill is choosing per interaction rather than per system.

## Quick Decision Table

| Concern | Request/Response | Command Queue | Event Stream / Pub-Sub |
|---|---|---|---|
| Caller waits? | Yes | No (gets an acknowledgement or job ID) | No |
| Receivers | One | One owner (competing workers) | Many independent consumers |
| Message meaning | "Do this and tell me the result" | "Do this eventually" | "This happened" |
| Consistency | Immediate for caller | Eventual | Eventual |
| Coupling | Time + availability | Low time coupling | Lowest; producer unaware of consumers |
| Failure handling | Timeouts, retries, circuit breakers | Retries, DLQ, idempotency | Retries, DLQ, idempotency, replay |
| Typical HTTP result | `200 OK` / `201 Created` | `202 Accepted` + status URL | `202` or `200` after local commit |
| Examples | Get balance, validate coupon | Generate PDF report, send email | `OrderPaid`, `CustomerRegistered` |

## Request/Response

### When to Use

- The caller needs the result to continue (authorize payment, check stock, show data)
- Operations are fast and bounded (well under typical timeouts)
- Queries and simple CRUD

### When Not to Use

- Long-running work (reports, video processing) that would hold connections open
- Fan-out to many downstream side effects that are not needed for the response
- Chains of synchronous calls where one slow service slows everyone

### Example with Resilience

```csharp
builder.Services.AddHttpClient<PricingClient>(c => c.BaseAddress = new Uri("https://pricing"))
    .AddStandardResilienceHandler(); // timeouts, retries with backoff, circuit breaker (Microsoft.Extensions.Http.Resilience)

app.MapGet("/quotes/{sku}", async (string sku, PricingClient pricing, CancellationToken ct) =>
    Results.Ok(await pricing.GetQuoteAsync(sku, ct)));
```

## Command Queue (Asynchronous Work, One Owner)

### When to Use

- Work that takes seconds to hours
- Work that must survive restarts and be retried
- Smoothing load spikes (queue absorbs bursts, workers drain steadily)

### Example: 202 Accepted + Status Endpoint

```csharp
app.MapPost("/reports", async (ReportRequest req, IReportJobs jobs, CancellationToken ct) =>
{
    var jobId = await jobs.EnqueueAsync(req, ct);           // persist job + enqueue durably
    return Results.Accepted($"/reports/{jobId}", new { jobId, status = "queued" });
});

app.MapGet("/reports/{jobId:guid}", async (Guid jobId, IReportJobs jobs, CancellationToken ct) =>
    await jobs.GetStatusAsync(jobId, ct) is { } status ? Results.Ok(status) : Results.NotFound());

public sealed class ReportWorker(IQueue queue, IReportJobs jobs) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var msg in queue.ReadAllAsync<GenerateReport>(stoppingToken))
        {
            if (await jobs.IsCompletedAsync(msg.JobId, stoppingToken)) { await msg.AckAsync(); continue; } // idempotent
            await jobs.RunAsync(msg.JobId, stoppingToken);
            await msg.AckAsync();
        }
    }
}
```

## Event-Driven

### When to Use

- Several parts of the system react to the same business fact
- Teams and services should evolve independently
- Building read models, search indexes, audit logs, analytics
- Integration between bounded contexts without direct dependencies

### When Not to Use

- The producer needs to know whether consumers succeeded
- The business process requires strict immediate consistency across components
- The organization lacks tooling for tracing, lag monitoring, and schema evolution

### Example: Synchronous Decision + Asynchronous Side Effects

```csharp
app.MapPost("/orders/{id:guid}/pay", async (Guid id, PayRequest req, OrderService orders, CancellationToken ct) =>
{
    // 1. Synchronous: the user needs to know whether payment succeeded
    var result = await orders.PayAsync(id, req.PaymentToken, req.IdempotencyKey, ct);
    if (!result.Succeeded) return Results.UnprocessableEntity(new { result.Reason });

    // 2. Inside PayAsync: order state + OrderPaid outbox record were committed in ONE transaction.
    // 3. Asynchronous: receipt email, loyalty points, analytics, and search consume OrderPaid later.
    return Results.Ok(new { status = "paid" });
});
```

## Same Scenario: Online Food Ordering

| Interaction | Style | Reason |
|---|---|---|
| Show menu and prices | Request/response (with caching) | Immediate read |
| Place order and charge card | Request/response | User must know the outcome |
| Restaurant receives the order | Event `OrderPlaced` | Restaurant system reacts independently |
| Assign a courier | Command queue to dispatch service | One owner, may take time and retries |
| Push notifications, analytics, loyalty | Events | Independent, non-blocking |
| Monthly statement PDF | Command queue + `202 Accepted` | Long-running |

## Decision Matrix

| Factor | Request/Response | Command Queue | Events |
|---|---|---|---|
| Simplicity | Strong | Medium | Lower |
| User needs immediate result | Strong | Weak | Weak |
| Resilience to downstream outages | Weak | Strong | Strong |
| Load leveling | Weak | Strong | Strong |
| Extensibility (add consumers) | Weak | Weak | Strong |
| Debuggability | Strong | Medium | Needs distributed tracing |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Waiting synchronously for emails, analytics, and search updates | Commit the core change, publish an event, return |
| Publishing events outside the database transaction | Transactional outbox |
| Assuming exactly-once delivery | Idempotent consumers with dedupe keys |
| Events that carry commands ("SendEmailNow") to one specific consumer | Use a command queue; events describe facts |
| Returning `200` for work that has not finished | `202 Accepted` with a status resource |
| No correlation IDs across async hops | Propagate trace context (W3C `traceparent`) in message headers |

## Interview Questions
- **[L1]** When is request/response the right choice?
- **[L1]** What is an event-driven reaction?
- **[L2]** What consistency trade-off does async introduce?
- **[L2]** Why are duplicate messages expected?
- **[L3]** How do you decide whether an API operation should return 200 or 202?
- **[L3]** How do you combine synchronous decisions with asynchronous side effects?

## Interview Answers
1. Request/response is right when the caller needs the result to proceed, such as reading data to display, validating input, authorizing a payment, or checking availability, and when the operation completes within a bounded latency budget. It is also the simplest model to build, test, and debug, so it is the default for queries and short commands.
2. An event-driven reaction is when a component publishes a fact that already happened, such as `OrderPaid`, and one or more independent consumers subscribe and react to it on their own schedule, for example sending a receipt, updating loyalty points, or refreshing a search index. The producer does not wait for, call, or even know about the consumers, which decouples them in time, availability, and deployment.
3. With asynchronous processing, the system is eventually consistent: after the caller's request completes, other components may not yet reflect the change, so a user might see "processing" or stale data for a while. The design must expose status to clients, define acceptable staleness, handle ordering and retries, provide reconciliation for failures, and sometimes use compensating actions instead of rolling back a single transaction.
4. Most brokers provide at-least-once delivery. A consumer can process a message successfully and then crash, time out, or lose its connection before acknowledging it; the broker then redelivers. Publishers can also retry sends after an uncertain failure, and outbox relays may republish after a crash. Therefore consumers must be idempotent, using unique message IDs or business keys, conditional updates, or a processed-messages table.
5. Return `200 OK` or `201 Created` when the requested state change has fully completed before the response is sent and the response reflects the final result. Return `202 Accepted` when the server has durably accepted the request but the work will complete asynchronously, and include a way to observe completion, such as a `Location` header to a status resource, a job ID, or a webhook callback. Never return `202` without persisting the work first, and never return `200` for work that has not happened.
6. Perform the decision that determines the user's response synchronously, such as validating and charging payment, and commit the resulting state change together with an outbox record of the event in the same local database transaction. A relay then publishes the event reliably, and independent consumers perform side effects like emails, analytics, and projections asynchronously with idempotent handling. Use idempotency keys on the synchronous API so client retries are safe, and use sagas when later asynchronous steps can fail and require compensation.
