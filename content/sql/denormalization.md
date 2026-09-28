---
id: sql-denormalization
slug: denormalization
title: Denormalization and Read Models
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Relational modeling"
prerequisites: [sql-normalization, sql-database-design]
tags: [sql, denormalization, read-model, cqrs, consistency]
relatedTopics: [sql-indexes-performance, sql-transactions-isolation]
order: 140
status: published
---
# Denormalization and Read Models

## Introduction
Denormalization stores derived or repeated data intentionally to make a known read workload faster. It shifts cost from reads to writes, storage, consistency, and repair.

```text
Source of truth
      ↓ event/transaction
Projection builder
Read model optimized for one feature
Fast query + explicit freshness
```

## Purpose
Use a read model when repeated joins/aggregations are measurable bottlenecks or when a feature needs a different shape than the transactional schema. A read model is not a second accidental source of truth; it must be rebuildable.

## Real-World Simple Example
```sql
CREATE TABLE CustomerOrderSummary
(
    CustomerId bigint PRIMARY KEY,
    OrderCount integer NOT NULL,
    LifetimeValue decimal(19,4) NOT NULL,
    RefreshedAt timestamp NOT NULL
);
```

A dashboard can read one summary row instead of aggregating millions of orders on every request.

## Professional company-level example
```text
OrderPaid event
Idempotent projector
     ├── update CustomerOrderSummary
     ├── record checkpoint/lag
     └── expose RefreshedAt

Nightly reconciliation
Compare projection with source-of-truth aggregate
Repair/rebuild if needed
```

A projection consumer must handle duplicate and out-of-order events. A rebuild should create a new version/shadow table, verify it, and switch readers safely rather than serving a partially rebuilt table.

## Common failure scenarios
- Projection update succeeds but source event is lost.
- Same event is delivered twice and doubles a total.
- Projection lags during a provider outage.
- A changed projection algorithm requires historical rebuild.
- Sensitive data is duplicated into a store with weaker access controls.

## Comparison
| Choice | Best fit | Cost |
|---|---|---|
| Live normalized query | Fresh data, manageable plan | Join/aggregation cost |
| Summary table | Repeated aggregates | Refresh/reconciliation |
| Materialized view | Database-managed derived query | Engine-specific maintenance |
| Cache | Short-lived repeated reads | Eviction/staleness |
| Separate read store | Different scale/query model | New operational system |

## Interview Questions
- **[L1]** What is denormalization?
- **[L1]** What is a read model?
- **[L2]** Why must a read model be rebuildable?
- **[L2]** How do you make a projection idempotent?
- **[L3]** When does a read model justify CQRS or a separate datastore?
- **[L3]** How should consumers be told that a read model is stale?

## Interview Answers
1. It deliberately stores derived/repeated data to optimize a known read workload.
2. A data shape owned for querying, derived from source-of-truth data and often eventually consistent.
3. Bugs, schema changes, missed events, and new projection logic require recovering the read model without corrupting source data.
4. Store/process a stable event ID or use an operation key and ensure repeating the operation produces the same final state.
5. When query shape/scale differs substantially, live computation harms writes, and the team can operate projection, lag, rebuild, and reconciliation infrastructure.
6. Document freshness, expose `AsOf`/`RefreshedAt` or lag, and define which business operations require source-of-truth reads instead.

## Expert perspective
Denormalization is not automatically optimization. It is a consistency and operations decision. The real design is the ownership, freshness, idempotency, rebuild, security, and reconciliation story around the duplicate data.
