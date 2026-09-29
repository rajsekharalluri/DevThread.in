---
id: comparisons-monolith-microservices
slug: monolith-microservices
title: Modular Monolith vs Microservices
category: comparisons
categoryTitle: Decision Guides
difficulty: senior
estimatedMinutes: 25
version:
  minimum: "Architecture pattern, .NET 8+ examples"
prerequisites: [architecture-modular-monolith, architecture-microservices]
tags: [architecture, monolith, microservices, comparison]
relatedTopics: [architecture-cqrs, architecture-event-driven]
order: 30
status: published
---
# Modular Monolith vs Microservices

## Introduction

The real choice is rarely "big ball of mud vs microservices". It is between a **modular monolith** - one deployable unit with strong internal module boundaries - and **microservices** - independently deployable services that own their data and communicate over the network.

Both aim for the same thing: **boundaries that let teams change parts of the system independently.** A modular monolith enforces boundaries in code and keeps the simplicity of one process, one deployment, and local transactions. Microservices enforce boundaries with process and data isolation, buying independent deployment and scaling at the cost of distributed-systems complexity.

## Quick Decision Table

| Concern | Modular Monolith | Microservices |
|---|---|---|
| Deployment | One unit | Many independent units |
| Communication | In-process calls, in-memory events | Network: HTTP/gRPC, messaging |
| Data | One database, schema-per-module ownership | Database per service |
| Transactions | Local ACID transactions | Sagas, outbox, eventual consistency |
| Latency | Nanoseconds per call | Milliseconds per hop, plus failure modes |
| Scaling | Scale the whole app (or run module-specific hosts) | Scale each service independently |
| Team autonomy | Shared release train | Independent releases per team |
| Operations | One pipeline, simpler observability | Service mesh/gateway, distributed tracing, many pipelines |
| Failure isolation | A crash or memory leak affects all modules | Failures can be contained per service |
| Best starting point | New product, uncertain domain, small-to-medium team | Proven boundaries, multiple teams, differing scale/compliance needs |

## Modular Monolith

### When to Use

- New products where domain boundaries are still being discovered
- One or a few teams (roughly under 30-50 engineers on the product)
- Strong consistency needs across business areas
- Limited platform or on-call capacity

### When Not to Use

- Several teams are blocked by a shared release train and coordination is the bottleneck
- One module has radically different scaling, availability, security, or compliance needs
- Different parts genuinely need different technology stacks

### Enforcing Boundaries in Code

```text
src/
  Orders/
    Orders.Contracts/       (public API: commands, queries, integration events)
    Orders.Core/            (internal domain + application logic)
    Orders.Infrastructure/  (EF Core DbContext with schema "orders")
  Payments/
    Payments.Contracts/
    Payments.Core/
    Payments.Infrastructure/  (schema "payments")
  Host/                     (composition root, single deployable)
```

```csharp
// Orders may depend only on Payments.Contracts, never on Payments.Core or its tables
public sealed class PlaceOrderHandler(IPaymentsModule payments, OrdersDbContext db)
{
    public async Task<Guid> Handle(PlaceOrder cmd, CancellationToken ct)
    {
        var order = Order.Place(cmd.CustomerId, cmd.Lines);
        var auth = await payments.AuthorizeAsync(new AuthorizePayment(order.Id, order.Total), ct); // in-process call
        if (!auth.Approved) throw new PaymentDeclinedException(auth.Reason);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return order.Id;
    }
}
```

```csharp
// Architecture test (NetArchTest) keeps modules honest in CI
[Fact]
public void Orders_does_not_reference_payments_internals()
{
    var result = Types.InAssembly(typeof(Order).Assembly)
        .ShouldNot().HaveDependencyOnAny("Payments.Core", "Payments.Infrastructure")
        .GetResult();
    Assert.True(result.IsSuccessful);
}
```

**Why each part exists:** a contracts project is the only public surface; schema-per-module prevents cross-module table joins; architecture tests stop boundary erosion that otherwise happens silently over months.

## Microservices

### When to Use

- Multiple teams need to release independently without coordination
- Parts of the system have very different scaling profiles (search traffic 100x checkout)
- Isolation requirements: PCI scope for payments, data residency, blast-radius reduction
- The organization has platform capabilities: CI/CD per service, observability, on-call, service templates

### When Not to Use

- Early-stage products with unclear domains (you will draw the boundaries wrong and pay to move them)
- Small teams where each engineer would own several services
- Workflows that need strong consistency across many entities in one operation

### What the Same Operation Looks Like Across Services

```csharp
public async Task<IResult> PlaceOrder(PlaceOrder cmd, CancellationToken ct)
{
    var order = Order.Place(cmd.CustomerId, cmd.Lines);

    // Network call: needs timeout, retry policy, circuit breaker, idempotency key
    var auth = await paymentsClient.AuthorizeAsync(
        new AuthorizePayment(order.Id, order.Total), idempotencyKey: order.Id.ToString(), ct);
    if (!auth.Approved) return Results.UnprocessableEntity(auth.Reason);

    await using var tx = await db.Database.BeginTransactionAsync(ct);
    db.Orders.Add(order);
    db.Outbox.Add(OutboxMessage.From(new OrderPlaced(order.Id, order.CustomerId, order.Total)));  // reliable event
    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    // If shipping later fails, a saga issues a compensating RefundPayment command.
    return Results.Created($"/orders/{order.Id}", new { order.Id });
}
```

The business logic is identical; the difference is everything around it: network failure handling, idempotency, outbox, sagas, tracing, versioned contracts, and separate deployments.

## Migration Path: Monolith First, Extract When Justified

```text
1. Build a modular monolith with contracts, schema-per-module, and architecture tests
2. Communicate between modules via contracts and in-process events
3. Measure: which module has different scaling, release cadence, ownership, or compliance needs?
4. Extract that module (strangler fig): route its traffic to a new service, replace in-process calls with HTTP/gRPC, replace in-process events with a broker + outbox
5. Move its schema to its own database; remove the old module
6. Repeat only where evidence supports it
```

## Decision Matrix

| Factor | Favors Modular Monolith | Favors Microservices |
|---|---|---|
| Team size / count | 1-5 teams | Many teams with clear ownership |
| Domain maturity | Still evolving | Stable, well-understood boundaries |
| Consistency needs | Strong, cross-module | Mostly independent per service |
| Scaling differences | Similar across modules | Very different per area |
| Compliance / isolation | Uniform | Specific areas need isolation |
| Platform maturity | Limited | Strong CI/CD, observability, on-call |
| Time to market | Faster initially | Slower initially, faster at org scale |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Starting with 20 microservices for a new product | Start with a modular monolith; extract later |
| Shared database across "microservices" | Database per service; integrate via APIs/events |
| Synchronous call chains (A calls B calls C calls D) | Events, caching, or merging services with chatty coupling |
| "Monolith" without module boundaries | Contracts, schema ownership, architecture tests |
| Splitting by technical layer (UI service, DB service) | Split by business capability / bounded context |
| Coordinated releases of many services | Versioned contracts and backward compatibility |

## Interview Questions
- **[L1]** What is a modular monolith?
- **[L1]** What is the main cost of microservices?
- **[L2]** What is a distributed monolith?
- **[L2]** How does data consistency change?
- **[L3]** What evidence justifies extracting a service?
- **[L3]** Why is a modular monolith often the safer first architecture?

## Interview Answers
1. A modular monolith is a single deployable application organized into explicit business modules, each with its own public contract, internal implementation, and owned data (often a separate schema), with dependency rules enforced by project structure and architecture tests. Modules communicate through contracts or in-process events rather than reaching into each other's internals, so the system has microservice-like boundaries without network calls or distributed deployment.
2. The main cost is distributed-systems complexity: every interaction becomes a network call that can be slow or fail, requiring timeouts, retries, and circuit breakers; consistency across services requires sagas, outboxes, and eventual consistency; debugging needs distributed tracing and centralized logging; and each service needs its own pipeline, monitoring, security, versioned contracts, and on-call ownership. These costs are paid continuously, not once.
3. A distributed monolith is a system split into multiple deployables that are still tightly coupled: services share a database, depend on long synchronous call chains, must be released together, or break when another service changes. It has the operational overhead of microservices without the benefit of independent deployment or failure isolation, and is often the result of splitting before boundaries were understood.
4. In a monolith with one database, a business operation spanning several modules can commit atomically in one local ACID transaction. With microservices, each service owns its database, so cross-service operations cannot share a transaction; they become sequences of local transactions coordinated with events or orchestration (sagas), with compensating actions on failure, the outbox pattern for reliable publishing, idempotent consumers, and reconciliation. Other services see changes eventually rather than immediately.
5. Evidence includes: a module whose change rate or release cadence is blocked by the shared release train; a clear team that will own it end to end; significantly different scaling, latency, or availability requirements measured in production; compliance, security, or data residency isolation needs; failure isolation needs, such as a resource-heavy module destabilizing others; and a stable boundary with low chatty coupling to other modules. The organization must also have the platform and operational capability to run another service.
6. It keeps deployment, debugging, and transactions simple while the team learns the domain and discovers where the real boundaries are. Moving a boundary inside a monolith is a refactoring, whereas moving it between services means migrating data and contracts. With enforced module boundaries, the codebase stays maintainable and extraction-ready, so microservices can be introduced later for specific modules when evidence justifies them, without paying distributed-system costs upfront.
