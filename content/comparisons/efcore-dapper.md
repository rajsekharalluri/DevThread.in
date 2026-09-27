---
id: comparisons-efcore-dapper
slug: efcore-dapper
title: EF Core vs Dapper vs Raw SQL
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: ".NET 8+"
prerequisites: [dotnet-data-access, efcore-query-performance]
tags: [ef-core, dapper, sql, data-access, comparison]
relatedTopics: [dotnet-data-access, efcore-query-performance]
order: 20
status: published
---
# EF Core vs Dapper vs Raw SQL

## Decision table

| Concern | EF Core | Dapper | Raw provider SQL |
|---|---|---|---|
| Modeling/relationships | Strong | Manual | Manual |
| Change tracking | Built in | Manual | Manual |
| LINQ composition | Strong/provider translation | None | None |
| SQL control | Medium | High | Highest |
| Migrations | Supported | External | External |
| Typical fit | Domain persistence | Focused reads/reports | Special provider/tuned operation |

## Example

```csharp
// EF Core: model-aware query/projection
var summaries = await db.Orders
    .AsNoTracking()
    .Where(o => o.TenantId == tenantId)
    .Select(o => new OrderSummary(o.Id, o.Total))
    .ToListAsync(ct);

// Dapper: explicit parameterized SQL
var summaries = await connection.QueryAsync<OrderSummary>(
    "SELECT Id, Total FROM Orders WHERE TenantId = @TenantId",
    new { TenantId = tenantId });
```

EF handles translation/tracking/model conventions; Dapper maps the SQL result and leaves query/transaction/index decisions visible. Both can produce poor performance if rows, plans, and round trips are ignored.

## Choose EF Core when

- Aggregates/relationships need modeling.
- Change tracking/unit-of-work is useful.
- Team benefits from LINQ/migrations.
- Provider translation is tested.

## Choose Dapper/raw SQL when

- A report/projection needs precise SQL.
- Provider-specific features matter.
- A measured hot query needs explicit control.
- The team can own parameterization/tests/migrations.

## Interview Questions
- **[L1]** What is EF Core best at?
- **[L1]** What is Dapper best at?
- **[L2]** Does Dapper automatically make queries faster?
- **[L2]** What responsibility increases with raw SQL?
- **[L3]** How would you choose for a high-throughput service?
- **[L3]** Why should both approaches be hidden behind application boundaries?

## Interview Answers
1. Modeling entities/relationships, change tracking, unit-of-work persistence, LINQ, and migrations.
2. Explicit SQL mapping for focused queries/projections with little ORM overhead.
3. No; SQL shape, indexes, rows, plans, network, and materialization dominate.
4. Parameterization, SQL correctness, schema compatibility, transaction behavior, mapping, and integration testing.
5. Start from access patterns/consistency, measure, use EF by default where it fits, and specialize only measured queries.
6. Controllers/use cases should not own provider details; boundaries make testing, ownership, and replacement possible.
