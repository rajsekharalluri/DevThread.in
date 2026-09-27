---
id: sql-triggers
slug: triggers
title: SQL Triggers
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 40
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-transactions-isolation]
tags: [sql, triggers, auditing, constraints, side-effects]
relatedTopics: [sql-transactions-isolation, sql-views-procedures]
order: 100
status: published
---
# SQL Triggers

## Introduction
A trigger is database code that runs automatically when an `INSERT`, `UPDATE`, or `DELETE` occurs. It operates inside the initiating transaction and can enforce or record data changes regardless of which application performed them.

```text
UPDATE statement
      ↓
Trigger executes
      ├── audit/change
      ├── validate
      └── fail → original transaction can roll back
```

## Purpose
Triggers are appropriate when a narrow, data-local rule or audit requirement must apply to every writer, including scripts and other applications that bypass your main service.

## Real-World Simple Example
```sql
CREATE TRIGGER AuditOrderStatus
ON Orders
AFTER UPDATE
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO OrderAudit(OrderId, OldStatus, NewStatus, ChangedAt)
    SELECT i.Id, d.Status, i.Status, SYSUTCDATETIME()
    FROM inserted AS i
    JOIN deleted AS d ON d.Id = i.Id
    WHERE i.Status <> d.Status;
END;
```

The code compares transition sets and records only real status changes.

## Professional company-level example
A compliance audit trigger should be narrow, set-based, versioned through migrations, permission-controlled, and observable. A multi-row `UPDATE` can affect thousands of rows, so the trigger must operate on a set — never assume it runs once for one row.

Do not call an external API from a trigger:

```text
Database transaction
   ├── update row
   └── HTTP call ❌ locks held during network delay
```

Use an outbox/event for external notifications instead.

## Important risks
Triggers are invisible from most application code. They add latency and locks to the original write, can recursively fire other triggers, and can surprise developers who do not know they exist. They should not contain general business workflows or cross-service communication.

## Comparison
| Requirement | Better choice |
|---|---|
| Every writer must create audit record | Narrow trigger |
| Application workflow after commit | Outbox/event |
| Request validation | Application + database constraints |
| Remote notification | Worker/message broker |
| Complex business policy | Domain/application code |

## Interview Questions
- **[L1]** What causes a trigger to execute?
- **[L1]** Can a trigger roll back the statement that fired it?
- **[L2]** Why must triggers handle multiple changed rows?
- **[L2]** Why are network calls inside triggers dangerous?
- **[L3]** When is a trigger better than an application event handler?
- **[L3]** How should trigger behavior be governed in a large organization?

## Interview Answers
1. A configured insert/update/delete event on the table causes it to run automatically.
2. Yes. It runs in the initiating transaction, so an error or rollback can fail the original statement.
3. One SQL statement can affect many rows and the trigger commonly receives transition sets such as `inserted`/`deleted`; row-by-row assumptions lose data.
4. Network latency and failure become part of the database transaction, increasing lock duration and coupling data writes to an unreliable external system.
5. Use a trigger when every possible writer must obey a narrow data-local audit/invariant rule. Use application events for visible, orchestrated workflows.
6. Version through migrations, document owners/side effects, restrict alteration permissions, test multi-row/rollback behavior, and monitor execution time and recursion.

## Expert perspective
Triggers are not automatically bad; invisible, broad, unowned triggers are. Use them only when data proximity is the requirement, keep them narrow, and never mistake an implicit audit hook for a complete integration architecture.
