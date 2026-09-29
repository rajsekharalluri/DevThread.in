---
id: efcore-loading-dbcontext-lifetime
slug: loading-dbcontext-lifetime
title: "EF Core Loading Strategies, N+1, DbContext Lifetime, and Transactions"
category: efcore
categoryTitle: Entity Framework Core
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "EF Core 8+"
prerequisites: [efcore-change-tracking]
tags: [ef-core, loading, n-plus-one, dbcontext, pooling, transactions, bulk-operations]
relatedTopics: [efcore-query-performance, efcore-migrations-deployment, dotnet-data-access]
order: 15
status: published
---
# EF Core Loading Strategies, N+1, DbContext Lifetime, and Transactions

## Introduction

Most EF Core performance and correctness problems come from four areas: **how related data is loaded**, **accidental N+1 query patterns**, **misusing DbContext lifetime**, and **unclear transaction boundaries**. This page explains each with working code, shows how to diagnose them, and gives production defaults.

## Part 1: Loading Related Data

EF Core offers several ways to load navigation properties. Choosing the wrong one silently multiplies queries or loads far more data than needed.

| Strategy | How | Queries | Best For | Risk |
|---|---|---|---|---|
| **Projection** (`Select`) | Shape exactly the columns you need | 1 | Read endpoints, lists, DTOs | None - the preferred default for reads |
| **Eager loading** (`Include`) | Load entities with related entities | 1 (single query) or N (split query) | Loading an aggregate to modify it | Over-fetching, cartesian explosion |
| **Split queries** (`AsSplitQuery`) | One SQL query per included collection | 1 + collections | Several large collection includes | Multiple round trips, no single snapshot |
| **Explicit loading** (`Entry().Collection().LoadAsync()`) | Load a navigation on demand | 1 per call | Conditional loading of one navigation | Easy to call in loops |
| **Lazy loading** (proxies) | Navigation loads automatically when accessed | 1 per access | Rarely recommended | N+1 queries, surprise I/O |

### Projection (Default for Reads)

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Where(o => o.CustomerId == customerId)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new OrderListItem(
        o.Id,
        o.CreatedAt,
        o.Status,
        o.Lines.Count,                       // translated to a COUNT subquery
        o.Lines.Sum(l => l.Quantity * l.UnitPrice)))
    .Take(20)
    .ToListAsync(ct);
```

EF generates one SQL query that returns only the listed columns. No entities are tracked, no unneeded columns are transferred.

### Eager Loading for Writes

```csharp
var order = await db.Orders
    .Include(o => o.Lines)
    .Include(o => o.ShippingAddress)
    .SingleAsync(o => o.Id == orderId, ct);

order.AddLine(productId, quantity, unitPrice);  // aggregate needs its lines to enforce invariants
await db.SaveChangesAsync(ct);
```

Load the **whole aggregate** when you are about to modify it and its invariants depend on child data.

### Cartesian Explosion and Split Queries

Including two collections in one query multiplies rows: an order with 50 lines and 20 status history entries returns 50 x 20 = 1,000 rows for one order.

```csharp
// Single query with two collection includes: rows = lines x history
var heavy = await db.Orders.Include(o => o.Lines).Include(o => o.History).SingleAsync(o => o.Id == id, ct);

// Split into separate queries: rows = lines + history
var light = await db.Orders
    .Include(o => o.Lines)
    .Include(o => o.History)
    .AsSplitQuery()
    .SingleAsync(o => o.Id == id, ct);
```

EF Core logs a warning when it detects multiple collection includes in a single query. You can set the default globally with `UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)` and opt back with `AsSingleQuery()`. Split queries use multiple round trips and, without a transaction at a suitable isolation level, may see data changed between queries.

### Filtered Include

```csharp
var customer = await db.Customers
    .Include(c => c.Orders.Where(o => o.Status == OrderStatus.Open).OrderByDescending(o => o.CreatedAt).Take(5))
    .SingleAsync(c => c.Id == customerId, ct);
```

## Part 2: The N+1 Problem

N+1 happens when code runs one query for a list, then one additional query **per item** to load related data.

### How It Happens

```csharp
// With lazy loading enabled - looks innocent, runs 1 + N queries
var orders = await db.Orders.Where(o => o.CreatedAt >= today).ToListAsync(ct);   // 1 query
foreach (var order in orders)
{
    Console.WriteLine($"{order.Customer.Name}: {order.Lines.Count} lines");      // 2 queries per order
}

// Without lazy loading - same problem written explicitly
foreach (var order in orders)
{
    var customer = await db.Customers.FindAsync([order.CustomerId], ct);          // 1 query per order
}
```

For 500 orders this is 1,001 database round trips. At 2 ms each, that is two seconds of pure latency.

### How to Fix It

```csharp
var report = await db.Orders
    .AsNoTracking()
    .Where(o => o.CreatedAt >= today)
    .Select(o => new { CustomerName = o.Customer.Name, LineCount = o.Lines.Count })
    .ToListAsync(ct);                                                              // 1 query with joins
```

Or batch-load related data once:

```csharp
var customerIds = orders.Select(o => o.CustomerId).Distinct().ToList();
var customers = await db.Customers.Where(c => customerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, ct);
```

### How to Detect N+1

- Enable SQL logging in development: `optionsBuilder.LogTo(Console.WriteLine, LogLevel.Information)`
- Watch the **number of queries per request** in OpenTelemetry traces (EF Core instrumentation creates a span per command)
- Write a test that counts commands with a `DbCommandInterceptor` and asserts a maximum per endpoint

```csharp
public sealed class CommandCounter : DbCommandInterceptor
{
    public int Count;
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
    {
        Interlocked.Increment(ref Count);
        return base.ReaderExecutingAsync(command, eventData, result, ct);
    }
}
```

Avoid lazy loading in web applications. If you enable it, treat every navigation access as a potential query.

## Part 3: DbContext Lifetime and Pooling

### The Rules

| Rule | Why |
|---|---|
| One `DbContext` per unit of work (typically one HTTP request) | It is a unit of work with a change tracker |
| Never share a `DbContext` across threads or concurrent operations | It is not thread-safe; concurrent use throws |
| Never make it a singleton | The change tracker grows forever; stale data; thread-safety issues |
| Keep it short-lived | Long-lived contexts accumulate tracked entities and memory |

### Registration Options

```csharp
// Standard: scoped per request
builder.Services.AddDbContext<OrdersDbContext>(o => o.UseNpgsql(cs));

// Pooled: reuses DbContext instances to reduce allocation overhead in high-throughput APIs
builder.Services.AddDbContextPool<OrdersDbContext>(o => o.UseNpgsql(cs), poolSize: 1024);

// Factory: for code that controls lifetime itself (background services, parallel work, Blazor)
builder.Services.AddPooledDbContextFactory<OrdersDbContext>(o => o.UseNpgsql(cs));
```

**Pooling caveat:** pooled contexts are reset and reused, so do not store per-request state (like tenant ID) in fields set via constructor injection of scoped services. Set such state after retrieval, or use the documented pattern of a scoped factory that assigns tenant state on each lease.

### Background Services Need Their Own Scope

`BackgroundService` is a singleton. Injecting a scoped `DbContext` directly into it is a bug.

```csharp
public sealed class ExpireCartsWorker(IDbContextFactory<OrdersDbContext> contexts, TimeProvider clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await using var db = await contexts.CreateDbContextAsync(stoppingToken);   // fresh context per iteration
            var cutoff = clock.GetUtcNow().AddHours(-24);
            await db.Carts.Where(c => c.UpdatedAt < cutoff).ExecuteDeleteAsync(stoppingToken);
        }
    }
}
```

### Parallel Queries Need Separate Contexts

```csharp
await using var db1 = await contexts.CreateDbContextAsync(ct);
await using var db2 = await contexts.CreateDbContextAsync(ct);
var ordersTask = db1.Orders.CountAsync(ct);
var customersTask = db2.Customers.CountAsync(ct);
await Task.WhenAll(ordersTask, customersTask);
```

## Part 4: Transactions

### Default Behavior

`SaveChangesAsync` wraps all pending changes in **one transaction** automatically. If any statement fails, everything rolls back. For most use cases, a single `SaveChangesAsync` per unit of work is all you need.

### Explicit Transactions

Use an explicit transaction when multiple `SaveChangesAsync` calls, raw SQL, or set-based operations must be atomic together.

```csharp
await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);

var account = await db.Accounts.SingleAsync(a => a.Id == fromId, ct);
account.Withdraw(amount);
await db.SaveChangesAsync(ct);

await db.Accounts.Where(a => a.Id == toId)
    .ExecuteUpdateAsync(s => s.SetProperty(a => a.Balance, a => a.Balance + amount), ct);

db.Outbox.Add(OutboxMessage.From(new TransferCompleted(fromId, toId, amount)));
await db.SaveChangesAsync(ct);

await tx.CommitAsync(ct);   // disposing without commit rolls back
```

### Execution Strategies and Retries

With connection resiliency enabled (`EnableRetryOnFailure`), user-initiated transactions must run inside the execution strategy so the **whole unit** is retried:

```csharp
var strategy = db.Database.CreateExecutionStrategy();
await strategy.ExecuteAsync(async () =>
{
    await using var tx = await db.Database.BeginTransactionAsync(ct);
    // ... work ...
    await db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);
});
```

Work inside must be safe to repeat (re-read state rather than relying on values captured before the first attempt).

### Transaction Guidelines

| Guideline | Reason |
|---|---|
| Keep transactions short | Long transactions hold locks and block other requests |
| Never call external HTTP APIs inside a transaction | Network latency holds locks; the external call cannot be rolled back |
| Use optimistic concurrency tokens for user edits | Detect lost updates without long locks |
| Use the outbox for events | Publish only what was committed |

## Part 5: Bulk Operations

| Need | Approach |
|---|---|
| Update/delete many rows by a condition | `ExecuteUpdateAsync` / `ExecuteDeleteAsync` (single SQL statement, no tracking) |
| Insert thousands of rows | `AddRange` + one `SaveChangesAsync` (EF batches statements) |
| Insert millions of rows | Provider bulk copy (`SqlBulkCopy`, Npgsql binary `COPY`) or libraries like EFCore.BulkExtensions |

```csharp
// Archive old orders in one statement instead of loading and deleting 200,000 entities
var archived = await db.Orders
    .Where(o => o.Status == OrderStatus.Completed && o.CreatedAt < cutoff)
    .ExecuteDeleteAsync(ct);
```

`ExecuteUpdate/ExecuteDelete` bypass the change tracker: already-tracked entities are not updated, and `SaveChanges` interceptors and concurrency checks do not run for these rows.

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Lazy loading in APIs | Projection or explicit `Include` |
| `Include` on every query "just in case" | Project the fields each endpoint needs |
| Two collection includes on large data | `AsSplitQuery` or projection |
| Singleton or static `DbContext` | Scoped, pooled, or factory-created per unit of work |
| Injecting `DbContext` into a `BackgroundService` | `IDbContextFactory` or scope per iteration |
| HTTP calls inside database transactions | Commit first; use outbox for follow-up work |
| Loading entities to delete them in bulk | `ExecuteDeleteAsync` |

## Interview Questions
- **[L1]** What is the difference between eager loading, explicit loading, and lazy loading in EF Core?
- **[L1]** What is the N+1 query problem?
- **[L2]** What is cartesian explosion and how do split queries help?
- **[L2]** Why should a DbContext not be shared across threads or registered as a singleton?
- **[L2]** When do you need an explicit transaction instead of relying on SaveChanges?
- **[L3]** How would you find and eliminate N+1 queries across a large existing codebase?
- **[L3]** How do you use EF Core safely inside background services and parallel workloads?
- **[L3]** What are the trade-offs of ExecuteUpdate/ExecuteDelete compared with loading entities and calling SaveChanges?

## Interview Answers
1. Eager loading uses `Include` to load related entities as part of the original query. Explicit loading loads a navigation later on demand with `context.Entry(entity).Collection(...).LoadAsync()` or `.Reference(...)`. Lazy loading, enabled via proxies, loads a navigation automatically the first time code accesses it, which is convenient but hides database calls and easily causes N+1 queries. For read endpoints, projections with `Select` are usually better than any of the three because they fetch only needed columns.
2. The N+1 problem occurs when code executes one query to fetch a list of N items and then one additional query per item to fetch related data, for example looping over orders and accessing each order's customer with lazy loading. The number of round trips grows with the data size, so latency and database load grow linearly, often becoming the dominant cost of an endpoint. It is fixed with projections, eager loading, or batch-loading related data in one query.
3. Cartesian explosion happens when a single SQL query joins multiple collection navigations: the result contains the product of the collection sizes for each parent row, duplicating parent and child data and transferring far more rows than needed. Split queries (`AsSplitQuery`) load each included collection in a separate SQL query, so the total rows equal the sum rather than the product. The trade-offs are more round trips and potential inconsistency between queries if data changes, unless a suitable transaction is used.
4. `DbContext` is not thread-safe: its change tracker, connection, and internal state assume one operation at a time, and concurrent use throws or corrupts state. As a singleton it would be shared across concurrent requests, its change tracker would grow indefinitely and hold stale entities, and memory would leak. It is designed as a short-lived unit of work, typically scoped per request, pooled, or created from a factory for each unit of work.
5. `SaveChanges` already wraps all its pending changes in one transaction. You need an explicit transaction when several `SaveChanges` calls, raw SQL commands, `ExecuteUpdate/ExecuteDelete` operations, or reads with a specific isolation level must succeed or fail together, or when coordinating an outbox write with other operations across multiple save calls. With retrying execution strategies, the explicit transaction must run inside `CreateExecutionStrategy().ExecuteAsync`.
6. Measure first: enable EF Core instrumentation in OpenTelemetry and look at traces for endpoints with high query counts per request, or log SQL in lower environments. Add a command-counting interceptor to integration tests and assert query budgets for key endpoints so regressions fail CI. Disable lazy loading, then fix hot spots by replacing entity loading in loops with projections, `Include`, or batched `Contains` queries. Prioritize by production impact (request volume multiplied by extra round trips) and add the query-count tests as guards.
7. A `BackgroundService` is a singleton, so it must not capture a scoped `DbContext`; instead inject `IDbContextFactory<T>` or `IServiceScopeFactory` and create a fresh, short-lived context for each unit of work, such as each message or each timer tick, disposing it afterwards. For parallel work, give each concurrent operation its own context instance, because one context cannot run concurrent queries. Keep units of work small, pass cancellation tokens, handle transient failures with execution strategies, and make processing idempotent.
8. `ExecuteUpdate/ExecuteDelete` translate to a single set-based SQL statement, so they are dramatically faster and use far less memory for bulk changes, with no entity materialization or change tracking. The trade-offs are that they bypass the change tracker, so tracked entities in the context become stale; domain logic and invariants in entity methods do not run; `SaveChanges` interceptors, auditing hooks, and optimistic concurrency checks are not applied automatically; and cascade behavior depends on the database. Use them for bulk maintenance operations and keep entity-based updates for business operations that must enforce domain rules.
