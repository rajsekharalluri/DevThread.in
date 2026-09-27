---
id: sql-window-functions
slug: window-functions
title: SQL Window Functions
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Relational SQL"
prerequisites: [sql-group-aggregations, sql-subqueries-ctes]
tags: [sql, windows, ranking, lag, lead, analytics]
relatedTopics: [sql-query-examples, sql-indexes-performance]
order: 60
status: published
---
# SQL Window Functions

## Introduction
A window function calculates across related rows while keeping one output row per input row. This differs from `GROUP BY`, which collapses many rows into one row per group.

```text
GROUP BY:
10 order rows → 1 customer summary row

WINDOW FUNCTION:
10 order rows → 10 order rows + rank/running total/previous value
```

## Purpose
Use windows for ranking, latest-row selection, running totals, trends, comparisons with previous/next rows, and top-N-per-group queries.

## Real-World Simple Example
```sql
SELECT CustomerId,
       OrderId,
       CreatedAt,
       Total,
       ROW_NUMBER() OVER
       (
           PARTITION BY CustomerId
           ORDER BY CreatedAt DESC, OrderId DESC
       ) AS RecencyNumber
FROM Orders;
```

To retrieve the latest order per customer, wrap this query and filter `RecencyNumber = 1` in the outer query.

## Professional company-level example
A payment-risk analysis can compare each transaction with the previous transaction:

```sql
SELECT CustomerId,
       TransactionId,
       Amount,
       CreatedAt,
       LAG(Amount) OVER
       (
           PARTITION BY CustomerId
           ORDER BY CreatedAt, TransactionId
       ) AS PreviousAmount,
       SUM(Amount) OVER
       (
           PARTITION BY CustomerId
           ORDER BY CreatedAt, TransactionId
           ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
       ) AS RunningSpend
FROM Payments
WHERE CreatedAt >= @periodStart;
```

The `PARTITION BY` group is the customer. The window `ORDER BY` defines sequence. A deterministic ID tie-breaker is important when timestamps are equal.

## Ranking choices

```text
ROW_NUMBER  → always unique: 1,2,3,4
RANK        → ties share rank and leave gaps: 1,2,2,4
DENSE_RANK  → ties share rank without gaps: 1,2,2,3
```

Choose based on business meaning. For “exactly three rows,” `ROW_NUMBER` with a stable tie-breaker is usually appropriate. For competition ranking, `RANK` may be correct.

## Performance and failure scenarios
Window functions often need sorting or partition state. Large partitions can consume memory or spill to disk. Multiple windows with different orderings may require multiple sorts. Filter before windowing when semantics permit, and use an index supporting partition/order columns when measured beneficial.

Do not expose ranks calculated over unauthorized rows. Apply tenant/authorization filters before calculating the window, otherwise the rank itself can leak information.

## Comparison
| Need | Use |
|---|---|
| Collapse rows to summary | `GROUP BY` |
| Keep rows + ranking | `ROW_NUMBER`/`RANK` |
| Previous/next comparison | `LAG`/`LEAD` |
| Running total | `SUM(...) OVER` + frame |
| Latest row per group | `ROW_NUMBER` + outer filter |
| Top N per group | Window rank + outer filter |

## Interview Questions
- **[L1]** What does `PARTITION BY` do?
- **[L1]** How is a window function different from `GROUP BY`?
- **[L2]** Compare `ROW_NUMBER`, `RANK`, and `DENSE_RANK`.
- **[L2]** What is a window frame?
- **[L3]** How would you optimize a window query over a very large table?
- **[L3]** How do you ensure ranking does not leak unauthorized information?

## Interview Answers
1. It divides rows into independent groups; the window calculation resets for each partition.
2. `GROUP BY` collapses rows; a window function keeps the original row grain and adds a calculated value.
3. `ROW_NUMBER` is unique, `RANK` leaves gaps after ties, and `DENSE_RANK` does not leave gaps.
4. A frame defines which ordered rows are included for the current row, such as from the partition start through the current row for a running total.
5. Filter early, support partition/order columns with suitable indexes, reduce unnecessary windows/sorts, inspect memory grants/spills, and move analytical work to a read/warehouse workload if needed.
6. Apply authorization and tenant filters before computing the window so the ranking population contains only permitted rows.

## Expert perspective
Window functions are powerful because they preserve detail while adding analysis. Senior engineers still reason about partition cardinality, deterministic ordering, frame semantics, memory/sort cost, and security population before using them in a production endpoint.
