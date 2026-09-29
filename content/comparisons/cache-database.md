---
id: comparisons-cache-database
slug: cache-database
title: Cache vs Database vs Read Model
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "Distributed data concepts, .NET 8+"
prerequisites: [dotnet-caching-auth-serialization, sql-denormalization]
tags: [cache, database, read-model, consistency, comparison]
relatedTopics: [sql-denormalization, architecture-cqrs]
order: 100
status: published
---
# Cache vs Database vs Read Model

## Introduction

When reads get slow or expensive, teams reach for "add a cache". Sometimes that is right. Sometimes the real fix is a better index in the **database**, and sometimes the workload needs a durable, specially shaped **read model**. These three differ in one fundamental way: **who owns the truth and what happens when data is lost or stale.**

- The **database** is the authoritative, durable record.
- A **cache** is a disposable copy kept close to the reader to save time or load. Losing it should only cost performance.
- A **read model** is a durable, derived projection shaped for a specific query, rebuilt from the source of truth.

## Quick Decision Table

| Concern | Database (source of truth) | Cache | Read Model |
|---|---|---|---|
| Authoritative? | Yes | No | No (derived) |
| Durability | Durable, backed up | Ephemeral (may be evicted any time) | Durable, rebuildable |
| Freshness | Always current | Stale up to TTL / invalidation | Lags source by projection delay |
| Shape | Normalized for integrity | Usually same shape as source response | Denormalized for one query pattern |
| Typical tech | PostgreSQL, SQL Server | IMemoryCache, Redis, CDN, HybridCache | Separate table, MongoDB, Elasticsearch, materialized view |
| Best for | Writes, invariants, transactions | Repeated identical reads, hot keys | Complex aggregations, search, dashboards |
| Main risk | Load and slow queries | Stale or wrong-tenant data, stampedes | Lag, rebuild cost, projection bugs |

## Option 1: Optimize the Database First

### When to Use

- Queries are slow because of missing indexes, poor query shape, N+1 access, or over-fetching
- Data must be perfectly current (balances, stock reservations)
- Load is moderate and the database has headroom

### Example: Fix Before You Cache

```sql
-- Slow: scans all orders for every dashboard load
SELECT customer_id, SUM(total) FROM orders WHERE tenant_id = @t AND created_at >= @from GROUP BY customer_id;

-- Add a covering index that matches the filter and aggregation
CREATE INDEX ix_orders_tenant_created ON orders (tenant_id, created_at) INCLUDE (customer_id, total);
```

Measure with the actual execution plan. Many "we need a cache" problems disappear with a correct index or a projection that selects only needed columns.

### When Not to Rely on the Database Alone

- The same expensive result is requested thousands of times per second
- Latency budgets are single-digit milliseconds across regions
- The query shape conflicts with the write model (full-text search, cross-aggregate dashboards)

## Option 2: Cache

### When to Use

- Many identical reads of data that changes rarely (product catalog, configuration, permissions snapshot)
- Expensive computations or remote calls with acceptable staleness
- Protecting a dependency from read spikes

### When Not to Use

- As the only copy of important data
- When correctness requires the latest value (payment status, inventory decrement)
- When the key space is enormous and hit rate would be low

### Cache-Aside with HybridCache (.NET 9+)

```csharp
public sealed class ProductReader(HybridCache cache, CatalogDb db)
{
    public async Task<ProductDto?> GetAsync(string tenantId, string sku, CancellationToken ct) =>
        await cache.GetOrCreateAsync(
            $"tenant:{tenantId}:product:{sku}:v2",          // tenant scope + schema version in the key
            async token => await db.Products.AsNoTracking()
                .Where(p => p.TenantId == tenantId && p.Sku == sku)
                .Select(p => new ProductDto(p.Sku, p.Name, p.Price))
                .FirstOrDefaultAsync(token),
            new HybridCacheEntryOptions { Expiration = TimeSpan.FromMinutes(10), LocalCacheExpiration = TimeSpan.FromMinutes(1) },
            tags: [$"tenant:{tenantId}:products"],
            cancellationToken: ct);

    public Task InvalidateTenantAsync(string tenantId, CancellationToken ct) =>
        cache.RemoveByTagAsync($"tenant:{tenantId}:products", ct).AsTask();
}
```

**Why each part exists:**
- **Tenant in the key** prevents one tenant seeing another tenant's data
- **Version suffix (`v2`)** lets you change the cached shape without deserializing old entries
- **Two-level cache** (local memory + distributed) cuts network hops for hot keys
- **Stampede protection** - `HybridCache` ensures only one caller rebuilds a missing key while others wait
- **Tags** allow bulk invalidation when a tenant's catalog is re-imported

### Invalidation Strategies

| Strategy | How | Trade-off |
|---|---|---|
| TTL | Entry expires after a time | Simple; stale up to TTL |
| Explicit invalidation on write | Remove/update key after commit | Fresher; must not miss any write path |
| Event-driven invalidation | Consumers of `ProductChanged` evict keys | Works across services; eventual |
| Versioned keys | Include data version in key | No deletes needed; old keys expire naturally |

## Option 3: Read Model

### When to Use

- The query needs a different shape than the write model (joins across aggregates, denormalized summaries, full-text search)
- The result must survive restarts and be queryable with its own indexes
- The same derived view is used constantly (dashboards, search pages, timelines)

### When Not to Use

- A simple index or cache would solve it
- Users cannot tolerate any lag between write and read
- The team cannot operate projection monitoring and rebuilds

### Projection Example

```csharp
public sealed class CustomerSummaryProjection(ReadDb read)
{
    public async Task Handle(OrderPaid evt, CancellationToken ct)
    {
        // Idempotent: the unique (event_id) constraint on processed_events rejects duplicates
        await read.Database.ExecuteSqlInterpolatedAsync($"""
            WITH ins AS (INSERT INTO processed_events(event_id) VALUES ({evt.EventId}) ON CONFLICT DO NOTHING RETURNING 1)
            INSERT INTO customer_summary(tenant_id, customer_id, order_count, lifetime_value, last_order_at)
            SELECT {evt.TenantId}, {evt.CustomerId}, 1, {evt.Total}, {evt.PaidAt} FROM ins
            ON CONFLICT (tenant_id, customer_id) DO UPDATE
              SET order_count = customer_summary.order_count + 1,
                  lifetime_value = customer_summary.lifetime_value + EXCLUDED.lifetime_value,
                  last_order_at = GREATEST(customer_summary.last_order_at, EXCLUDED.last_order_at)
            """, ct);
    }
}
```

**Operational requirements:** monitor projection lag, alert on handler failures, and support a **full rebuild** from the source (replay events or re-scan tables) because projection bugs will happen.

## Same Scenario: "Customer Dashboard Is Slow"

| Situation | Choose |
|---|---|
| Query scans without an index | Fix the database query/index |
| Same dashboard viewed repeatedly, data can be 1-5 minutes old | Cache the response per tenant+customer |
| Dashboard combines orders, payments, tickets, and loyalty across services | Read model built from events |
| Dashboard must show balance to the cent in real time | Database (possibly a replica with read-your-writes rules) |

## Decision Matrix

| Factor | Database | Cache | Read Model |
|---|---|---|---|
| Correctness requirement is strict | Strong | Weak | Medium (lag) |
| Read volume extremely high | Weak | Strong | Strong |
| Query shape differs from write model | Weak | Weak | Strong |
| Operational complexity | Low | Low-Medium | Medium-High |
| Data loss impact | Critical | None (performance only) | Rebuild required |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Caching before measuring the query plan | Fix indexes and query shape first |
| Keys without tenant/user scope | Include tenant and permission scope in keys |
| Caching authorization-sensitive responses globally | Cache per scope or cache raw data and authorize after retrieval |
| No stampede protection on hot keys | Use `HybridCache`, request coalescing, or early refresh |
| Treating Redis as durable storage | Keep truth in the database; Redis may evict |
| Read model with no rebuild path | Design replay/rebuild from day one |

## Interview Questions
- **[L1]** What is the purpose of a cache?
- **[L1]** Why is a cache usually not the source of truth?
- **[L2]** What is cache invalidation?
- **[L2]** How is a read model different from a cache?
- **[L3]** How do you avoid serving one tenant another tenant's cached data?
- **[L3]** When should a slow query become a read model instead of a cache entry?

## Interview Answers
1. A cache stores a copy of data or computed results closer to the consumer so repeated reads avoid expensive database queries, remote calls, or computation. This reduces latency, protects downstream systems from load spikes, and lowers cost, at the price of possibly serving slightly stale data.
2. Caches are designed to be disposable: entries expire, get evicted under memory pressure, are lost on restarts or failovers, and may be stale relative to the source. Durable truth needs transactions, constraints, backups, and recovery, which belong in the authoritative database. If losing the cache would lose business data, it is being misused as a database.
3. Cache invalidation is making cached entries stop being served after the underlying data changes. Techniques include TTL expiration, explicitly removing or updating keys after a successful write commit, event-driven eviction when other services change data, tag-based bulk invalidation, and versioned keys. Each has a defined staleness window, and the hard part is ensuring every write path triggers invalidation.
4. A cache is usually an ephemeral copy of the same data shape, optimized for speed, and can be dropped at any time. A read model is a durable, derived, often denormalized data store purpose-built for a query pattern, with its own schema and indexes, kept up to date by projections from source events or change data capture, and it can be rebuilt from the source of truth. Read models are queried like a database; caches are consulted like a shortcut.
5. Include the tenant ID (and, where responses depend on permissions, the user or role scope) in every cache key, derive it from the authenticated context on the server rather than from client input, and avoid caching responses that mix tenants. Use tenant-prefixed tags for invalidation, cache raw data and apply authorization after retrieval when permissions vary per user, and add tests that verify two tenants never receive each other's entries.
6. Use a read model when the query is structurally expensive rather than merely repeated: it needs joins or aggregations across aggregates or services, full-text or faceted search, or different indexes than the write model, and the result must be durable, queryable in many variations, and rebuildable. Use a cache when the same result is requested repeatedly for a short period and a TTL-bounded staleness is acceptable. Before either, verify that indexing and query shape cannot fix the problem.
