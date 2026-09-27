---
id: databases-nosql-graph-data-access
slug: nosql-graph-data-access
title: PostgreSQL, MongoDB, Neo4j, and Polyglot Data Access
category: databases
categoryTitle: NoSQL, Graph & Data Access
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "PostgreSQL, MongoDB, Neo4j current concepts"
prerequisites: [sql-database-design, dotnet-data-access]
tags: [postgresql, mongodb, neo4j, cypher, nosql, graph]
relatedTopics: [sql-normalization, dotnet-data-access, architecture-fundamentals]
order: 10
status: published
---
# PostgreSQL, MongoDB, Neo4j, and Polyglot Data Access

## Overview
Different data stores optimize different access patterns: PostgreSQL provides relational integrity and powerful SQL, MongoDB models document-shaped aggregates, and Neo4j models connected entities and traversals. Polyglot persistence means choosing deliberately, not collecting databases by trend.

## Why This Exists
A relational schema is excellent for transactions and relationships, but a document or graph model may better fit hierarchical documents or multi-hop relationships. The correct choice starts with access patterns, consistency, scale, and operational capability.

## Fundamentals
PostgreSQL supports relational constraints, transactions, JSONB, indexing, and extensions. MongoDB stores BSON documents and favors aggregate-oriented access. Neo4j stores nodes and relationships and uses Cypher for graph traversal. Every additional datastore adds backup, security, monitoring, skills, and data synchronization cost.

## Syntax and API
```javascript
// MongoDB: document-shaped order read
await db.collection('orders').findOne(
  { _id: orderId, tenantId },
  { projection: { orderNumber: 1, lines: 1, total: 1, status: 1 } }
);
```
```cypher
// Neo4j: customers connected to products through purchased orders
MATCH (c:Customer {id: $customerId})-[:PLACED]->(:Order)-[:CONTAINS]->(p:Product)
RETURN p.id, count(*) AS purchases
ORDER BY purchases DESC;
```

## How It Works
Document design embeds data that is read and updated together, while references are appropriate when embedded data grows independently or has separate ownership. Graph queries traverse relationships directly; performance depends on labels, relationship direction, indexes, and traversal depth.

## Internal Implementation
PostgreSQL query planning and MVCC provide strong relational behavior. MongoDB indexes fields inside documents but still requires careful document-size and update-pattern decisions. Neo4j's traversal cost is driven by graph shape and query predicates; unbounded variable-length traversals can become expensive.

## Real-World Example
Use PostgreSQL for orders/payments, MongoDB for a flexible product-content document, and Neo4j only when recommendations or relationship traversal is a core workload—not merely because graphs sound modern.

## Production Example
A service owns its primary data in PostgreSQL and publishes events to build a MongoDB read projection. The projection is rebuildable and has explicit freshness/repair semantics; it is not treated as a second source of truth.

## Common Mistakes
- Choosing MongoDB without modeling document growth and update patterns.
- Using a graph database for simple foreign-key relationships.
- Sharing databases across services without ownership.
- Using ORM abstractions that hide expensive queries.
- No migration, backup, restore, or reconciliation plan.

## Performance
Benchmark real queries and data shapes. Measure indexes, working set, network round trips, query plans, document size, traversal depth, and write amplification.

## Security
Apply least privilege, encrypt connections/at-rest data, enforce tenant filters, protect backups, and ensure projections do not replicate fields a consumer is not authorized to see.

## Testing
Test provider-specific query behavior against real engines, migration/restore procedures, unique/foreign constraints where applicable, document validation, and projection reconciliation.

## When to Use
Choose PostgreSQL for relational transactions, MongoDB for aggregate/document access patterns, and Neo4j for genuine relationship traversal workloads.

## When Not to Use
Do not add a second or third database to avoid learning SQL or because a technology is popular; operational complexity must be justified by a measurable access-pattern benefit.

## Trade-offs
Polyglot persistence can fit workloads well but multiplies operations, skills, backup, consistency, and deployment concerns.

## Related Topics
See [Relational Database Design](/sql/database-design) and [EF Core, Dapper & Data Access](/dotnet/data-access).

## Practical Exercise
Model an e-commerce system in PostgreSQL, MongoDB, and Neo4j for three different access patterns. Explain why each model is appropriate and how data synchronization would work.

## Interview Questions
- **[L1]** When would you choose a document database over a relational database?
- **[L1]** What type of problem is a graph database good at?
- **[L2]** What operational cost does adding a second datastore create?
- **[L2]** How would you build a rebuildable MongoDB read projection from PostgreSQL events?
- **[L3]** How do you decide whether polyglot persistence is justified?

## Interview Answers
1. **[L1]** Choose a document database when data is naturally aggregate-shaped, read/written together, schema-flexible, and does not need extensive relational joins/constraints.
2. **[L1]** Graph databases excel at multi-hop relationship traversal such as recommendations, dependency networks, and fraud relationships.
3. **[L2]** It adds deployment, backup/restore, monitoring, security, upgrades, expertise, data synchronization, and incident-response responsibilities.
4. **[L2]** Publish durable events from the source transaction, consume idempotently, write the projection, track lag, and support full rebuild from source history.
5. **[L3]** Compare measured query needs, consistency, scale, team capability, and total operational cost. Add a datastore only when its unique capability produces more value than its long-term complexity.

## Senior Developer Perspective
Database choice is an access-pattern and ownership decision. Senior engineers can explain why a particular workload needs PostgreSQL, MongoDB, or Neo4j, how it will be operated, and what happens when the derived data is stale or corrupt.
