---
id: sql-database-design
slug: database-design
title: Relational Database Design
category: sql
categoryTitle: SQL / Databases
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "Relational modeling"
prerequisites: [sql-fundamentals, sql-normalization]
tags: [sql, schema, keys, constraints, modeling, migrations]
relatedTopics: [sql-indexes-performance, sql-transactions-isolation]
order: 130
status: published
---
# Relational Database Design

## Introduction
Database design translates business facts, relationships, lifecycle rules, query patterns, and operational constraints into tables, keys, constraints, indexes, and migration strategy.

```text
Business rule
    ↓
Entity + identity
    ↓
Relationships + ownership
    ↓
Constraints + transactions
    ↓
Indexes from access patterns
    ↓
Safe migration + operations
```

## Purpose
A schema is a long-lived contract. Good design prevents invalid states, supports important queries, and allows the data model to evolve without outages.

## Real-World Simple Example
```sql
CREATE TABLE OrderLines
(
    Id              bigint PRIMARY KEY,
    OrderId         bigint NOT NULL REFERENCES Orders(Id),
    ProductId       bigint NOT NULL REFERENCES Products(Id),
    Quantity        integer NOT NULL CHECK (Quantity > 0),
    UnitPriceAtSale decimal(19,4) NOT NULL CHECK (UnitPriceAtSale >= 0),
    UNIQUE (OrderId, ProductId)
);
```

Every definition communicates a business rule: order/product relationship, positive quantity, non-negative price, and one line per product per order.

## Professional company-level example
Large schema changes use expand-and-contract:

```text
Deploy 1: add new nullable column/table
      ↓
Backfill in small batches
      ↓
Deploy 2: application writes both/reads compatible shape
      ↓
Verify all consumers migrated
      ↓
Deploy 3: enforce constraint/remove old shape
```

Never assume a migration that works on a small local database is safe on a large, actively-used production table. Consider locks, transaction log growth, replica lag, rollback, old application versions during rolling deployment, and the ability to pause/resume a backfill.

## Key design decisions

```text
Identity       → primary key strategy
Relationships  → foreign keys/cardinality/delete behavior
Optionality    → NULL versus explicit state
Money          → fixed precision decimal, not float
Time           → UTC storage + explicit display timezone
Security       → sensitive columns, ownership, access paths
Performance    → indexes from real queries
History        → current state versus immutable snapshot
```

## Comparison
| Decision | Conservative default | Revisit when |
|---|---|---|
| Primary key | Stable surrogate/strong domain ID | External identity/partition needs differ |
| Foreign key | Enforce in database | Ownership is across independent services |
| Delete | Explicit/archive | Proven cascade boundary |
| Money | Fixed decimal | Specialized monetary model |
| Migration | Expand/contract | Small isolated table |
| Index | Measured access path | Workload changes |

## Interview Questions
- **[L1]** What is a primary key?
- **[L1]** Why should foreign keys be enforced by the database?
- **[L2]** How do you choose nullability and data types?
- **[L2]** How do you safely migrate a large production table?
- **[L3]** How does data ownership affect schema boundaries in microservices?
- **[L3]** How would you model current state and historical state together?

## Interview Answers
1. A primary key uniquely identifies a row and provides a stable reference for updates and relationships.
2. Database constraints protect data from every writer, including scripts, future services, and application bugs, not only the current application path.
3. Choose based on business meaning, valid states, precision, scale, query patterns, and lifecycle. Avoid making every field nullable “just in case.”
4. Use expand-and-contract, backfill in batches, monitor locks/log/replicas, maintain compatibility during rollout, and keep a rollback/reconciliation plan.
5. A service should own its tables and expose APIs/events for others. Cross-service joins become composition/projection problems with explicit consistency trade-offs.
6. Store mutable current state separately from immutable history/snapshots when the historical value must not change as the current value changes.

## Expert perspective
A schema is an operational API. Senior engineers design the data model, constraints, indexes, migration process, retention, backup/restore, security, and service ownership as one system instead of treating tables as passive storage.
