---
id: sql-fundamentals
slug: fundamentals
title: SQL Fundamentals
category: sql
categoryTitle: SQL / Databases
difficulty: beginner
estimatedMinutes: 45
version:
  minimum: "Relational SQL"
prerequisites: []
tags: [sql, databases, relational, querying]
relatedTopics: [sql-select-filtering-sorting, sql-joins, sql-transactions-isolation]
order: 10
status: published
---
# SQL Fundamentals

## Introduction
SQL is a declarative language for defining, reading, and changing data in relational databases. You describe the result you need; the database optimizer chooses a physical plan using scans, seeks, joins, sorts, indexes, statistics, memory, and disk I/O.

```text
Application
    │ parameterized SQL
    ▼
Database parser → optimizer → execution plan
                              ├── index seek/scan
                              ├── join
                              ├── filter/sort
                              └── result
```

A table stores rows with typed columns. A primary key identifies a row; foreign keys connect tables; constraints protect valid states; transactions group changes safely.

## Purpose
Relational databases exist when correctness, relationships, transactions, durability, and flexible queries matter. SQL gives multiple applications a shared way to read and change the same governed data without putting every integrity rule only in application code.

## Real-World Simple Example
```sql
CREATE TABLE Orders (
    Id         bigint PRIMARY KEY,
    CustomerId bigint NOT NULL,
    Status     varchar(20) NOT NULL,
    Total      decimal(19,4) NOT NULL CHECK (Total >= 0),
    CreatedAt  timestamp NOT NULL
);

SELECT Id, Status, Total
FROM Orders
WHERE CustomerId = @customerId
  AND Status = 'Pending'
ORDER BY CreatedAt DESC, Id DESC;
```

The query is parameterized, returns only required columns, filters by ownership, and uses a deterministic ordering.

## Professional company-level example
A production API should combine:

```text
Request validation
    ↓
Tenant/authorization predicate
    ↓
Parameterized query
    ↓
Projection to DTO
    ↓
Bounded pagination
    ↓
Metrics: duration, rows, logical reads, errors
```

```csharp
var orders = await db.Orders
    .AsNoTracking()
    .Where(o => o.TenantId == tenantId && o.Status == OrderStatus.Pending)
    .OrderByDescending(o => o.CreatedAt)
    .ThenByDescending(o => o.Id)
    .Select(o => new OrderSummary(o.Id, o.Total, o.CreatedAt))
    .Take(100)
    .ToListAsync(cancellationToken);
```

The API does not load every entity, trust a user-supplied tenant ID, or return an unbounded result set.

## Important ideas

### Logical versus physical execution
The logical query says `FROM`, `WHERE`, `SELECT`, and `ORDER BY`; the optimizer may execute a different physical order if it is cheaper. A query that returns correct data can still be operationally bad if it scans a billion-row table or sorts a huge result in memory.

### NULL
`NULL` means unknown/missing, not zero or an empty string. Comparisons involving `NULL` use three-valued logic:

```sql
WHERE ClosedAt = NULL       -- wrong: never true
WHERE ClosedAt IS NULL      -- correct
```

### Transactions
A transaction provides a boundary for atomic changes. It does not automatically make a workflow across multiple databases/services atomic.

### Security
Always use parameters. Give the application least-privilege database access. Never expose raw SQL errors, credentials, or unrestricted administrative access through an API.

## Comparison
| Need | SQL capability |
|---|---|
| One record identity | Primary key |
| Cross-table relationship | Foreign key + join |
| Valid state | `NOT NULL`, `CHECK`, `UNIQUE` |
| Atomic changes | Transaction |
| Fast selective read | Appropriate index |
| Flexible result | `SELECT`, joins, CTEs, windows |
| Many independent reactions | Event/outbox instead of long transaction |

## Interview Questions
- **[L1]** What is SQL and how is it different from imperative code?
- **[L1]** What are primary keys, foreign keys, and constraints used for?
- **[L2]** Why can a correct SQL query still be a production performance problem?
- **[L2]** What is the difference between `NULL`, zero, and an empty string?
- **[L3]** When would you choose a relational database instead of a document or graph database?
- **[L3]** How would you troubleshoot a query that became slow after data growth?

## Interview Answers
1. SQL is declarative: it describes the result, while the database chooses execution steps. Imperative code explicitly controls the steps.
2. A primary key identifies rows, a foreign key enforces relationships, and constraints prevent invalid data regardless of which application writes it.
3. It may scan too many rows, sort large data, use a bad plan/statistics estimate, create too many round trips, or return more columns/rows than needed.
4. `NULL` represents unknown/missing and requires `IS NULL`/`IS NOT NULL`; zero is a numeric value and an empty string is a string value.
5. Use relational storage when transactions, relationships, constraints, and flexible queries are central. Choose another model only when its access pattern and operational trade-offs justify it.
6. Capture the actual execution plan, compare estimated/actual rows, inspect indexes and statistics, check sargability, parameter sensitivity, logical reads, locks, and data volume, then measure a targeted fix.

## Expert perspective
Good SQL is not just syntax that returns the expected rows. It has a defined row grain, security predicates, transaction behavior, bounded resource cost, a plan that works at realistic scale, and an operational owner who can diagnose it when the workload changes.
