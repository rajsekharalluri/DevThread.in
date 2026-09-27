---
id: sql-subqueries-ctes
slug: subqueries-ctes
title: Subqueries and Common Table Expressions
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Relational SQL"
prerequisites: [sql-joins, sql-group-aggregations]
tags: [sql, subquery, cte, recursive, readability]
relatedTopics: [sql-window-functions, sql-query-examples]
order: 50
status: published
---
# Subqueries and Common Table Expressions

## Introduction
A subquery is a query inside another query. A Common Table Expression (CTE) names an intermediate query using `WITH`, making multi-stage SQL easier to read and enabling recursive structures such as organization hierarchies.

```text
Source rows
   ↓
Intermediate query/subquery/CTE
   ↓
Join, filter, rank, or aggregate
   ↓
Final result
```

## Purpose
Use subqueries and CTEs when a business question naturally has stages: calculate customer totals, find the latest record, test existence, or walk a hierarchy.

## Real-World Simple Example
```sql
WITH CustomerSpend AS
(
    SELECT CustomerId, SUM(Total) AS PaidTotal
    FROM Orders
    WHERE Status = 'Paid'
    GROUP BY CustomerId
)
SELECT c.Email, s.PaidTotal
FROM CustomerSpend AS s
JOIN Customers AS c ON c.Id = s.CustomerId
WHERE s.PaidTotal > @threshold;
```

The CTE gives the aggregation a name and a clear result grain: one row per customer.

## Professional company-level example
A recursive CTE can produce an employee hierarchy, category tree, or bill of materials:

```sql
WITH OrgChart AS
(
    SELECT Id, Name, ManagerId, 1 AS Depth
    FROM Employees
    WHERE ManagerId IS NULL

    UNION ALL

    SELECT e.Id, e.Name, e.ManagerId, o.Depth + 1
    FROM Employees AS e
    JOIN OrgChart AS o ON e.ManagerId = o.Id
    WHERE o.Depth < 20
)
SELECT Id, Name, ManagerId, Depth
FROM OrgChart
ORDER BY Depth, Name;
```

Production code needs depth limits, cycle protection, authorization filters, and tests for malformed relationships.

## Important behavior
A CTE is normally a named query expression, not automatically a cached temporary table. The optimizer may inline it, reorder operations, or materialize/spool it depending on the engine and plan. Do not assume a CTE is faster merely because it is easier to read.

`EXISTS` tests whether at least one row exists and may stop early. `NOT EXISTS` is usually safer than `NOT IN` when NULL values can occur, because SQL's three-valued logic makes `NOT IN` surprisingly return no rows in some cases.

## Common failure scenario
A correlated subquery can run once per outer row:

```sql
SELECT c.Id,
       (SELECT SUM(o.Total)
        FROM Orders o
        WHERE o.CustomerId = c.Id) AS Total
FROM Customers c;
```

If the optimizer cannot transform it effectively, a pre-aggregated CTE and join may be cheaper. Verify using the actual execution plan, not syntax preference.

## Comparison
| Tool | Useful for | Watch for |
|---|---|---|
| Scalar subquery | One calculated value | Repeated execution |
| `EXISTS` | Presence test | Correlation/indexing |
| Derived table | Local intermediate result | Readability/nesting |
| CTE | Named multi-stage query | Not automatically materialized |
| Recursive CTE | Hierarchies | Cycles/depth/cost |
| Window function | Detail plus rank/aggregate | Sort/memory cost |

## Interview Questions
- **[L1]** What is a subquery?
- **[L1]** What is a CTE?
- **[L2]** What is the difference between `EXISTS` and `IN`?
- **[L2]** Does a CTE always materialize as a temporary table?
- **[L3]** How would you diagnose a slow correlated subquery?
- **[L3]** When should a repeated CTE-based query become a view or read model?

## Interview Answers
1. A subquery is a nested `SELECT` used as a value, table-like source, or condition inside a larger query.
2. A CTE names a query expression with `WITH` and can make multi-stage or recursive logic clearer.
3. `EXISTS` tests for any matching row and can short-circuit; `IN` compares against a set of values and has NULL pitfalls with `NOT IN`.
4. No. The optimizer may inline, reorder, or spool it. Check the execution plan.
5. Inspect actual execution counts/loops, indexes, row estimates, and rewrite it as a pre-aggregated join if it is executing repeatedly.
6. When the logic is copied across consumers or expensive enough to justify a stable/materialized projection with ownership and freshness rules.

## Expert perspective
CTEs improve the human structure of a query, but the database plan determines runtime behavior. Senior engineers use CTEs for intent and inspect plans for cost; they never confuse readable SQL with automatically efficient SQL.
