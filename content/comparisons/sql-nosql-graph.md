---
id: comparisons-sql-nosql-graph
slug: sql-nosql-graph
title: Relational SQL vs Document vs Graph Databases
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: "Database modeling concepts"
prerequisites: [sql-database-design, databases-nosql-graph-data-access]
tags: [sql, mongodb, neo4j, databases, comparison]
relatedTopics: [sql-normalization, databases-nosql-graph-data-access]
order: 60
status: published
---
# Relational SQL vs Document vs Graph Databases

## Decision table

| Model | Best fit | Main trade-off |
|---|---|---|
| Relational SQL | Transactions, relationships, constraints | Joins/schema migration |
| Document | Aggregate-shaped flexible data | Cross-document relationships/consistency |
| Graph | Multi-hop relationship traversal | Operational/graph-specific complexity |

## Example

```text
Orders/payments/inventory → relational SQL
Product content aggregate → document database
Fraud/recommendation network → graph database
```

Choose from access patterns, consistency, scale, team capability, backup/restore, and operational cost. Do not add multiple stores merely because each is popular.

## Interview Questions
- **[L1]** When is relational SQL the natural choice?
- **[L1]** What problem is a graph database good at?
- **[L2]** When is a document model useful?
- **[L2]** What operational cost does polyglot persistence add?
- **[L3]** How would you decide between SQL and MongoDB for an order system?
- **[L3]** How do you prevent derived data across stores from becoming inconsistent?

## Interview Answers
1. Transactions, constraints, relationships, reporting queries, and shared authoritative business data.
2. Multi-hop relationships such as recommendations, dependencies, and fraud networks.
3. When data is aggregate-shaped, read/written together, schema-flexible, and does not require broad relational joins.
4. More backup/restore, monitoring, security, upgrades, skills, synchronization, and incident responsibilities.
5. Start with order invariants/relationships/transactions; SQL is usually primary. Choose document only for a measured aggregate/content access pattern.
6. Define one source of truth, publish durable events, make projections idempotent/rebuildable, expose freshness, and reconcile.
