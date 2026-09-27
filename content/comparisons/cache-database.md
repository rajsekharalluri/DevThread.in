---
id: comparisons-cache-database
slug: cache-database
title: Cache vs Database vs Read Model
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 30
version:
  minimum: "Distributed data concepts"
prerequisites: [dotnet-caching-auth-serialization, sql-denormalization]
tags: [cache, database, read-model, consistency, comparison]
relatedTopics: [sql-denormalization, architecture-cqrs]
order: 100
status: published
---
# Cache vs Database vs Read Model

## Decision table

| Choice | Source of truth? | Best use | Main concern |
|---|---:|---|---|
| Database | Usually | Durable authoritative data | Query/load cost |
| Cache | No | Repeated safe reads | Staleness/invalidation |
| Read model | Derived | Specialized/query-heavy shape | Rebuild/lag |

## Example

```text
Write order → database (truth)
             ├── invalidate/update cache
             └── publish event → read model
GET summary → cache → read model/database fallback
```

Never treat a cache as authoritative unless the system explicitly accepts data loss/ephemerality. Cache keys must include tenant/security scope where needed.

## Interview Questions
- **[L1]** What is the purpose of a cache?
- **[L1]** Why is a cache usually not the source of truth?
- **[L2]** What is cache invalidation?
- **[L2]** How is a read model different from a cache?
- **[L3]** How do you avoid serving one tenant another tenant's cached data?
- **[L3]** When should a slow query become a read model instead of a cache entry?

## Interview Answers
1. Avoid repeated computation/remote reads and reduce latency/load.
2. It can expire/evict/fail and may be stale; durable truth belongs in authoritative storage.
3. Removing/updating cached data after source changes, using TTL/versioning/events and accepting defined freshness behavior.
4. A read model is a durable derived data shape often rebuilt from source events; a cache is usually ephemeral acceleration.
5. Include tenant/user/permission scope in keys and apply authorization before/after retrieval.
6. Use a read model when the query shape/aggregation is durable, repeated, and needs independent indexing/rebuild/freshness semantics rather than only short-lived reuse.
