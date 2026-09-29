---
id: comparisons-efcore-dapper
slug: efcore-dapper
title: EF Core vs Dapper vs Raw SQL
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: ".NET 8+, EF Core 8+, Dapper 2.x"
prerequisites: [dotnet-data-access, efcore-query-performance]
tags: [ef-core, dapper, sql, data-access, comparison]
relatedTopics: [dotnet-data-access, efcore-query-performance]
order: 20
status: published
---
# EF Core vs Dapper vs Raw SQL

## Introduction

.NET teams usually choose among three data-access styles:

- **EF Core** - a full ORM: maps entities and relationships, translates LINQ to SQL, tracks changes, manages migrations, and supports unit-of-work persistence.
- **Dapper** - a micro-ORM: you write the SQL; Dapper executes it with parameters and maps result rows to objects very efficiently.
- **Raw ADO.NET / provider SQL** - direct use of `DbConnection`, `DbCommand`, and provider-specific features (bulk copy, `COPY`, table-valued parameters) for maximum control.

These are not mutually exclusive. A common, healthy architecture uses **EF Core for the domain write model** and **Dapper or raw SQL for specific read models, reports, and measured hot paths.**

## Quick Decision Table

| Concern | EF Core | Dapper | Raw ADO.NET / Provider SQL |
|---|---|---|---|
| SQL authorship | Generated from LINQ (can drop to SQL) | You write it | You write it |
| Mapping | Entities, relationships, owned types, conversions | Rows to POCOs, multi-mapping | Manual |
| Change tracking / unit of work | Built in | None | None |
| Migrations | Built in | External (DbUp, Flyway, EF migrations) | External |
| Performance overhead | Low-moderate (compiled queries, no-tracking help) | Very low | Lowest |
| Provider-specific features | Partial | Any SQL you can write | Everything (bulk copy, COPY, TVPs) |
| Productivity for CRUD | High | Medium | Low |
| Testing | In-memory providers are misleading; use real DB (Testcontainers) | Real DB integration tests | Real DB integration tests |
| Typical fit | Aggregates, business writes, standard queries | Reports, dashboards, read models, hot queries | Bulk loads, special provider operations |

## EF Core

### When to Use

- Domain models with aggregates, relationships, and invariants
- Standard CRUD and business transactions
- Teams that benefit from LINQ composition, migrations, and conventions

### When Not to Use (Alone)

- Complex reporting SQL (window functions, CTEs) that LINQ expresses poorly
- Bulk operations on millions of rows (use `ExecuteUpdate/ExecuteDelete` or provider bulk tools)
- Hot paths where measured translation or materialization overhead matters

### Example

```csharp
// Write: load aggregate, apply behavior, save - change tracking generates the UPDATE
var order = await db.Orders.Include(o => o.Lines).SingleAsync(o => o.Id == id && o.TenantId == tenantId, ct);
order.AddLine(productId, quantity, unitPrice);
await db.SaveChangesAsync(ct);

// Read: no tracking + projection to fetch only needed columns
var page = await db.Orders
    .AsNoTracking()
    .Where(o => o.TenantId == tenantId && o.CreatedAt >= from)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new OrderSummary(o.Id, o.Status, o.Total, o.Lines.Count))
    .Take(50)
    .ToListAsync(ct);

// Set-based update without loading entities (EF Core 7+)
await db.Orders.Where(o => o.Status == OrderStatus.Placed && o.CreatedAt < cutoff)
    .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, OrderStatus.Cancelled), ct);
```

**Why each part exists:** tracking is used only where you modify data; `AsNoTracking` plus projection avoids loading full entities for reads; `ExecuteUpdateAsync` performs a single SQL `UPDATE` instead of loading and saving thousands of rows.

## Dapper

### When to Use

- Read models, reports, and dashboards where you want exact SQL
- Queries using window functions, CTEs, or database-specific syntax
- Measured hot queries where you need predictable SQL and minimal overhead
- Legacy databases with schemas that do not map well to an ORM

### When Not to Use

- Complex aggregate persistence (you would re-implement change tracking by hand)
- Teams that will skip parameterization or integration tests

### Example

```csharp
await using var connection = new NpgsqlConnection(connectionString);

const string sql = """
    SELECT c.id AS CustomerId, c.name AS Name,
           COUNT(o.id) AS OrderCount,
           SUM(o.total) AS LifetimeValue,
           RANK() OVER (ORDER BY SUM(o.total) DESC) AS ValueRank
    FROM customers c
    JOIN orders o ON o.customer_id = c.id AND o.status IN ('paid', 'shipped')
    WHERE c.tenant_id = @TenantId AND o.created_at >= @From
    GROUP BY c.id, c.name
    ORDER BY LifetimeValue DESC
    LIMIT @Take
    """;

var topCustomers = await connection.QueryAsync<CustomerValue>(
    new CommandDefinition(sql, new { TenantId = tenantId, From = from, Take = 20 }, cancellationToken: ct));
```

**Why each part exists:** parameters (`@TenantId`) prevent SQL injection and enable plan reuse; `CommandDefinition` passes cancellation; the window function is natural in SQL and awkward in LINQ.

## Raw ADO.NET / Provider Features

### When to Use

- Bulk loading millions of rows
- Provider features no abstraction exposes well

### Example: PostgreSQL Binary COPY

```csharp
await using var connection = new NpgsqlConnection(connectionString);
await connection.OpenAsync(ct);
await using var writer = await connection.BeginBinaryImportAsync(
    "COPY events (id, tenant_id, type, payload, occurred_at) FROM STDIN (FORMAT BINARY)", ct);
foreach (var e in events)
{
    await writer.StartRowAsync(ct);
    await writer.WriteAsync(e.Id, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(e.TenantId, NpgsqlDbType.Uuid, ct);
    await writer.WriteAsync(e.Type, NpgsqlDbType.Text, ct);
    await writer.WriteAsync(e.PayloadJson, NpgsqlDbType.Jsonb, ct);
    await writer.WriteAsync(e.OccurredAt, NpgsqlDbType.TimestampTz, ct);
}
await writer.CompleteAsync(ct);
```

`COPY` can be orders of magnitude faster than row-by-row inserts for large loads.

## Using Both in One Application

```csharp
builder.Services.AddDbContextPool<OrdersDbContext>(o => o.UseNpgsql(cs));   // writes and standard queries
builder.Services.AddScoped<IReportQueries, DapperReportQueries>();          // Dapper for reports

public sealed class DapperReportQueries(OrdersDbContext db) : IReportQueries
{
    // Reuse EF's connection so both participate in the same transaction when needed
    public async Task<IReadOnlyList<CustomerValue>> TopCustomersAsync(Guid tenantId, DateTime from, CancellationToken ct)
    {
        var connection = db.Database.GetDbConnection();
        var rows = await connection.QueryAsync<CustomerValue>(
            new CommandDefinition(TopCustomersSql, new { TenantId = tenantId, From = from, Take = 20 },
                transaction: db.Database.CurrentTransaction?.GetDbTransaction(), cancellationToken: ct));
        return rows.AsList();
    }
}
```

Keep both behind application-level interfaces (`IOrderRepository`, `IReportQueries`) so controllers and use cases never depend on the data-access technology.

## Same Scenario: Order Management Service

| Operation | Choice | Reason |
|---|---|---|
| Place order, add lines, change status | EF Core | Aggregate behavior + change tracking + transactions |
| Order list page with filters | EF Core `AsNoTracking` projection | Composable LINQ, sufficient performance |
| Finance dashboard with window functions | Dapper | Exact SQL, readable |
| Cancel 200,000 stale orders | EF Core `ExecuteUpdateAsync` | Single set-based statement |
| Import 5 million historical events | Raw provider `COPY` / `SqlBulkCopy` | Bulk throughput |

## Decision Matrix

| Factor | EF Core | Dapper | Raw SQL |
|---|---|---|---|
| Developer productivity (CRUD) | Strong | Medium | Weak |
| SQL control and predictability | Medium | Strong | Strong |
| Raw performance | Medium-Strong | Strong | Strongest |
| Domain modeling | Strong | Weak | Weak |
| Schema migrations | Strong | External | External |
| Risk of hidden inefficiency | Medium (N+1, over-fetching, bad translation) | Low (SQL is visible) | Low |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| "Dapper is faster, so rewrite everything" | Measure; most latency is SQL plan, indexes, and round trips |
| N+1 queries with lazy loading in EF | Projections, `Include` deliberately, or split queries |
| Tracking queries for read-only pages | `AsNoTracking` + projection |
| String-concatenated SQL in Dapper | Always parameters |
| Testing EF with the InMemory provider | Integration tests against the real database (Testcontainers) |
| Data-access types leaking into controllers | Application interfaces; keep persistence behind boundaries |

## Interview Questions
- **[L1]** What is EF Core best at?
- **[L1]** What is Dapper best at?
- **[L2]** Does Dapper automatically make queries faster?
- **[L2]** What responsibility increases with raw SQL?
- **[L3]** How would you choose for a high-throughput service?
- **[L3]** Why should both approaches be hidden behind application boundaries?

## Interview Answers
1. EF Core is best at persisting domain models: mapping entities, relationships, owned types, and value conversions; tracking changes so a unit of work saves only what changed in one transaction; composing strongly typed LINQ queries; and managing schema migrations. It gives high productivity for business writes and standard queries, with tools like `AsNoTracking`, projections, compiled queries, and `ExecuteUpdate/ExecuteDelete` for efficiency.
2. Dapper is best at executing hand-written, parameterized SQL and mapping the results to objects with minimal overhead. It shines for read models, reports, dashboards, and queries using window functions, CTEs, or provider-specific syntax, where the team wants the exact SQL to be visible, reviewable, and predictable.
3. No. Dapper reduces mapping and ORM overhead, which is usually a small part of total query time. Performance is dominated by the SQL itself and its execution plan, indexes, the number of rows scanned and returned, round trips, network latency, locking, and materialization. A poorly written Dapper query is slower than a well-shaped EF Core projection. Measure with execution plans and profiling before switching tools.
4. With raw SQL the team owns everything the ORM would otherwise help with: always parameterizing input to prevent SQL injection, keeping SQL in sync with schema changes and migrations, mapping columns to types correctly, managing connections and transactions, handling provider differences, and writing integration tests against a real database because the compiler cannot validate SQL strings.
5. Start from access patterns and measurements rather than assumptions. Use EF Core for the write model and most queries with pooled DbContexts, no-tracking projections, compiled queries for very hot paths, and set-based operations. Profile under realistic load; where specific queries are proven hot or need complex SQL, move them to Dapper or raw SQL behind the same interfaces. Also address the dominant factors: indexes, query shape, batching, caching, connection pooling, and read replicas.
6. Controllers and use cases should depend on intent-revealing application interfaces such as `IOrderRepository` or `IReportQueries`, not on DbContexts, connections, or SQL strings. That keeps business logic independent of the persistence technology, makes it possible to optimize or replace a single query implementation (EF to Dapper) without touching callers, centralizes concerns like tenant filtering and transactions, and makes testing and code ownership clearer.
