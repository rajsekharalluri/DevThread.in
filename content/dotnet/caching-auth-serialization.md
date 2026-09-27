---
id: dotnet-caching-auth-serialization
slug: caching-auth-serialization
title: .NET Caching, Authentication, Authorization, and Serialization
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: ".NET 8+ / ASP.NET Core"
prerequisites: [dotnet-aspnet-core-backend, dotnet-di-configuration-options]
tags: [dotnet, caching, jwt, authorization, serialization]
relatedTopics: [dotnet-logging-diagnostics, architecture-resilience]
order: 80
status: published
---
# .NET Caching, Authentication, Authorization, and Serialization

## Introduction
Four concerns often appear together in an API but must be reasoned about separately:

```text
Authentication  → Who is calling?
Authorization   → What may that identity do?
Caching         → Can repeated work be avoided safely?
Serialization   → What exact data crosses the boundary?
```

## Purpose
A production API needs trustworthy identity, explicit permission checks, fast reads without dangerous staleness, and stable payload contracts that do not expose persistence details.

## Real-World Simple Example
```csharp
[Authorize(Policy = "orders:read")]
[HttpGet("{id:guid}")]
public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
{
    var tenantId = User.GetTenantId();
    var order = await service.GetAsync(id, tenantId, ct);
    return order is null ? NotFound() : Ok(order);
}
```
The token establishes identity, the policy checks permission, tenant scope protects ownership, and `OrderResponse` controls serialization.

## Professional production example
```csharp
var key = $"order-summary:{tenantId}:{orderId}";
var summary = await cache.GetOrCreateAsync(key, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2);
    return await repository.GetSummaryAsync(tenantId, orderId, ct);
});
```
A cache strategy must define TTL, invalidation, stampede control, authorization scope, fallback behavior, and what stale data is acceptable. Never cache a response without including tenant/user/permission dimensions in the key when the data is not public.

JWT validation should verify signature, trusted issuer, intended audience, expiry, not-before, algorithm policy, key rotation, and required claims. Authorization should use policies/permissions, not only a role string checked inconsistently in controllers.

## Serialization boundaries
```csharp
public sealed record OrderResponse(Guid Id, decimal Total, string Status);
```
Return DTOs instead of EF entities. This prevents navigation cycles, lazy-loading surprises, internal fields, and accidental breaking changes from becoming public API behavior. Configure `System.Text.Json` deliberately for naming, enum representation, nulls, dates, and maximum payload size.

## Comparison
| Choice | Strength | Risk |
|---|---|---|
| Memory cache | Fast local access | Not shared across instances |
| Distributed cache | Shared between instances | Network dependency and invalidation complexity |
| JWT | Stateless API identity | Revocation/key rotation design |
| Cookie | Browser session convenience | CSRF/session protection |
| DTO | Stable intentional contract | Mapping effort |
| Entity serialization | Less mapping initially | Leaks persistence behavior |

## Common production failures
- Serving one tenant's cached response to another tenant.
- Treating an expired JWT as authenticated because only its signature was checked.
- Cache stampede when thousands of requests miss the same key.
- Invalidating a cache after the response is already observed as stale.
- Returning database entities with internal fields or circular references.
- Treating authorization as a frontend concern.

## Interview Questions
- **[L1]** What is the difference between authentication and authorization?
- **[L1]** Why should an API return DTOs instead of database entities?
- **[L2]** What should JWT validation verify?
- **[L2]** What cache invalidation problems exist in production?
- **[L3]** How would you design caching for a multi-instance API with tenant-specific data?
- **[L3]** How would you design authorization for resource and tenant ownership?

## Interview Answers
1. Authentication establishes who the caller is; authorization decides what that identity may do.
2. DTOs protect the API contract from database shape, prevent internal fields/cycles from leaking, and make response versioning intentional.
3. Verify signature/algorithm policy, issuer, audience, expiry, not-before, signing-key rotation, and required claims.
4. Staleness, invalidation races, stampedes, unbounded growth, cache outages, and incorrect key scope are common problems.
5. Use keys containing tenant/resource/permission scope, bounded TTLs, explicit invalidation for critical writes, stampede protection, fallback behavior, and hit/staleness/error metrics.
6. Enforce permissions in the application service using authenticated identity, tenant context, and resource ownership; repeat checks on every relevant API operation.

## Expert perspective
Caching and security interact directly. A fast cached response is still a security vulnerability if its key ignores tenant or permission scope, and a valid JWT is still insufficient if the application never checks whether that identity owns the requested resource.
