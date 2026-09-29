---
id: comparisons-sql-nosql-graph
slug: sql-nosql-graph
title: Relational SQL vs Document vs Graph Databases
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "Database modeling concepts; PostgreSQL 15+, MongoDB 7+, Neo4j 5+ examples"
prerequisites: [sql-database-design, databases-nosql-graph-data-access]
tags: [sql, mongodb, neo4j, databases, comparison]
relatedTopics: [sql-normalization, databases-nosql-graph-data-access]
order: 60
status: published
---
# Relational SQL vs Document vs Graph Databases

## Introduction

Database models differ in what they make **easy** and what they make **expensive**:

- **Relational (PostgreSQL, SQL Server, MySQL):** data in normalized tables with typed columns, foreign keys, constraints, and ACID transactions. Easy: integrity, joins, ad-hoc queries, reporting. Harder: deeply nested aggregates, very variable schemas.
- **Document (MongoDB, Cosmos DB, DynamoDB-style):** data as JSON-like documents that group everything read together. Easy: reading and writing whole aggregates, flexible schemas, horizontal scaling. Harder: cross-document relationships, joins, and multi-document consistency.
- **Graph (Neo4j, Amazon Neptune):** nodes and relationships as first-class data. Easy: multi-hop traversals ("friends of friends who bought X"). Harder: large aggregations, bulk tabular reporting.

Choose from **access patterns, consistency requirements, and operational capability** - not from popularity.

## Quick Decision Table

| Concern | Relational SQL | Document | Graph |
|---|---|---|---|
| Data shape | Tables and rows | Nested documents (aggregates) | Nodes and relationships |
| Schema | Enforced, migrated | Flexible (validators optional) | Flexible labels/properties |
| Relationships | Foreign keys + joins | Embedding or references | First-class, traversed directly |
| Transactions | Strong, multi-table ACID | Single-document atomic; multi-document supported with cost | ACID within the database |
| Query strengths | Joins, aggregation, reporting | Whole-aggregate reads, simple filters | Multi-hop paths, pattern matching |
| Scaling style | Vertical + read replicas; sharding with effort | Horizontal sharding built in | Mostly vertical; clustering for reads |
| Best fit | Orders, payments, inventory, finance | Catalogs, content, user profiles, event payloads | Recommendations, fraud rings, dependency graphs, access graphs |
| Main risk | Rigid for highly variable data; join cost | Duplication, inconsistent denormalized data | Operational niche, weak at aggregates |

## Relational SQL

### When to Use

- Business-critical data with invariants: money, stock, bookings
- Many relationships and ad-hoc reporting needs
- Multiple entities must change atomically

### When Not to Use as the Only Store

- Highly variable, per-record schemas (product attributes that differ by category) at large scale - though JSONB columns often suffice
- Deep relationship traversal across many hops at interactive latency

### Example

```sql
CREATE TABLE orders (
  id          UUID PRIMARY KEY,
  customer_id UUID NOT NULL REFERENCES customers(id),
  status      TEXT NOT NULL CHECK (status IN ('placed','paid','shipped','cancelled')),
  total       NUMERIC(12,2) NOT NULL CHECK (total >= 0),
  created_at  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE order_items (
  order_id   UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
  product_id UUID NOT NULL REFERENCES products(id),
  quantity   INT  NOT NULL CHECK (quantity > 0),
  unit_price NUMERIC(12,2) NOT NULL,
  PRIMARY KEY (order_id, product_id)
);

-- Revenue by category last 30 days: natural in SQL
SELECT p.category, SUM(oi.quantity * oi.unit_price) AS revenue
FROM order_items oi
JOIN orders o   ON o.id = oi.order_id AND o.status IN ('paid','shipped')
JOIN products p ON p.id = oi.product_id
WHERE o.created_at >= now() - interval '30 days'
GROUP BY p.category
ORDER BY revenue DESC;
```

## Document Database

### When to Use

- Data read and written as a unit (an order with its lines, a product page with variants)
- Schema varies by record and evolves quickly
- Very high throughput with horizontal partitioning by a natural key

### When Not to Use

- Many-to-many relationships queried from multiple directions
- Cross-entity invariants that must hold atomically across many documents
- Heavy ad-hoc analytics (export to a warehouse instead)

### Example (MongoDB)

```javascript
db.orders.insertOne({
  _id: "ord_123",
  customerId: "cus_42",
  status: "paid",
  createdAt: ISODate("2025-03-01T10:00:00Z"),
  lines: [
    { sku: "SKU-1", name: "USB-C Hub", qty: 2, unitPrice: 25.00 },   // name/price snapshot at purchase
    { sku: "SKU-9", name: "Monitor 27in", qty: 1, unitPrice: 310.00 }
  ],
  total: 360.00,
  shipping: { city: "Pune", country: "IN" }
});

db.orders.createIndex({ customerId: 1, createdAt: -1 });
db.orders.find({ customerId: "cus_42" }).sort({ createdAt: -1 }).limit(10);   // one read returns full orders
```

**Modeling rule:** embed what is read together and owned by the aggregate (order lines); reference what is shared and changes independently (customer profile). Denormalized snapshots (product name at purchase time) are intentional, not accidental duplication.

## Graph Database

### When to Use

- Queries follow relationships several hops deep with variable path lengths
- Relationship patterns are the insight: fraud rings, recommendations, network/dependency impact, identity and access graphs

### When Not to Use

- Primarily tabular data with aggregations and reports
- Simple one-hop lookups (a foreign key join is fine)

### Example (Neo4j Cypher)

```cypher
// Customers who share a device or payment card with a known fraudster (up to 3 hops)
MATCH (f:Customer {flagged: true})-[:USED_DEVICE|USED_CARD*1..3]-(suspect:Customer)
WHERE suspect <> f
RETURN DISTINCT suspect.id, suspect.email
LIMIT 50;

// "Customers who bought this also bought"
MATCH (:Product {sku: "SKU-1"})<-[:CONTAINS]-(:Order)<-[:PLACED]-(c:Customer)-[:PLACED]->(:Order)-[:CONTAINS]->(rec:Product)
WHERE rec.sku <> "SKU-1"
RETURN rec.sku, count(DISTINCT c) AS buyers
ORDER BY buyers DESC LIMIT 5;
```

The equivalent SQL needs recursive CTEs or several self-joins and becomes slow and hard to read as hop count grows.

## Polyglot Persistence Done Safely

```text
PostgreSQL (source of truth for orders, payments, inventory)
  -> transactional outbox -> events (OrderPaid, ProductUpdated)
      -> MongoDB projection: customer order history documents (fast reads)
      -> Neo4j projection: purchase graph for recommendations and fraud
      -> Search index: product search
Each projection: idempotent, monitored for lag, fully rebuildable from the source
```

## Same Scenario: E-commerce Platform

| Data / Query | Best Fit | Reason |
|---|---|---|
| Orders, payments, stock reservations | Relational | Invariants and transactions |
| Product catalog with category-specific attributes | Document (or SQL + JSONB) | Variable schema, whole-document reads |
| "Customers also bought" and fraud-ring detection | Graph | Multi-hop relationship queries |
| Finance reporting | Relational / data warehouse | Joins and aggregations |
| Order history page (high read volume) | Document projection or SQL read model | Precomputed aggregate reads |

## Decision Matrix

| Factor | Relational | Document | Graph |
|---|---|---|---|
| Strong integrity / transactions | Strong | Medium | Medium |
| Flexible schema | Medium (JSONB) | Strong | Strong |
| Aggregate read performance | Medium | Strong | Weak |
| Multi-hop relationships | Weak | Weak | Strong |
| Ad-hoc reporting | Strong | Weak | Weak |
| Horizontal write scaling | Medium | Strong | Weak-Medium |
| Team familiarity (typical) | Strong | Medium | Weak |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Choosing MongoDB to "avoid schemas" | Schemas still exist; enforce them with validators and code |
| Normalizing a document database like SQL | Model around aggregates and access patterns |
| Adding a graph database for one-hop lookups | Use SQL joins |
| Several databases with no single source of truth | Designate an owner store; project the rest |
| Dual writes to two databases in application code | Outbox + events, or CDC |
| Ignoring backup, restore, and upgrade skills for each new store | Include operational cost in the decision |

## Interview Questions
- **[L1]** When is relational SQL the natural choice?
- **[L1]** What problem is a graph database good at?
- **[L2]** When is a document model useful?
- **[L2]** What operational cost does polyglot persistence add?
- **[L3]** How would you decide between SQL and MongoDB for an order system?
- **[L3]** How do you prevent derived data across stores from becoming inconsistent?

## Interview Answers
1. Relational SQL is the natural choice for authoritative business data with invariants and relationships: orders, payments, inventory, accounts, bookings. It provides multi-table ACID transactions, foreign keys, check and unique constraints, powerful joins and aggregations for ad-hoc queries and reporting, mature tooling, and broad team familiarity, which makes it the safe default for most systems of record.
2. Graph databases are good at queries dominated by relationships, especially multi-hop and variable-length traversals and pattern matching: recommendations ("people who bought this also bought"), fraud rings sharing devices or cards, network and dependency impact analysis, organizational and access-control graphs, and knowledge graphs. These traversals stay fast and readable where SQL would need recursive CTEs or many self-joins.
3. A document model is useful when data is naturally aggregate-shaped and read or written as a unit, such as an order with its lines, a product with variants and attributes, or a user profile with preferences; when the schema varies by record or evolves quickly; and when the workload benefits from horizontal partitioning by a natural key. It is less suitable when entities are shared across many aggregates and queried from many directions.
4. Each additional datastore brings its own provisioning, configuration, backup and restore testing, monitoring and alerting, security hardening and access control, patching and version upgrades, capacity planning, cost, and on-call expertise. It also adds data synchronization pipelines, consistency and lag issues between stores, more complex local development and testing, and more incident scenarios. That cost must be justified by a capability the existing store cannot provide.
5. Start from the order domain's invariants and access patterns. Orders involve money, stock reservations, status transitions, and relationships to customers, products, payments, and shipments, and they need reporting, so relational SQL is usually the primary store because of transactions and constraints. MongoDB becomes attractive for specific measured needs, such as a very high-volume order history read model or massive write scale with a natural shard key, typically as a projection from the SQL source of truth rather than a replacement. Also weigh team skills and operations.
6. Designate a single source of truth per piece of data and treat other stores as derived projections. Publish changes reliably from the source using a transactional outbox or change data capture instead of dual writes, make projection consumers idempotent and ordered per entity key, track and alert on projection lag and failures, expose freshness to users where it matters, run periodic reconciliation jobs that compare source and projections, and make every projection fully rebuildable from source history.
