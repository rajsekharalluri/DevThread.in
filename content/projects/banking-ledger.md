---
id: projects-banking-ledger
slug: banking-ledger
title: "Build: Banking Ledger and Transfer System"
category: projects
categoryTitle: Build Real Systems
difficulty: architect
estimatedMinutes: 90
version:
  minimum: "Distributed transaction exercise"
prerequisites: [sql-transactions-isolation, architecture-distributed-systems, architecture-reliability-engineering]
tags: [project, banking, ledger, consistency, audit]
relatedTopics: [sql-transactions-isolation, projects-payment-platform, architecture-saga-process-manager]
order: 60
status: published
---
# Build: Banking Ledger and Transfer System

## Introduction
Design a money-transfer system with an immutable ledger, account balances, idempotent commands, authorization, audit, and reconciliation. The ledger—not a mutable balance alone—is the financial source of truth.

```text
Transfer command
   ↓ validate/auth/idempotency
Ledger transaction
   ├── debit entry
   ├── credit entry
   └── transaction reference
   ↓
Balance projection + audit/reconciliation
```

## Purpose
Practice money precision, transaction boundaries, double-entry accounting, concurrency, duplicate requests, and recovery. Never use floating-point arithmetic for monetary values.

## Professional design
A transfer creates balanced entries in one local transaction:

```text
Transfer T1
  Account A: -100.00
  Account B: +100.00
  Sum: 0.00
```

Use fixed-precision decimal/minor units, unique transfer/idempotency keys, database constraints, consistent lock ordering, and an immutable append-only ledger. A balance can be a projection verified against ledger entries.

## Cross-boundary transfer
If accounts are in different service/database boundaries:

```text
Create pending transfer
  ↓ reserve/source authorization
  ↓ destination credit
  ↓ complete
or compensate/reconcile
```

Use a saga/process manager and explicit pending/failed/unknown states. Do not pretend two independent databases share one ACID transaction.

## Interview Questions
- **[L1]** What is double-entry accounting?
- **[L1]** Why should money not use floating point?
- **[L2]** How do you prevent duplicate transfers?
- **[L2]** Why should the ledger be immutable?
- **[L3]** How do you handle a transfer across two service databases?
- **[L3]** How do you reconcile a balance projection with the ledger?

## Interview Answers
1. Every transaction has balanced debit/credit entries whose total is zero, preserving accounting integrity.
2. Binary floating point cannot represent many decimal fractions exactly; use decimal/minor units with defined currency precision.
3. Enforce a unique idempotency/transfer key and return the original result for duplicate requests.
4. Immutable history provides auditability and permits rebuilding current balance/projections; corrections are compensating entries, not destructive edits.
5. Use pending state, saga/process manager, idempotent local steps, compensation, provider/reconciliation checks, and explicit unknown outcomes.
6. Recalculate from authoritative ledger entries, compare to projection with checksums/counts, alert on differences, and repair by replay/rebuild under controlled operations.

## Expert Perspective
Financial systems are consistency and audit systems. A mutable balance plus a “successful” HTTP response is not a ledger; correctness requires immutable entries, idempotency, authorization, concurrency, reconciliation, and recovery design.
