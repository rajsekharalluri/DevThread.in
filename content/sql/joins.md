---
id: sql-joins
slug: joins
title: SQL Joins
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-select-filtering-sorting]
tags: [sql, joins, relationships, cardinality]
relatedTopics: [sql-group-aggregations, sql-query-examples]
order: 30
status: published
---
# SQL Joins

## Introduction
A join combines rows from two or more tables using a relationship. Joins are how a normalized database reconstructs useful business views without copying every fact into every table.

```text
Customers ──1──────< Orders ──1──────< OrderLines >────1── Products
```

The most important question before writing a join is: **what is one output row?** One customer, one order, one order line, or one customer/month? A join can change that grain.

## Purpose
Use joins to combine related data while preserving a single source of truth. The join type expresses what should happen when a relationship is missing.

```sql
SELECT o.Id, o.OrderNumber, c.Email
FROM Orders AS o
JOIN Customers AS c ON c.Id = o.CustomerId
WHERE o.Status = 'Paid';
```

## Join types

```text
INNER JOIN: only matching rows
Customers ── matches ── Orders

LEFT JOIN: every left row, matching right data when present
Customers ── matches or NULL ── Orders

CROSS JOIN: every possible combination; usually dangerous accidentally
```

```sql
-- Customers who have no orders: anti-join
SELECT c.Id, c.Email
FROM Customers AS c
LEFT JOIN Orders AS o ON o.CustomerId = c.Id
WHERE o.Id IS NULL;
```

## Real-World Simple Example
A customer with three orders produces three rows in a customer-to-orders join. If that result is joined to four order-line rows per order, the output can contain twelve rows for that customer. That multiplication is correct only if the output grain is order line.

```sql
SELECT o.Id AS OrderId, ol.ProductId, ol.Quantity
FROM Orders AS o
JOIN OrderLines AS ol ON ol.OrderId = o.Id;
```

## Professional company-level example
A dashboard may need one row per order, including the latest status. Joining all status-history rows would duplicate each order, so first reduce history to one row:

```sql
WITH LatestStatus AS
(
    SELECT OrderId, Status,
           ROW_NUMBER() OVER (
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

### Common join failure

```sql
-- Accidentally converts the LEFT JOIN into an INNER JOIN:
SELECT c.Id, o.Id
FROM Customers c
LEFT JOIN Orders o ON o.CustomerId = c.Id
WHERE o.Status = 'Paid';
```

Customers with no orders are removed because `o.Status` is `NULL`. Put the right-side filter in the `ON` clause when unmatched left rows must remain:

```sql
LEFT JOIN Orders o
  ON o.CustomerId = c.Id
 AND o.Status = 'Paid'
```

## Comparison
| Join | Keeps unmatched left rows? | Typical use |
|---|---:|---|
| `INNER JOIN` | No | Required relationship |
| `LEFT JOIN` | Yes | Optional child data |
| `FULL JOIN` | Both sides | Reconciliation |
| `CROSS JOIN` | All combinations | Deliberate matrix generation |
| `EXISTS` | Outer row if match exists | Existence checks without multiplying rows |
| `NOT EXISTS` | Outer row if no match | Anti-join, often safer with NULLs |

Join performance depends on indexes, cardinality, statistics, and the selected algorithm (nested loop, hash, or merge join). Index foreign keys and inspect actual row counts in the execution plan.

## Interview Questions
- **[L1]** What is the difference between `INNER JOIN` and `LEFT JOIN`?
- **[L1]** How do you find rows with no matching relationship?
- **[L2]** Why can a join multiply result rows unexpectedly?
- **[L2]** What is the difference between `JOIN` and `EXISTS` for an existence check?
- **[L3]** How do service boundaries change the way joins are designed?
- **[L3]** How would you troubleshoot a join that became slow after data growth?

## Interview Answers
1. `INNER JOIN` keeps only matches on both sides; `LEFT JOIN` keeps every left row and supplies `NULL` for missing right data.
2. Use a `LEFT JOIN` with a right-key `IS NULL` filter or use `NOT EXISTS`.
3. A one-to-many relationship creates one output row per matching child. Joining two child collections can create a multiplication of combinations.
4. `EXISTS` tests whether a match exists and does not return child rows, so it avoids accidental multiplication. A join is appropriate when child data is needed.
5. Separate service databases cannot join directly. Use APIs, events/read projections, or application composition with explicit consistency and latency trade-offs.
6. Confirm output grain, inspect actual/estimated row counts, verify join-key indexes/uniqueness, review join algorithm and data skew, and check authorization filters are not causing a large scan.

## Expert perspective
Join expertise is mostly cardinality reasoning. Before optimizing syntax, state the expected number of output rows, prove the relationship's uniqueness, and confirm that the join still respects ownership and authorization boundaries.
