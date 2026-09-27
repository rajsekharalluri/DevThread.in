---
id: sql-select-filtering-sorting
slug: select-filtering-sorting
title: SELECT, Filtering, Sorting, and Pagination
category: sql
categoryTitle: SQL / Databases
difficulty: beginner
estimatedMinutes: 45
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals]
tags: [sql, select, where, order-by, pagination]
relatedTopics: [sql-joins, sql-query-examples, sql-indexes-performance]
order: 20
status: published
---
# SELECT, Filtering, Sorting, and Pagination

## Introduction
A `SELECT` query should return a deliberate projection, a bounded number of rows, and a deterministic order. The database is a shared production resource, so an endpoint that casually returns every column and row can become an outage as data grows.

```text
API request
   ↓ validate filters/page size
SELECT projection
   ↓ WHERE
filtered rows
   ↓ ORDER BY
stable order
   ↓ OFFSET/keyset + limit
bounded response
```

## Purpose
Filtering reduces work, projection reduces data transfer, sorting gives users predictable results, and pagination protects memory/network/database capacity.

## Real-World Simple Example
```sql
SELECT Id, OrderNumber, Total, Status
FROM Orders
WHERE CustomerId = @customerId
  AND Status IN ('Pending', 'Paid')
ORDER BY CreatedAt DESC, Id DESC
OFFSET @skip ROWS FETCH NEXT @pageSize ROWS ONLY;
```

The ID tie-breaker matters because two rows can have the same timestamp. Without it, page boundaries can move between requests.

## Professional company-level example
Offset pagination is simple but gets slower for deep pages because the database may scan/skip earlier rows. A continuously growing feed often uses keyset/cursor pagination:

```sql
SELECT Id, CreatedAt, Total
FROM Orders
WHERE CustomerId = @customerId
  AND (
       CreatedAt < @lastCreatedAt
       OR (CreatedAt = @lastCreatedAt AND Id < @lastId)
  )
ORDER BY CreatedAt DESC, Id DESC
FETCH FIRST @pageSize ROWS ONLY;
```

The client receives an opaque cursor representing the last row instead of a page number. The server caps page size and validates allowed sort fields; it never interpolates arbitrary client text into SQL identifiers.

## Important behavior
SQL logically processes `FROM`, `WHERE`, grouping, `SELECT`, ordering, and pagination, but the optimizer can execute equivalent physical operations in a different order. Functions around indexed columns can prevent efficient seeking:

```sql
-- Often non-sargable:
WHERE YEAR(CreatedAt) = 2026

-- Searchable date range:
WHERE CreatedAt >= '2026-01-01'
  AND CreatedAt <  '2027-01-01'
```

## Comparison
| Pagination | Strength | Limitation |
|---|---|---|
| Offset | Simple, random page numbers | Deep pages and concurrent changes |
| Keyset/cursor | Stable and efficient at depth | No arbitrary page jump; needs stable key |
| Full export | Simple batch semantics | Must be bounded/streamed |

## Interview Questions
- **[L1]** What does `WHERE` do?
- **[L1]** Why must pagination use a deterministic order?
- **[L2]** Compare offset and keyset pagination.
- **[L2]** Why can a function around an indexed column hurt performance?
- **[L3]** How would you safely support a client-selected sort field?
- **[L3]** What should an API cursor contain and why should it be opaque?

## Interview Answers
1. It filters individual source rows before grouping and projection.
2. Without a unique tie-breaker, equal sort values can appear in different orders, causing duplicates or skipped rows between page requests.
3. Offset is simple and supports page numbers but degrades at depth; keyset uses the last key and remains stable/fast but does not support arbitrary jumps.
4. The database may need to calculate the function for every row before comparing, preventing a direct index seek. Rewrite as a range/equality predicate on the raw column.
5. Map a small server-side allow-list such as `newest` → `CreatedAt DESC, Id DESC`; reject unknown values. Never interpolate raw client input.
6. It should encode the last stable sort-key values and relevant scope/filter version; opacity prevents clients depending on internal schema and lets the server evolve the cursor format.

## Expert perspective
Pagination is an API contract and a database resource budget. Senior engineers define ordering stability, consistency under concurrent writes, maximum page size, cursor evolution, and authorization before implementing the SQL.
