---
id: architecture-event-driven
slug: event-driven
title: Event-Driven Architecture
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-fundamentals, architecture-distributed-systems]
tags: [architecture, events, messaging, eventual-consistency, outbox]
relatedTopics: [architecture-kafka, architecture-messaging-patterns]
order: 30
status: published
---
# Event-Driven Architecture

## Introduction
An event is an immutable fact that already happened, such as `OrderPaid` or `InventoryReserved`. Event-driven architecture publishes these facts so other components can react asynchronously without the producer directly calling every consumer.

```text
Order service
    │ commits order + outbox event
    ▼
Event broker
    ├── billing consumer
    ├── inventory consumer
    ├── notification consumer
    └── analytics consumer
```

## Purpose
Events reduce direct coupling, let consumers scale independently, absorb bursts, and allow new consumers to be added without changing the original producer. The cost is eventual consistency, duplicate delivery, retries, schema governance, and harder debugging.

## Event versus command

```text
Command: “Please authorize this payment.”
         addressed to one owner, may succeed/fail

Event:   “Payment was authorized.”
         fact, many consumers may react
```

Naming matters. Past-tense events should not secretly contain commands.

## Simple example
```csharp
public sealed record OrderPaid(
    Guid EventId,
    Guid OrderId,
    decimal Amount,
    DateTimeOffset OccurredAt);
```

A consumer must expect duplicates:

```csharp
if (await processed.ExistsAsync(message.EventId, ct))
    return;

await UpdateLocalStateAsync(message, ct);
await processed.MarkAsync(message.EventId, ct);
```

## Professional company-level example: outbox
Without an outbox:

```text
1. Commit order
2. Publish event
3. Process crashes between steps
→ Order exists, event is lost
```

With an outbox:

```text
One database transaction
   ├── update order
   └── insert outbox row
          ↓ commit
Relay publishes outbox event
          ↓
Consumers process/retry/replay
```

The outbox does not provide global exactly-once processing; it makes the local state change and publication intent atomic. Consumers still need idempotency.

## Failure behavior
A production event flow needs:

- At-least-once delivery assumptions.
- Bounded retries and exponential backoff with jitter.
- Dead-letter storage for poison messages.
- Schema versioning/backward compatibility.
- Consumer lag and oldest-message metrics.
- Replay and reconciliation procedures.
- Authorization and sensitive-data controls on payloads.

## Comparison
| Communication | Coupling | Consistency | Best use |
|---|---|
| Direct HTTP | Strong timing/availability coupling | Immediate | Immediate decision |
| Queue command | One work owner, async | Eventual | Background work |
| Event stream | Many independent consumers | Eventual | Facts/reactions/replay |
| Shared database | High schema coupling | Immediate local | Usually transitional |

## Interview Questions
- **[L1]** What is an event and how is it different from a command?
- **[L1]** Why must event consumers be idempotent?
- **[L2]** What problem does the outbox pattern solve?
- **[L2]** What happens when a consumer repeatedly fails?
- **[L3]** When should a system use events instead of synchronous calls?
- **[L3]** How do you evolve event schemas without breaking existing consumers?

## Interview Answers
1. An event records a completed fact; a command requests an action from an owner.
2. At-least-once delivery and retries can deliver the same event multiple times. Idempotency prevents duplicate side effects.
3. It prevents a committed local change from losing the intent to publish because the application crashed between database commit and broker publish.
4. Retry transient failures with limits/backoff, then move poison messages to a dead-letter queue with alerting and controlled replay.
5. Use events when reactions can be asynchronous, multiple consumers need the fact, or producer/consumer availability should be decoupled. Use synchronous calls for immediate decisions.
6. Add compatible fields, version contracts when semantics change, keep consumers tolerant of unknown fields, and support old/new consumers during rolling deployment.

## Expert perspective
Event-driven design is a consistency and failure decision, not merely a way to add Kafka. Senior engineers design the outbox, idempotency, schema, retry, replay, security, and operational story before choosing the broker.
