---
id: sql-normalization
slug: normalization
title: Database Normalization and Normal Forms
category: sql
categoryTitle: SQL / Databases
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Relational modeling"
prerequisites: [sql-fundamentals, sql-joins]
tags: [sql, normalization, 1nf, 2nf, 3nf, bcnf, data-modeling]
relatedTopics: [sql-denormalization, sql-database-design]
order: 70
status: published
---
# Database Normalization and Normal Forms

## Introduction
Normalization organizes facts so each fact has a clear owner and duplicated data does not drift. It is a modeling tool, not a database switch.

```text
Bad flat table:
Customer + Order + Product repeated on every line
             ↓ update one copy but miss another
              inconsistent data

Normalized:
Customer ──< Order ──< OrderLine >── Product
```

## Purpose
Normalization protects correctness and makes updates predictable. It is especially valuable for transactional systems where customers, orders, inventory, and payments change independently.

## Normal forms in simple language

- **1NF:** one atomic value per column; no comma-separated lists or repeated columns.
- **2NF:** every non-key attribute depends on the whole composite key.
- **3NF:** non-key attributes do not depend on other non-key attributes.
- **BCNF:** every determinant is a candidate key; a stricter form for special dependency cases.

## Real-World Simple Example
Bad:

```text
OrderId | CustomerEmail | Product1 | Product2 | CustomerName
```

Better:

```sql
Customers(Id, Email, Name)
Orders(Id, CustomerId)
Products(Id, Name)
OrderLines(OrderId, ProductId, Quantity)
```

Customer email is stored once. Product repetition is modeled as order lines.

## Professional company-level example
Not every repeated value is a mistake. An order should preserve the price charged at purchase time even when the catalog price changes:

```sql
CREATE TABLE OrderLines
(
    OrderId bigint NOT NULL,
    ProductId bigint NOT NULL,
    Quantity int NOT NULL CHECK (Quantity > 0),
    UnitPriceAtSale decimal(19,4) NOT NULL
);
```

`UnitPriceAtSale` is a deliberate historical fact, not accidental duplication. It answers “what did this customer pay?” while `Products.CurrentPrice` answers “what does the product cost now?”

## Anomaly model

```text
Insert anomaly  → cannot add a product without an order
Update anomaly  → customer email changed in one row but not others
Delete anomaly  → deleting last order accidentally deletes product information
```

Normalization removes these anomalies by separating ownership and enforcing relationships with keys/constraints.

## Comparison
| Model | Strength | Cost |
|---|---|---|
| Normalized OLTP | Correct writes, clear ownership | More joins |
| Denormalized read model | Fast/simple reads | Sync, staleness, repair |
| Star/warehouse model | Analytical scans | Not ideal for transactional updates |
| Document aggregate | Natural nested read | Cross-document relationships/transactions differ |

## Interview Questions
- **[L1]** Why do we normalize a database?
- **[L1]** What does First Normal Form mean?
- **[L2]** What problem does Third Normal Form solve?
- **[L2]** When is duplicated data intentional and correct?
- **[L3]** How do normalization decisions differ between OLTP and analytics?
- **[L3]** How would you decide whether to denormalize a slow read path?

## Interview Answers
1. To give each fact a clear owner and prevent insert, update, and delete anomalies caused by uncontrolled duplication.
2. Each column contains an atomic value; repeating groups and comma-separated multi-values are modeled separately.
3. It removes transitive dependencies where a non-key field depends on another non-key field, such as customer email stored on orders.
4. When it represents a historical snapshot or a different business fact, such as price charged at sale versus current catalog price.
5. Transactional systems prioritize correct independent writes and usually start normalized. Analytics often favors read-optimized denormalized/star structures.
6. Measure the actual plan/workload, confirm the bottleneck, define ownership/freshness, choose a rebuild/reconciliation strategy, and add a projection only if the read benefit justifies the complexity.

## Expert perspective
Normalize facts first, then denormalize deliberately. A senior engineer can explain who owns duplicated data, how fresh it must be, how it is repaired, and why a join or projection is cheaper than accepting inconsistency.
