---
id: sql-query-examples
slug: query-examples
title: Practical SQL Query Patterns
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 55
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-joins, sql-group-aggregations]
tags: [sql, examples, reporting, reconciliation, interviews]
relatedTopics: [sql-window-functions, sql-subqueries-ctes]
order: 120
status: published
---
# Practical SQL Query Patterns

## Introduction
Practical SQL is about translating a business question into a precise result grain, then choosing joins, filters, grouping, windows, and constraints that preserve correctness.

```text
Business question
What is one output row?
Tables + relationships
Filters + NULL rules
Query + plan
Test edge cases and counts
```

## Purpose
These patterns appear in APIs, reports, reconciliation jobs, interviews, and production troubleshooting. They are starting points, not copy/paste answers; adapt them to the schema, constraints, indexes, and database dialect.

## Real-World Simple Examples
### Duplicate values
```sql
SELECT Email, COUNT(*) AS Occurrences
FROM Customers
GROUP BY Email
HAVING COUNT(*) > 1;
```

### Rows without a relationship
```sql
SELECT c.Id, c.Email
FROM Customers AS c
WHERE NOT EXISTS
(
    SELECT 1 FROM Orders AS o WHERE o.CustomerId = c.Id
);
```

### Second-highest distinct value
```sql
SELECT MAX(Total)
FROM Orders
WHERE Total < (SELECT MAX(Total) FROM Orders);
```

### Top three per group
```sql
WITH Ranked AS
(
    SELECT p.*, ROW_NUMBER() OVER
    (
        PARTITION BY CategoryId
        ORDER BY Sales DESC, Id
    ) AS RowNumber
    FROM Products AS p
)
SELECT * FROM Ranked WHERE RowNumber <= 3;
```

## Professional company-level example
### Latest status per order

```sql
WITH LatestStatus AS
(
    SELECT OrderId, Status,
           ROW_NUMBER() OVER
           (
               PARTITION BY OrderId
               ORDER BY ChangedAt DESC, Id DESC
           ) AS RowNumber
    FROM OrderStatusHistory
)
SELECT o.Id, o.OrderNumber, s.Status
FROM Orders AS o
JOIN LatestStatus AS s
  ON s.OrderId = o.Id
 AND s.RowNumber = 1;
```

### Payment reconciliation

```sql
SELECT o.Id,
       o.Total,
       COALESCE(SUM(p.Amount), 0) AS CapturedAmount
FROM Orders AS o
LEFT JOIN Payments AS p
  ON p.OrderId = o.Id
 AND p.Status = 'Captured'
GROUP BY o.Id, o.Total
HAVING COALESCE(SUM(p.Amount), 0) <> o.Total;
```

This query identifies underpaid, overpaid, duplicated, or incorrectly recorded orders. A production reconciliation job should record findings, be rerunnable, and not silently modify financial data without an auditable correction process.

## Important rules

- Always define the output grain before writing a join.
- Treat `NULL` explicitly; do not assume it means zero.
- Use stable tie-breakers for pagination and ranking.
- Use `NOT EXISTS` when testing absence and nullable values may exist.
- Use parameters for external values.
- Do not use `DISTINCT` to hide a row-multiplication bug.
- Test duplicates, missing relations, empty input, ties, and time boundaries.

## Comparison
| Requirement | Useful pattern |
|---|---|
| Duplicate detection | `GROUP BY` + `HAVING` |
| No matching row | `NOT EXISTS` / anti-join |
| Latest row per entity | `ROW_NUMBER()` |
| Top N per group | Window function + outer filter |
| Aggregate report | `GROUP BY` |
| Compare related totals | `JOIN` + aggregate + `HAVING` |
| Multi-stage readability | CTE |

## Interview Questions
- **[L1]** How do you find duplicate values?
- **[L1]** How do you find rows without a matching relationship?
- **[L2]** How do you find the latest row per entity?
- **[L2]** Why can `DISTINCT` hide a real query bug?
- **[L3]** How do you review an unfamiliar SQL query before approving it?
- **[L3]** How would you turn a repeated expensive query into a reliable read model?

## Interview Answers
1. Group by the value and use `HAVING COUNT(*) > 1`.
2. Use `NOT EXISTS` or a left anti-join and filter the right key as `NULL`.
3. Use `ROW_NUMBER()` partitioned by the entity and ordered by the latest timestamp plus a deterministic ID tie-breaker.
4. It removes duplicate output after a one-to-many join, hiding the fact that the query's grain is wrong and potentially masking missing business logic.
5. State intended grain, test edge cases, inspect every join's cardinality, verify authorization predicates, review NULL/time behavior, and inspect the execution plan at realistic scale.
6. Define source-of-truth ownership, update it idempotently from events/transactions, expose freshness/lag, provide reconciliation and rebuild, and test out-of-order/duplicate events.

## Expert perspective
Strong SQL answers begin with the business question and row grain, not clever syntax. The best production query is correct under messy data, secure for every tenant, measurable at scale, and understandable during an incident.
