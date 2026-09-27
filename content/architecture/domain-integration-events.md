---
id: architecture-domain-integration-events
slug: domain-integration-events
title: Domain Events and Integration Events
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-event-driven, architecture-design-patterns-ddd]
tags: [domain-events, integration-events, ddd, messaging]
relatedTopics: [architecture-cqrs, architecture-outbox-inbox]
order: 130
status: published
---
# Domain Events and Integration Events

## Introduction
A domain event records a meaningful fact inside a bounded context. An integration event is an external contract published for another service/context. They may describe the same business occurrence but should not be treated as the same model automatically.

```text
Aggregate changes
      ↓
Domain event: internal language
      ↓ transaction boundary
Outbox/translator
      ↓ versioned contract
Integration event: external language
      ↓
Other service consumers
```

## Purpose
Domain events keep internal business reactions decoupled. Integration events prevent internal classes/fields from becoming public contracts and allow services to evolve independently.

## Simple example
```csharp
public sealed record OrderSubmittedDomainEvent(Guid OrderId);

public sealed record OrderSubmittedV1(
    Guid EventId,
    Guid OrderId,
    decimal Total,
    DateTimeOffset OccurredAt);
```

The domain event may carry an aggregate reference or internal value objects; the integration event should carry a stable, serialized contract.

## Professional company-level example
```text
Order.Submit()
  → collect OrderSubmittedDomainEvent
  → save aggregate + outbox in one transaction
  → map to OrderSubmitted.v1
  → publish to broker
  → consumers process idempotently
```

Do not publish an EF entity directly as an event. Define ownership, schema version, PII policy, retention, consumer expectations, and compatibility rules.

## Comparison
| Event | Scope | Contract |
|---|---|---|
| Domain event | Inside bounded context | Internal model |
| Integration event | Across boundaries | Versioned external schema |
| Command | Request to act | One intended owner |
| Notification | In-process signal | Usually ephemeral |

## Interview Questions
- **[L1]** What is a domain event?
- **[L1]** What is an integration event?
- **[L2]** Why should an EF/domain entity not be serialized directly as an integration event?
- **[L2]** How do domain events interact with transactions?
- **[L3]** How would you evolve an integration event without breaking consumers?
- **[L3]** When should an event stay internal instead of being published externally?

## Interview Answers
1. An internal fact that a domain change occurred and may trigger other domain/application behavior.
2. A stable external message describing a business fact for another service/context to consume.
3. Entity shape contains internal fields/behavior and changes with persistence; external events need stable, versioned, minimal contracts.
4. Collect/handle internal effects within the local transaction when appropriate; persist publication intent through an outbox so external delivery is reliable after commit.
5. Add backward-compatible fields, version breaking semantic changes, tolerate unknown fields, and run contract tests with existing consumers.
6. Keep it internal when no other boundary needs the fact, publication would leak implementation detail, or eventual-consistency/operational cost is not justified.

## Expert perspective
Events are contracts with consumers. Senior engineers separate internal domain vocabulary from external integration schema and design transaction, versioning, privacy, replay, and ownership rules before publishing.
