---
id: databases-learning-path
slug: learning-path
title: Data Stores Learning Path
category: databases
categoryTitle: NoSQL, Graph & Data Access
difficulty: beginner
estimatedMinutes: 15
version:
  minimum: "Database concepts"
prerequisites: [sql-learning-path]
tags: [databases, learning-path, nosql, graph, polyglot-persistence]
relatedTopics: [databases-nosql-graph-data-access, comparisons-sql-nosql-graph, comparisons-cache-database]
order: 1
status: published
---
# Data Stores Learning Path

## Introduction

Relational SQL is the foundation for most business systems, but production platforms also use document databases, graph databases, caches, search engines, and read models. This path explains the order in which to learn these stores and how to decide when a new one is justified. Complete the SQL learning path first; every other store is easier to evaluate once you understand relational modeling, transactions, and indexing.

## Stage 1: Relational Foundations (SQL Track)

| Topic | Why It Comes First |
|---|---|
| SQL fundamentals, joins, aggregation | Most data questions are relational |
| Database design, normalization, denormalization | Modeling decisions determine correctness and performance |
| Indexes and performance | Most "the database is slow" problems are index or query-shape problems |
| Transactions and isolation | Correctness under concurrency |

**Outcome:** you can model an order system, write its queries, and explain its execution plans.

## Stage 2: Beyond Relational

| Topic | What You Learn |
|---|---|
| PostgreSQL, MongoDB, Neo4j & Polyglot Data Access | Document aggregates, graph traversals, when each model fits, operating multiple stores |
| Decision Guide: SQL vs Document vs Graph | Side-by-side modeling of the same domain and a decision matrix |

**Outcome:** you can model the same domain three ways and justify which store owns which data.

## Stage 3: Derived Data and Performance Layers

| Topic | What You Learn |
|---|---|
| Decision Guide: Cache vs Database vs Read Model | Cache-aside, invalidation, projections, rebuilds |
| Architecture: CQRS | Separating write models from read models |
| Architecture: Outbox/Inbox | Reliable propagation of changes between stores |

**Outcome:** a projection from a relational source of truth into a read-optimized store, with idempotent updates, lag monitoring, and a rebuild procedure.

## Stage 4: Data Access from Code

| Topic | What You Learn |
|---|---|
| .NET data access, EF Core, Dapper | Mapping, queries, transactions from application code |
| EF Core migrations and deployment | Safe schema evolution |

## Decision Principles to Remember

- One **source of truth** per piece of data; everything else is derived and rebuildable
- Choose stores from **access patterns and consistency needs**, not popularity
- Every additional store adds backup, monitoring, security, upgrade, and on-call cost
- Propagate changes with an **outbox or change data capture**, never with dual writes

## Interview Questions
- **[L1]** Why should you learn relational databases before NoSQL and graph databases?
- **[L1]** What does "source of truth" mean in a system with several data stores?
- **[L2]** What questions do you ask before adding a new type of database to a system?
- **[L2]** Why are dual writes to two databases dangerous?
- **[L3]** How would you introduce a search engine alongside an existing relational database?
- **[L3]** How do you keep a team from accumulating too many specialized data stores?

## Interview Answers
1. Relational databases teach the fundamentals every store builds on or deliberately trades away: data modeling, normalization, constraints, transactions, isolation, indexing, and query planning. Most business systems keep a relational source of truth, and you cannot evaluate what a document or graph database gains or gives up without understanding these concepts.
2. The source of truth is the authoritative store for a specific piece of data, where writes are accepted and invariants are enforced. Other stores, such as caches, search indexes, read models, and graph projections, hold derived copies that are updated from it and can be rebuilt from it. When copies disagree, the source of truth wins, and reconciliation fixes the derived data.
3. What access pattern or capability does the current store fail to provide, measured rather than assumed? Which data would it own or derive, and what is the source of truth? What consistency and freshness are acceptable? How will data be synchronized reliably? Who will operate it, including backups, restores, monitoring, security, and upgrades, and does the team have the skills? What is the total cost, and what happens during its outages?
4. Writing to two databases from application code is not atomic: if the first write succeeds and the second fails, or the process crashes between them, the stores become inconsistent with no record of what is missing. Retries can cause duplicates or out-of-order updates. The outbox pattern or change data capture makes propagation reliable by committing the change and the event together in the source database and delivering it asynchronously with retries and idempotent consumers.
5. Keep the relational database as the source of truth and treat the search index as a derived projection. Publish changes through a transactional outbox or CDC, consume them with idempotent indexers that upsert documents keyed by entity ID, and handle deletions. Build a full reindex job from the source for the initial load and for mapping changes, preferably using index aliases to swap to a new index without downtime. Monitor indexing lag and failures, and design the UI to tolerate slight staleness.
6. Establish architecture guidelines with a small set of approved stores and a lightweight decision record requirement for adding new ones, including the access pattern justification, ownership, operational plan, and cost. Prefer extending existing stores first, for example PostgreSQL JSONB, full-text search, or extensions, before adding new systems. Review existing stores periodically and consolidate those with low value, and make platform teams responsible for providing well-operated shared offerings.
