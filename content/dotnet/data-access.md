---
id: dotnet-data-access
slug: data-access
title: EF Core, Dapper, LINQ, and Production Data Access
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 65
version:
  minimum: ".NET 8+ / EF Core 8+"
prerequisites: [csharp-linq, sql-fundamentals]
tags: [dotnet, ef-core, dapper, linq, sql, performance]
relatedTopics: [sql-indexes-performance, dotnet-aspnet-core-backend]
order: 20
status: published
---
# EF Core, Dapper, LINQ, and Production Data Access

## Introduction
EF Core and Dapper solve different parts of the data-access problem. EF Core maps .NET models, tracks changes, translates LINQ, and manages migrations/unit-of-work behavior. Dapper maps explicit parameterized SQL to objects with minimal abstraction.

```text
Application use case
      ↓
Data-access boundary
      ├── EF Core: aggregate persistence/modeling
      └── Dapper: explicit SQL/projections/specialized reads
      ↓
Connection pool → database → execution plan
```

## Purpose
Data access should make transactions, query shape, authorization, connection lifetime, and performance visible without spreading provider details through controllers and business code.

## Real-World Simple Examples
### EF Core read projection

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Where(o => o.TenantId == tenantId && o.Status == OrderStatus.Paid)
    .OrderByDescending(o => o.CreatedAt)
    .Select(o => new OrderSummary(o.Id, o.Total, o.CreatedAt))
    .Take(100)
    .ToListAsync(ct);
```

### Dapper parameterized SQL

```csharp
const string sql = """
    SELECT Id, Total, CreatedAt
    FROM Orders
    WHERE TenantId = @TenantId AND Status = @Status
    ORDER BY CreatedAt DESC;
    """;

var rows = await connection.QueryAsync<OrderSummary>(
    sql, new { TenantId = tenantId, Status = "Paid" });
```

## EF Core behavior

```text
LINQ expression
      ↓
Expression tree
      ↓
Provider translates supported expressions to SQL
      ↓
Database executes SQL
      ↓
EF materializes/projections result
```

`ToListAsync`, `FirstAsync`, `CountAsync`, and similar terminal methods execute a query. Calling `ToList()` early moves later filtering to memory and can load an entire table.

Tracking keeps identity/original-value information so changes can be detected. `AsNoTracking()` is normally better for read-only DTO projections. Lazy loading can hide N+1 queries; explicit projections or deliberate includes make data access easier to see.

## Professional company-level example
An order command may use EF Core transaction behavior:

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(ct);

order.Submit();
db.Orders.Update(order);

await db.OutboxMessages.AddAsync(
    OutboxMessage.For("OrderSubmitted", order.Id), ct);

await db.SaveChangesAsync(ct);
await transaction.CommitAsync(ct);
```

A reporting endpoint may use Dapper for a tuned, database-specific query. Both are behind an application boundary so the controller does not decide tracking, SQL, or transaction behavior.

## N+1 example

```csharp
// Dangerous: one query for orders + one query per order.
var orders = await db.Orders.ToListAsync(ct);
foreach (var order in orders)
    Console.WriteLine(order.Customer.Email); // may trigger a query per iteration
```

Better:

```csharp
var rows = await db.Orders
    .Select(o => new { o.Id, CustomerEmail = o.Customer.Email })
    .ToListAsync(ct);
```

## EF Core versus Dapper

| Concern | EF Core | Dapper |
|---|---|---|
| SQL abstraction | LINQ/provider translation | SQL written explicitly |
| Change tracking | Built in | Manual |
| Migrations | Supported | External migration tool/process |
| Query control | Provider-dependent | High |
| Boilerplate | Lower for models/CRUD | Low mapping, more SQL ownership |
| Main risk | Hidden query/materialization cost | Manual SQL correctness/security |

Neither is automatically faster. Query plan, indexes, rows returned, allocations, and round trips matter more than ORM branding.

## Important production rules

- Keep `DbContext` scoped and never share it across concurrent operations.
- Pass `CancellationToken` to database calls.
- Project only needed columns.
- Parameterize every value.
- Keep transactions short.
- Use integration tests against the real database engine.
- Make tenant/authorization predicates unavoidable.
- Inspect generated SQL and execution plans.
- Treat migrations as deployable production code.

## Interview Questions
- **[L1]** When would you use EF Core versus Dapper?
- **[L1]** What does `AsNoTracking()` do?
- **[L2]** How does LINQ become SQL, and where can translation fail?
- **[L2]** What causes the N+1 query problem?
- **[L3]** How would you design data access for a high-throughput service?
- **[L3]** How do you test data-access code realistically?

## Interview Answers
1. EF Core fits domain persistence, relationships, migrations, and change tracking; Dapper fits explicit SQL and specialized projections. Choose from measured requirements.
2. It avoids EF change tracking, reducing memory/CPU for read-only results. It does not make a bad SQL query fast.
3. EF builds an expression tree and the provider translates supported expressions. Unsupported methods, provider differences, or premature materialization can fail or move work to memory.
4. N+1 is one parent query followed by one child query per parent, often caused by lazy loading in a loop. Use projection, explicit loading, or a deliberate join/batch.
5. Start with access patterns and consistency needs, isolate data access behind an application boundary, measure generated SQL/plans/allocations, and tune indexes/transactions before changing tools.
6. Unit-test policies with fakes, but integration-test queries, migrations, constraints, transactions, concurrency, and provider-specific translation against the real supported database.

## Expert perspective
Data access is not a thin CRUD detail. It is where object graphs, SQL plans, transactions, connection pools, authorization, and production load meet. Senior engineers make that boundary visible and measurable.
