---
id: comparisons-rest-grpc
slug: rest-grpc
title: REST vs gRPC vs Events
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "API communication concepts, .NET 8+"
prerequisites: [architecture-api-gateway-communication, architecture-event-driven]
tags: [rest, grpc, events, api, comparison]
relatedTopics: [dotnet-aspnet-core-backend, architecture-resilience]
order: 50
status: published
---
# REST vs gRPC vs Events

## Introduction

Services communicate in three dominant styles: **REST** (resource-oriented HTTP with JSON), **gRPC** (contract-first RPC over HTTP/2 with Protocol Buffers), and **events** (asynchronous messages describing facts that already happened). They are not competitors for the same job. REST and gRPC are **request/response** - the caller waits for an answer. Events are **fire-and-react** - the producer states a fact and does not wait for consumers.

The right choice depends on who the caller is, whether an immediate answer is required, performance needs, contract evolution, and how much coupling in time and availability you can accept.

## Quick Decision Table

| Concern | REST / JSON | gRPC / Protobuf | Events (Kafka, RabbitMQ, SNS/SQS) |
|---|---|---|---|
| Interaction | Request/response | Request/response + streaming | Asynchronous, one-to-many |
| Typical caller | Browsers, mobile, partners, public APIs | Internal services | Any number of independent consumers |
| Contract | OpenAPI (optional, often loose) | `.proto` file (strict, generated clients) | Event schema (JSON Schema, Avro, Protobuf) |
| Payload | Text JSON, human-readable | Binary, compact, fast to parse | Varies |
| Browser support | Native | Needs gRPC-Web or JSON transcoding | Not direct |
| Coupling | Caller waits; both must be up | Caller waits; both must be up | Producer and consumers decoupled in time |
| Consistency | Immediate for the caller | Immediate for the caller | Eventual |
| Main risk | Chatty calls, loose contracts | Tooling, proxies, debugging binary | Duplicates, ordering, replay, schema drift |

## REST

### What It Is

REST models the system as **resources** (`/orders/123`) manipulated with HTTP methods (`GET`, `POST`, `PUT`, `PATCH`, `DELETE`), status codes, and standard caching semantics.

### When to Use REST

- Public APIs, partner integrations, and browser/mobile clients
- CRUD-shaped resources where HTTP caching, status codes, and tooling help
- Teams that value human-readable payloads and easy debugging with `curl`
- Integration with API gateways, WAFs, and documentation portals (OpenAPI)

### When Not to Use REST

- Very high-throughput internal calls where JSON serialization and payload size matter
- Streaming scenarios (bidirectional, long-lived)
- Reactions that do not need an immediate answer (use events)

### Example (ASP.NET Core Minimal API)

```csharp
app.MapGet("/orders/{id:guid}", async (Guid id, OrdersDb db, CancellationToken ct) =>
    await db.Orders.AsNoTracking().Where(o => o.Id == id)
        .Select(o => new OrderDto(o.Id, o.Status, o.Total))
        .FirstOrDefaultAsync(ct) is { } order
        ? Results.Ok(order)
        : Results.NotFound());

app.MapPost("/orders", async (CreateOrder cmd, IOrderService orders, CancellationToken ct) =>
{
    var id = await orders.CreateAsync(cmd, ct);
    return Results.Created($"/orders/{id}", new { id });
});
```

**Why each part exists:** route constraints (`{id:guid}`) reject malformed input early; `AsNoTracking` and projection avoid loading entities you will not modify; `404` and `201 Created` with a `Location` header use HTTP semantics clients and gateways understand.

## gRPC

### What It Is

gRPC defines services and messages in a `.proto` contract. Tools generate strongly typed clients and servers. It runs over HTTP/2 with binary Protobuf encoding and supports unary calls plus client, server, and bidirectional streaming.

### When to Use gRPC

- Internal service-to-service calls with high volume or strict latency budgets
- Polyglot systems that benefit from generated, strongly typed clients
- Streaming (live price feeds, telemetry, long-running progress)
- Teams willing to enforce contract-first development and backward-compatible schema changes

### When Not to Use gRPC

- Public or browser-facing APIs (unless you add gRPC-Web or JSON transcoding)
- Environments where proxies, load balancers, or firewalls do not handle HTTP/2 well
- Teams that need to inspect traffic easily without specialized tools

### Example

```protobuf
syntax = "proto3";
option csharp_namespace = "Inventory.Grpc";

service Inventory {
  rpc Reserve (ReserveRequest) returns (ReserveReply);
  rpc WatchStock (WatchRequest) returns (stream StockLevel);
}

message ReserveRequest { string order_id = 1; string sku = 2; int32 quantity = 3; }
message ReserveReply   { bool reserved = 1; string reason = 2; }
message WatchRequest   { string sku = 1; }
message StockLevel     { string sku = 1; int32 available = 2; }
```

```csharp
// Server
public sealed class InventoryService(IStockStore store) : Inventory.InventoryBase
{
    public override async Task<ReserveReply> Reserve(ReserveRequest request, ServerCallContext context)
    {
        var ok = await store.TryReserveAsync(request.Sku, request.Quantity, request.OrderId, context.CancellationToken);
        return new ReserveReply { Reserved = ok, Reason = ok ? "" : "insufficient_stock" };
    }
}

// Client with a deadline - never call without one
var client = new Inventory.InventoryClient(GrpcChannel.ForAddress("https://inventory"));
var reply = await client.ReserveAsync(
    new ReserveRequest { OrderId = orderId, Sku = "SKU-1", Quantity = 2 },
    deadline: DateTime.UtcNow.AddMilliseconds(300),
    cancellationToken: ct);
```

**Contract evolution rules:** never reuse or renumber field tags, add new fields as optional, and reserve removed field numbers (`reserved 4;`) so old clients keep working.

## Events

### What It Is

An event is an immutable record of a business fact, such as `OrderPaid`, published to a broker. Any number of consumers subscribe and react independently. The producer does not know or wait for them.

### When to Use Events

- Several independent reactions to one fact (billing, email, analytics, search index)
- Work that can happen later without blocking the user
- Decoupling availability: consumers can be down and catch up later
- Building read models, audit trails, and integration with other bounded contexts

### When Not to Use Events

- The caller needs an immediate decision (stock available? payment authorized?)
- Simple CRUD with one consumer where a direct call is clearer
- Teams without idempotency, schema governance, and lag monitoring in place

### Example (Publishing Reliably with an Outbox)

```csharp
public async Task MarkPaidAsync(Guid orderId, CancellationToken ct)
{
    await using var tx = await db.Database.BeginTransactionAsync(ct);
    var order = await db.Orders.SingleAsync(o => o.Id == orderId, ct);
    order.MarkPaid();
    db.Outbox.Add(OutboxMessage.From(new OrderPaid(order.Id, order.CustomerId, order.Total, DateTimeOffset.UtcNow)));
    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
    // A background relay reads the outbox table and publishes to the broker.
}

// Consumer: idempotent handling because delivery is at-least-once
public async Task Handle(OrderPaid evt, CancellationToken ct)
{
    if (await processed.ExistsAsync(evt.OrderId, "send-receipt", ct)) return;
    await email.SendReceiptAsync(evt.CustomerId, evt.OrderId, ct);
    await processed.MarkAsync(evt.OrderId, "send-receipt", ct);
}
```

**Why the outbox:** writing to the database and publishing to a broker are two separate systems. Without an outbox, a crash between them either loses the event or publishes an event for a change that rolled back.

## Same Scenario, Three Styles

Checkout needs to: confirm stock, take payment, and then notify billing, email, and analytics.

| Step | Best Style | Reason |
|---|---|---|
| Browser submits checkout | REST | Public client, needs immediate result |
| Order service reserves stock | gRPC (or REST) | Internal, latency-sensitive, needs an answer now |
| Order service authorizes payment | REST/gRPC to payment provider | Must know the result before confirming |
| Notify billing, email, analytics, search | Events (`OrderPaid`) | Independent reactions; none should block checkout |

## Decision Matrix

| Factor | Favors REST | Favors gRPC | Favors Events |
|---|---|---|---|
| External or browser clients | Strong | Weak | Not applicable |
| Latency and throughput | Medium | Strong | High throughput, higher end-to-end latency |
| Need immediate answer | Yes | Yes | No |
| Multiple consumers | Weak (fan-out calls) | Weak | Strong |
| Availability decoupling | Weak | Weak | Strong |
| Debuggability | Strong | Medium | Medium (needs tracing across async hops) |
| Contract strictness | Medium | Strong | Depends on schema registry discipline |
| Operational cost | Low | Medium | Higher (broker, DLQs, lag, replay) |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Chain of 6 synchronous calls per user request | Collapse calls, cache, or move non-critical steps to events |
| gRPC calls without deadlines | Always set deadlines and propagate cancellation |
| "Event" that is really a command expecting a reply | Use request/response or an explicit command queue with a reply channel |
| Publishing events directly after `SaveChanges` | Use the outbox pattern |
| Consumers that are not idempotent | Deduplicate by event ID or business key |
| Breaking `.proto` or event schemas | Additive changes only, reserved field numbers, schema registry compatibility checks |

## Interview Questions
- **[L1]** What is REST good at?
- **[L1]** What is gRPC good at?
- **[L2]** When should an event be used instead of an RPC?
- **[L2]** What failure behavior does each style introduce?
- **[L3]** How would you choose communication for an order workflow?
- **[L3]** Why can a gateway not remove distributed-system complexity?

## Interview Answers
1. REST is good at broadly interoperable, resource-oriented APIs over plain HTTP. It works natively in browsers and mobile clients, uses standard status codes and HTTP caching, is easy to debug with human-readable JSON, and integrates with gateways, WAFs, and OpenAPI documentation, which makes it the default for public, partner, and frontend-facing APIs.
2. gRPC is good at efficient, strongly typed internal service-to-service communication. Contracts in `.proto` files generate clients and servers in many languages, binary Protobuf over HTTP/2 reduces payload size and parsing cost, connections are multiplexed, and it supports server, client, and bidirectional streaming, which suits high-volume, low-latency, or streaming internal workloads.
3. Use an event when the producer does not need an immediate answer, when several independent consumers should react to the same fact, when consumers may be unavailable and should catch up later, or when you want to avoid coupling the user's request latency and availability to downstream side effects such as emails, analytics, or search indexing. If the caller must make a decision based on the result, use request/response instead.
4. REST and gRPC introduce temporal coupling: the caller fails or slows when the callee is slow or down, so you need timeouts or deadlines, retries with backoff for idempotent operations, circuit breakers, and handling of partial failure in call chains. Events introduce at-least-once delivery (duplicates), possible out-of-order or late delivery, consumer lag, poison messages that need dead-letter queues, schema evolution risks, replay concerns, and eventual consistency that users and downstream systems must tolerate.
5. Split by what needs an immediate answer. The client submits checkout via REST; the order service synchronously reserves stock and authorizes payment (gRPC internally or REST to the provider) with deadlines and idempotency keys because the result determines the response. After committing the order, it records `OrderPaid` in an outbox in the same transaction, and a relay publishes it so billing, email, analytics, and search react asynchronously with idempotent consumers. Multi-step workflows that span services and need compensation (refund if shipping fails) are coordinated with a saga.
6. A gateway centralizes cross-cutting concerns such as routing, authentication, rate limiting, and TLS, but the downstream calls still happen over a network. Latency still adds up across hops, partial failures still occur, retries can still amplify load, aggregation endpoints still fan out to multiple services, authorization still has to be enforced by owning services, and data consistency across services still requires explicit design. The gateway can even become a single point of failure or a bottleneck if it accumulates business logic.
