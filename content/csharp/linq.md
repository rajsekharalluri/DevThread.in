---
id: csharp-linq
slug: linq
title: "C# LINQ: In-Memory Queries and Database Translation"
category: csharp
categoryTitle: C#
difficulty: intermediate
estimatedMinutes: 55
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-collections-delegates-events, csharp-generics]
tags: [csharp, linq, ienumerable, iqueryable, expressions, queries]
relatedTopics: [dotnet-data-access, sql-query-examples]
order: 50
status: published
---
# C# LINQ: In-Memory Queries and Database Translation

## Introduction
LINQ provides a common query syntax for in-memory sequences and external query providers. `IEnumerable<T>` usually executes delegates in .NET; `IQueryable<T>` builds expression trees that a provider such as EF Core may translate into SQL.

```text
IEnumerable<T> → objects in memory → C# delegates
IQueryable<T>  → provider query    → expression tree → SQL/remote query
```

## Purpose
LINQ makes filtering, projection, grouping, joining, ordering, and aggregation composable. The important production skill is knowing where execution occurs and when data is materialized.

## Simple example
```csharp
var overdue = orders
    .Where(o => o.Status == OrderStatus.Open && o.DueAt < clock.UtcNow)
    .OrderBy(o => o.DueAt)
    .Select(o => new OverdueOrder(o.Id, o.DueAt))
    .ToList();
```

`Where`, `OrderBy`, and `Select` are normally deferred. `ToList()` executes the pipeline and materializes the result.

## Professional company-level example
```csharp
public async Task<PagedResult<OrderSummary>> SearchAsync(
    OrderSearchRequest request,
    CancellationToken ct)
{
    IQueryable<Order> query = db.Orders
        .AsNoTracking()
        .Where(o => o.TenantId == request.TenantId);

    if (request.Status is not null)
        query = query.Where(o => o.Status == request.Status);

    var total = await query.CountAsync(ct);

    var items = await query
        .OrderByDescending(o => o.CreatedAt)
        .ThenByDescending(o => o.Id)
        .Skip(request.Skip)
        .Take(request.PageSize)
        .Select(o => new OrderSummary(o.Id, o.Total, o.Status))
        .ToListAsync(ct);

    return new(items, total);
}
```

Filtering, counting, sorting, paging, and projection execute in the database. The API does not load an entire table into application memory.

## Execution traps

```csharp
// Bad: loads every order first, then filters in memory.
var open = db.Orders.ToList()
    .Where(o => o.Status == OrderStatus.Open);

// Better: provider translates the filter to SQL.
var open = await db.Orders
    .Where(o => o.Status == OrderStatus.Open)
    .ToListAsync(ct);
```

```csharp
// Bad: query executes repeatedly.
IQueryable<Order> query = db.Orders.Where(o => o.TenantId == tenantId);
var count = await query.CountAsync(ct);
var firstPage = await query.Take(20).ToListAsync(ct);
```

The repeated execution may be intentional, but the consistency/cost should be understood. Use one query/projection or explicit snapshot semantics when required.

## `IEnumerable` versus `IQueryable`

| Type | Executes where | Lambda becomes |
|---|---|---|
| `IEnumerable<T>` | In application process | Delegate/C# code |
| `IQueryable<T>` | Provider/database | Expression tree/query language |

A custom C# method may work for `IEnumerable` but fail translation for `IQueryable`. Integration-test important queries against the real provider, not only an in-memory fake.

## Interview Questions
- **[L1]** What is deferred execution?
- **[L1]** What is the difference between `IEnumerable<T>` and `IQueryable<T>`?
- **[L2]** Why is calling `ToList()` too early dangerous?
- **[L2]** What is the N+1 query problem?
- **[L3]** How would you design reusable query specifications?
- **[L3]** How do you verify that a LINQ query is efficient in production?

## Interview Answers
1. Most LINQ operators build an iterator/query description and execute only when enumerated or materialized by `ToList`, `First`, `Count`, etc.
2. `IEnumerable` normally runs C# delegates over in-memory data; `IQueryable` builds provider-translatable expressions, commonly SQL.
3. It loads data and stops server-side filtering/projection/pagination, increasing memory, network, database, and latency cost.
4. It loads parents once and then queries children inside a loop, producing 1+N database calls. Use projection, eager loading, or batching.
5. Put business predicates in named specifications/extension methods on `IQueryable<T>` while keeping tenant/security filters impossible to omit.
6. Inspect generated SQL and actual execution plans, query count, logical reads, rows/materialization, latency percentiles, and behavior with production-like data.

## Expert perspective
LINQ is a language surface, not a performance guarantee. Senior developers always identify the execution boundary, materialization point, provider translation, and database plan before judging a query.
