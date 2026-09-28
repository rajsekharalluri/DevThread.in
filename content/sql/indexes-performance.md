---
id: sql-indexes-performance
slug: indexes-performance
title: SQL Indexes and Query Performance
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-joins]
tags: [sql, indexes, execution-plans, performance, sargability]
relatedTopics: [sql-transactions-isolation, sql-select-filtering-sorting]
order: 90
status: published
---
# SQL Indexes and Query Performance

## Introduction
An index is an additional data structure that helps a database locate rows or return them in order without scanning the entire table. An index is not free: every insert/update/delete may need to maintain it, and it consumes storage and memory.

```text
Query
Optimizer + statistics
  ├── table/index scan
  ├── index seek
  ├── lookup
  ├── join
  └── sort
```

## Purpose
Indexes reduce I/O for known access patterns. Query performance work means understanding the actual plan and data distribution, not adding indexes by intuition.

## Real-World Simple Example
```sql
CREATE INDEX IX_Orders_Customer_Created
ON Orders (CustomerId, CreatedAt DESC)
INCLUDE (Status, Total);
```

This can support:

```sql
SELECT Id, Status, Total, CreatedAt
FROM Orders
WHERE CustomerId = @customerId
ORDER BY CreatedAt DESC;
```

`CustomerId` supports the filter, `CreatedAt` supports ordering, and included columns can avoid a lookup to the base table.

## Professional company-level example
A safe performance investigation looks like this:

```text
1. Capture slow query and parameters
2. Capture actual execution plan
3. Compare estimated versus actual row counts
4. Measure logical reads, CPU, duration, memory, waits
5. Check indexes/statistics/sargability
6. Test one targeted change
7. Measure reads AND write impact
8. Deploy index through migration with rollback plan
```

### Non-sargable versus sargable

```sql
-- Usually prevents a seek:
WHERE YEAR(CreatedAt) = 2026

-- Allows a range seek on CreatedAt:
WHERE CreatedAt >= '2026-01-01'
  AND CreatedAt <  '2027-01-01'
```

## Internal behavior
A B-tree navigates from root to leaf pages. A seek is efficient when the predicate is selective and aligns with the index key order. A scan can be correct when many rows are needed or the table is small. The optimizer chooses using statistics; stale statistics or skewed values can produce a bad plan.

Parameter-sensitive behavior matters. A query optimized for a customer with ten orders may reuse a poor plan for a customer with ten million orders. Never judge a plan from one parameter value only.

## Common production failures
- `SELECT *` transfers unnecessary data.
- A function around an indexed column prevents efficient seeking.
- Too many indexes slow every write.
- A composite index has the wrong key order.
- Estimated rows differ dramatically from actual rows.
- A sort spills to disk because the memory estimate was wrong.
- A new index improves reads but harms write throughput.

## Comparison
| Structure | Strength | Cost |
|---|---|---|
| Clustered/primary access path | Organizes row storage | Only one physical order |
| Nonclustered index | Supports alternate access path | Extra write/storage cost |
| Covering index | Avoids base-table lookup | Wider index, more maintenance |
| Filtered index | Small and targeted | Only helps matching predicate |
| Full scan | Efficient for large result fraction | Expensive for selective lookups |

## Interview Questions
- **[L1]** What problem does an index solve?
- **[L1]** What is the difference between a scan and a seek?
- **[L2]** What is a covering index?
- **[L2]** What makes a predicate non-sargable?
- **[L3]** How would you troubleshoot a query that became slow after data growth?
- **[L3]** How would you prove a new index is worth its write cost?

## Interview Answers
1. It gives the database a structure for locating or ordering rows without reading every table row.
2. A seek navigates to likely matching index entries; a scan reads a large portion/all of an access structure. A scan is not always bad if most rows are needed.
3. It contains every column needed by a query as key or included columns, allowing the database to answer without looking up base-table rows.
4. Applying functions/expressions to an indexed column often forces evaluation for every row. Rewrite predicates as direct range/equality comparisons where possible.
5. Capture actual plan and row estimates, inspect statistics/indexes, check logical reads, waits, parameter sensitivity, locks, and data distribution; test a targeted change at realistic scale.
6. Compare read latency/reads/CPU before and after, then measure insert/update/delete throughput, storage, maintenance, and lock impact. Deploy only if the total workload improves.

## Expert perspective
An index is workload-specific code. Senior engineers design it from measured access patterns, validate it against production-like data, and account for writes, storage, migrations, and plan behavior rather than treating it as a free optimization.
