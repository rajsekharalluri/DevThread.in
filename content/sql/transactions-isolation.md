---
id: sql-transactions-isolation
slug: transactions-isolation
title: Transactions, Isolation, Locks, and Deadlocks
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "Relational SQL"
prerequisites: [sql-fundamentals, sql-joins]
tags: [sql, transactions, isolation, locks, deadlocks, concurrency]
relatedTopics: [sql-indexes-performance, architecture-distributed-systems]
order: 80
status: published
---
# Transactions, Isolation, Locks, and Deadlocks

## Introduction
A transaction groups database changes into an atomic unit. Isolation controls what concurrent transactions can see. Locks and row versions are mechanisms used to protect consistency.

```text
Transaction A: read → validate → write → commit
Transaction B: read → wait or version → write → commit/rollback
```

ACID means atomicity, consistency, isolation, and durability. It does not mean every distributed workflow is automatically atomic.

## Purpose
Use transactions to protect business invariants such as:

```text
Reserve inventory
    ├── decrement available quantity
    ├── create reservation
    └── record outbox event
All commit together or all roll back
```

## Real-World Simple Example
```sql
BEGIN TRANSACTION;

UPDATE Inventory
SET Available = Available - @quantity
WHERE Sku = @sku
  AND Available >= @quantity;

-- Application checks affected-row count.
-- 1 row: commit; 0 rows: insufficient stock, rollback.

COMMIT TRANSACTION;
```

The `WHERE Available >= @quantity` condition and transaction prevent two competing requests from both reserving the same final stock, assuming the database isolation/locking behavior is appropriate.

## Isolation levels

```text
Read Uncommitted → may see dirty data
Read Committed    → no dirty reads; non-repeatable reads possible
Repeatable Read   → protects rows read; phantom behavior varies
Snapshot          → consistent row-version view
Serializable      → strongest isolation; lowest concurrency
```

Higher isolation can mean more blocking or more row-version storage. The correct level comes from the business invariant, not from choosing the strongest setting automatically.

## Professional company-level example
Do not hold a database transaction open while calling a remote payment provider:

```text
Bad:
BEGIN TRANSACTION
  update order
  call payment provider  ← locks held during network delay
  commit

Better:
Create idempotent payment request
      ↓
Call provider with idempotency key
      ↓
Commit local result + outbox/event
      ↓
Reconcile timeout/unknown outcomes
```

The outbox pattern makes the local state change and event intent atomic without pretending the remote service participates in the database transaction.

## Locks and deadlocks
A deadlock occurs when transactions hold resources in opposite order:

```text
Transaction A: holds Order 1 → waits for Order 2
Transaction B: holds Order 2 → waits for Order 1
```

The database chooses a victim and rolls it back. Reduce deadlocks by acquiring resources in a consistent order, shortening transactions, indexing predicates, and retrying the victim with bounded backoff where the operation is safe.

## Comparison
| Choice | Benefit | Cost |
|---|---|---|
| Read committed | Common balance | Non-repeatable reads possible |
| Snapshot | Readers avoid many writer locks | Version-store pressure |
| Serializable | Strongest isolation | Blocking/concurrency cost |
| Short transaction | Less lock time | Requires deliberate boundaries |
| Distributed workflow | Scales across services | Eventual consistency/saga complexity |

## Interview Questions
- **[L1]** What does atomicity mean?
- **[L1]** What is a deadlock?
- **[L2]** Compare read committed and snapshot isolation.
- **[L2]** Why should a transaction not remain open during an HTTP call?
- **[L3]** How would you diagnose and reduce deadlocks?
- **[L3]** How do you maintain consistency across multiple service databases?

## Interview Answers
1. Atomicity means all operations in a transaction commit together or none of them become permanent.
2. A deadlock is a cycle where transactions hold locks the other transactions need; the database rolls one back to break the cycle.
3. Read committed prevents dirty reads but allows later reads to change; snapshot gives a consistent versioned view and reduces reader/writer blocking at version-store cost.
4. Network latency is unpredictable. Holding locks while waiting increases blocking, deadlocks, and resource exhaustion.
5. Inspect deadlock graphs, identify lock order/resources, add supporting indexes, shorten transactions, standardize access order, and retry safe victims with jitter.
6. Use local transactions plus outbox/events and sagas with idempotent steps/compensation. Do not assume one local ACID transaction crosses service databases.

## Expert perspective
Transaction boundaries should match business invariants and a bounded resource lifetime. Senior engineers define isolation, timeout, retry, idempotency, observability, and reconciliation together.
