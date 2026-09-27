---
id: efcore-change-tracking
slug: change-tracking
title: EF Core Change Tracking, Relationships, and Concurrency
category: efcore
categoryTitle: Entity Framework Core
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "EF Core 8+"
prerequisites: [dotnet-data-access, sql-transactions-isolation]
tags: [ef-core, dbcontext, tracking, relationships, concurrency]
relatedTopics: [dotnet-data-access, sql-transactions-isolation]
order: 10
status: published
---
# EF Core Change Tracking, Relationships, and Concurrency

## Introduction
EF Core's `DbContext` represents a short unit of work. It tracks entity identity and original values so it can determine which inserts, updates, and deletes to send during `SaveChanges`.

```text
Query entity
   ↓ tracked by DbContext
Change properties/relationships
   ↓ DetectChanges
SQL INSERT/UPDATE/DELETE
   ↓ SaveChanges transaction
Database
```

## Purpose
Tracking is useful when an application loads an aggregate, changes it through domain behavior, and persists it. It is unnecessary overhead for read-only projections, where `AsNoTracking()` is usually better.

## Simple example
```csharp
var order = await db.Orders
    .Include(o => o.Lines)
    .SingleAsync(o => o.Id == orderId, ct);

order.Submit();
await db.SaveChangesAsync(ct);
```

The context knows the original/current state and generates the required update. It is normally scoped to one request/unit of work, not shared globally.

## Relationships and loading

```text
Eager loading   → Include in the query
Explicit loading→ Load deliberately after entity exists
Lazy loading    → Automatic hidden query when navigation accessed
Projection      → Select only required shape
```

Lazy loading can create N+1 queries in loops. For APIs, projections are often clearer and cheaper than loading a large entity graph.

## Professional company-level example: optimistic concurrency
Two operators read the same order. Operator A saves first. Operator B must not silently overwrite A's change:

```csharp
public sealed class Order
{
    public Guid Id { get; set; }
    public byte[] Version { get; set; } = [];
}

modelBuilder.Entity<Order>()
    .Property(o => o.Version)
    .IsRowVersion();
```

If the row version changed, EF Core throws `DbUpdateConcurrencyException`. The application can reload/merge/reject rather than pretending the second edit won.

## Important behavior
- One `DbContext` is not thread-safe.
- Do not run concurrent queries on the same context.
- `AsNoTracking` avoids identity/change snapshots for reads.
- Tracking the same key twice can create identity conflicts.
- `SaveChanges` wraps changes in a transaction for a normal relational provider, but multi-service workflows need other patterns.
- Migrations update schema; they do not replace safe operational rollout planning.

## Comparison
| Mode | Use |
|---|---|
| Tracking | Modify aggregate and save |
| `AsNoTracking` | Read-only query/DTO |
| `AsNoTrackingWithIdentityResolution` | Read-only graph with repeated references |
| Eager loading | Known related data needed |
| Projection | API/report shape |
| Lazy loading | Rare; hidden-query risk |

## Interview Questions
- **[L1]** What does `DbContext` do?
- **[L1]** What is the difference between tracking and `AsNoTracking`?
- **[L2]** Why is `DbContext` not thread-safe?
- **[L2]** What causes an N+1 query with EF Core?
- **[L3]** How does optimistic concurrency prevent lost updates?
- **[L3]** How would you choose between entity loading and projection for an API?

## Interview Answers
1. It represents a unit of work, manages identity/tracking, translates queries, and persists changes through a provider.
2. Tracking records state/original values for updates; `AsNoTracking` skips that work for read-only results.
3. It has mutable internal state and a change tracker designed for one unit of work, not concurrent operations from multiple threads.
4. Loading parents and triggering one child query per parent, commonly through lazy loading or a loop.
5. A concurrency token/version is included in the update condition; if another writer changed it, no row matches and EF reports a conflict instead of overwriting silently.
6. Use projection for lists/read APIs; load an aggregate when domain behavior/invariants and a transaction require the entity graph. Measure shape/rows/query count.

## Expert perspective
EF Core behavior is a database contract. Senior engineers choose tracking/loading based on the unit of work, inspect generated SQL, protect concurrency, and never assume an object graph is cheap merely because it is convenient to navigate.
