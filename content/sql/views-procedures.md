---
id: sql-views-procedures
slug: views-procedures
title: Views, Stored Procedures, and Functions
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 45
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-joins]
tags: [sql, views, stored-procedures, functions, contracts]
relatedTopics: [sql-triggers, sql-indexes-performance]
order: 110
status: published
---
# Views, Stored Procedures, and Functions

## Introduction
A view is a named query. A stored procedure packages one or more database operations. A function returns a value or table-shaped result that can be used inside a query, depending on database rules.

```text
View       → reusable read projection
Procedure  → parameterized operation/batch
Function   → reusable calculation/query expression
```

## Purpose
Database objects can centralize approved projections, protect selected columns, run data-local batches, and give multiple applications a stable data contract. They are still production code and need versioning.

## Real-World Simple Example
```sql
CREATE VIEW ActiveOrders AS
SELECT Id, CustomerId, Total, Status
FROM Orders
WHERE Status IN ('Pending', 'Paid');
```

A reporting user can query the approved projection instead of receiving direct access to every column in `Orders`.

## Professional company-level example
A batch procedure should have a documented transaction, row-count contract, permissions, and plan behavior:

```sql
CREATE PROCEDURE CancelStaleOrders @OlderThan datetime2
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Orders
    SET Status = 'Cancelled'
    WHERE Status = 'Pending'
      AND CreatedAt < @OlderThan;

    SELECT @@ROWCOUNT AS RowsChanged;
END;
```

A normal view is not automatically cached or faster. Materialized/indexed views are engine-specific and add refresh/maintenance cost. Procedures can suffer parameter-sensitive plan behavior, so inspect actual plans for important calls.

## Important risks

- A view can hide an expensive join/scan.
- Procedures can become a second application codebase with poor versioning.
- Functions may have provider-specific restrictions.
- Result shape changes can break many consumers.
- Permissions/ownership chaining can grant more access than intended.

## Comparison
| Object | Main use | Typical side effects |
|---|---|---|
| View | Reusable read contract | Usually none |
| Materialized/indexed view | Stored derived read | Write/refresh cost |
| Procedure | Multi-step operation/batch | Can write/transaction |
| Scalar function | Value calculation | Engine restrictions |
| Table-valued function | Queryable result | Plan/parameter behavior |

## Interview Questions
- **[L1]** What is a view?
- **[L1]** How is a stored procedure different from a function?
- **[L2]** Does a normal view automatically improve performance?
- **[L2]** What is parameter-sensitive plan behavior in a procedure?
- **[L3]** When should logic live in a database object versus an application service?
- **[L3]** How should database objects be versioned and tested?

## Interview Answers
1. A view is a named query that consumers can query like a table; it usually does not store results itself.
2. Procedures can perform multiple statements and writes; functions return values/results and are typically more restricted for side effects.
3. No. A normal view usually expands into the surrounding query and still needs good SQL/indexes; materialized views are a separate engine-specific feature.
4. A plan compiled for one parameter distribution may be reused for another where it performs badly. Test representative values and use appropriate mitigation only when measured.
5. Keep data-local, permission, or batch behavior in the database when that is a clear benefit; keep general orchestration/integrations in application code where testing/versioning are easier.
6. Store definitions in migrations/version control, review them like application code, test permissions/result schemas/transactions, and deploy them compatibly with rolling application versions.

## Expert perspective
Views and procedures are not “free database helpers.” They are shared production APIs with ownership, versioning, security, plan, and compatibility responsibilities.
