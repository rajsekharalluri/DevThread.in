---
id: efcore-learning-path
slug: learning-path
title: EF Core and Data Access Learning Path
category: efcore
categoryTitle: Entity Framework Core
difficulty: intermediate
estimatedMinutes: 15
version:
  minimum: "EF Core 8+"
prerequisites: [csharp-learning-path, sql-learning-path]
tags: [ef-core, data-access, learning-path, curriculum]
relatedTopics: [efcore-change-tracking, efcore-query-performance]
order: 1
status: published
---
# EF Core and Data Access Learning Path

## Introduction
This track connects C# application code to relational database behavior and production data-access decisions.

## Learning sequence

```text
SQL/relational foundations
DbContext/DbSet/unit of work
LINQ query translation
Entities/relationships/configuration
Change tracking/loading
Migrations/transactions/concurrency
Projection/paging/performance
EF Core versus Dapper/raw SQL
Production testing/observability
```

## Daily development outcome
You should be able to write correct queries, understand generated SQL, choose tracking/projection/loading, manage transactions/concurrency, deploy migrations safely, and troubleshoot real database performance.

## Interview Questions
- **[L1]** What should be learned before EF Core?
- **[L1]** What does `DbContext` represent?
- **[L2]** How does LINQ become SQL?
- **[L2]** Why do tracking/loading choices matter?
- **[L3]** How do you choose EF Core versus Dapper?
- **[L3]** How do you prove a data-access implementation is production-ready?

## Interview Answers
1. C# types/LINQ plus SQL querying, schema, transactions, indexes, and relationships.
2. A short-lived unit of work with identity/change tracking, query translation, and persistence.
3. EF builds an expression tree and provider translates supported operations into SQL executed by the database.
4. They control memory, query count, materialization, updates, concurrency, and latency.
5. Use EF for domain persistence/modeling and Dapper/raw SQL for explicit/tuned queries when measured needs justify it.
6. Integration-test real provider behavior, inspect SQL/plans/rows/locks, secure tenant filters, test migrations/transactions/concurrency, and measure production-like load.

## Expert perspective
EF Core is not a substitute for SQL knowledge. The best data-access engineer understands both the object model and the database work it causes.
