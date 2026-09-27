---
id: sql-group-aggregations
slug: group-aggregations
title: GROUP BY, HAVING, and Aggregations
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 45
version:
  minimum: "Relational SQL"
prerequisites: [sql-select-filtering-sorting, sql-joins]
tags: [sql, group-by, count, sum, having, reporting]
relatedTopics: [sql-window-functions, sql-subqueries-ctes]
order: 40
status: published
---
# GROUP BY, HAVING, and Aggregations

## Introduction
Aggregation turns many rows into useful measures: count, total, average, minimum, maximum, or a custom business calculation. `GROUP BY` defines the result grain; `WHERE` filters rows before grouping; `HAVING` filters groups after aggregation.

```text
Rows
  ↓ WHERE filters individual rows
Filtered rows
  ↓ GROUP BY forms groups
Groups
  ↓ COUNT/SUM/AVG calculates measures
Aggregated result
  ↓ HAVING filters groups
Final report
```

## Purpose
Use aggregation for reports, quotas, dashboards, reconciliation, billing, and business rules that depend on a set of rows.

## Real-World Simple Example
```sql
SELECT CustomerId,
       COUNT(*) AS OrderCount,
       SUM(Total) AS LifetimeValue
FROM Orders
WHERE Status = 'Paid'
GROUP BY CustomerId
HAVING SUM(Total) >= @minimumValue
ORDER BY LifetimeValue DESC;
```

The output grain is one row per customer. Every selected column must either be part of the grouping key or be aggregated.

## Professional company-level example
A daily sales report may be calculated live for a small dataset or precomputed for a high-volume dashboard:

```sql
SELECT CAST(CreatedAt AS date) AS SalesDate,
       COUNT(*) AS OrderCount,
       SUM(Total) AS GrossRevenue
FROM Orders
WHERE Status = 'Paid'
  AND CreatedAt >= @fromUtc
  AND CreatedAt < @toUtc
GROUP BY CAST(CreatedAt AS date)
ORDER BY SalesDate;
```

For a large system, move repeated expensive reporting to a replica, warehouse, or summary table. Document freshness and correction behavior.

## Important behavior

```sql
-- Wrong: aggregate does not exist yet in WHERE
WHERE SUM(Total) > 1000

-- Correct: aggregate exists when HAVING runs
HAVING SUM(Total) > 1000
```

`COUNT(*)` counts rows. `COUNT(column)` ignores NULL values. `SUM`/`AVG` can return NULL when there are no non-null values, so use `COALESCE` only when treating missing data as zero is actually correct.

## Performance and failure scenarios
Large hash aggregates need memory. Bad cardinality estimates can cause spills to temporary disk. Filter early, select only needed columns, index access predicates, and do not run expensive reports on a primary transactional database during peak traffic without a plan.

A common financial bug is aggregating a many-to-many join without first defining the grain, double-counting revenue. Test with duplicate child rows, NULLs, empty groups, and boundary timestamps.

## Comparison
| Need | Tool |
|---|---|
| One row per group | `GROUP BY` |
| Filter raw rows | `WHERE` |
| Filter aggregate groups | `HAVING` |
| Keep detail rows plus calculations | Window function |
| Repeated expensive report | Summary/materialized read model |

## Interview Questions
- **[L1]** What does `GROUP BY` do?
- **[L1]** What is the difference between `WHERE` and `HAVING`?
- **[L2]** Why can `COUNT(*)` and `COUNT(column)` return different results?
- **[L2]** How can an aggregation spill to disk?
- **[L3]** When should a report be precomputed instead of calculated live?
- **[L3]** How do you prevent double-counting in an aggregate query with multiple joins?

## Interview Answers
1. It creates one output group for each unique combination of the specified grouping columns.
2. `WHERE` filters individual rows before grouping; `HAVING` filters groups after aggregate values are calculated.
3. `COUNT(*)` includes rows even when a particular column is NULL; `COUNT(column)` counts only non-NULL values.
4. The engine estimates a memory grant for a hash/sort aggregate. More groups or larger state than estimated can exceed memory and spill to disk.
5. Precompute when the report is expensive, frequent, and can tolerate documented staleness. Use live queries when freshness and workload cost are acceptable.
6. Define the intended grain, aggregate each child relationship separately when necessary, inspect row counts after every join, and test data with duplicates.

## Expert perspective
Aggregation correctness is primarily a grain problem. Senior engineers make row grain, NULL semantics, time zone, currency, freshness, and workload isolation explicit before discussing SQL syntax.
