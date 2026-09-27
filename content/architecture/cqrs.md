---
id: architecture-cqrs
slug: cqrs
title: "CQRS: Command Query Responsibility Segregation"
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-fundamentals, architecture-design-patterns-ddd]
tags: [cqrs, commands, queries, read-models, architecture]
relatedTopics: [architecture-event-driven, sql-denormalization, architecture-microservices]
order: 120
status: published
---
# CQRS: Command Query Responsibility Segregation

## Introduction
CQRS separates operations that change state (commands) from operations that read state (queries). It does not require two databases, event sourcing, or microservices; those are optional extensions.

```text
Command side                         Query side
validate + business rules             read-optimized projection
       ↓                                      ↑
write model/database ── events ── read model/database
```

## Purpose
Commands and queries often have different shapes and performance needs. Separating them can keep write-side invariants clear while allowing read models to be shaped for screens/reports.

## Simple example
```csharp
public sealed record SubmitOrder(Guid OrderId);
public sealed record GetOrderSummary(Guid OrderId);

public Task Handle(SubmitOrder command, CancellationToken ct)
    => orderAggregate.SubmitAndSaveAsync(command.OrderId, ct);

public Task<OrderSummary?> Handle(GetOrderSummary query, CancellationToken ct)
    => readStore.GetOrderSummaryAsync(query.OrderId, ct);
```

The command changes state through domain rules; the query reads a projection and does not load an aggregate to mutate it.

## Professional company-level example
A write transaction updates the source of truth and emits an outbox event. A projection consumer updates a read model used by dashboards/search. The read model may be eventually consistent, so the API documents freshness and falls back to source data for decisions that require current truth.

```text
POST /orders
  → command handler → order DB + outbox
  → 202/201

GET /orders/{id}/summary
  → read model optimized for screen
  → AsOf/lag metadata
```

## When CQRS is not needed
A simple CRUD service with one schema, modest read/write differences, and no complex workflows may be clearer with one model and ordinary queries. CQRS adds models, handlers, synchronization, observability, and repair work.

## Comparison
| Model | Best fit | Cost |
|---|---|---|
| CRUD | Simple consistent app | Less specialized reads |
| CQRS same DB | Different read/write models | More code/coordination |
| CQRS separate read store | Scale/query shape divergence | Eventual consistency/ops |
| Event sourcing | Audit/history is core truth | Complex storage/replay |

## Interview Questions
- **[L1]** What does CQRS separate?
- **[L1]** Does CQRS require two databases?
- **[L2]** Why can CQRS improve read performance?
- **[L2]** What consistency problem can CQRS introduce?
- **[L3]** When is CQRS unnecessary complexity?
- **[L3]** How would you repair a corrupt CQRS read model?

## Interview Answers
1. It separates state-changing commands from read-only queries.
2. No. It can use one database with separate models; separate stores are optional.
3. Read models can be projections shaped/indexed for specific screens instead of forcing reads through write aggregates.
4. Asynchronous projections can lag, so reads may be stale and need freshness/rebuild/reconciliation strategy.
5. Avoid it for simple CRUD and stable read/write models where the extra handlers and synchronization provide no measured benefit.
6. Rebuild into a shadow/versioned projection from source-of-truth events/data, verify counts/checksums, then switch reads atomically with rollback.

## Expert perspective
CQRS is a boundary and workload decision, not a badge of advanced architecture. Introduce it when read/write models genuinely diverge and the team can operate eventual consistency and rebuilds.
