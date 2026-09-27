---
id: architecture-hexagonal-onion
slug: hexagonal-onion
title: Hexagonal, Onion, and Layered Architecture
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 50
version:
  minimum: "Architecture patterns"
prerequisites: [architecture-clean, architecture-fundamentals]
tags: [hexagonal, onion, layered, ports-adapters, architecture]
relatedTopics: [architecture-modular-monolith, architecture-design-patterns-ddd]
order: 180
status: published
---
# Hexagonal, Onion, and Layered Architecture

## Introduction
These patterns organize dependencies so business policy is not controlled by UI, database, framework, or provider details.

```text
                 UI/API adapter
                       │
Infrastructure adapter ─┼─ Application/domain policy ─┼─ Message adapter
                       │
                 Database adapter

Dependency direction points inward toward policy.
```

Hexagonal Architecture calls integration boundaries ports and adapters. Onion Architecture places domain policy at the center with dependencies pointing inward. Layered Architecture separates presentation/application/domain/infrastructure, but may still allow poor dependency direction if not enforced.

## Purpose
The goal is replaceability and testability: business rules can be tested without starting a database or HTTP server, while infrastructure implements interfaces owned by the inner policy layer.

## Simple example
```csharp
// Application owns the port
public interface IOrderRepository
{
    Task<Order?> FindAsync(OrderId id, CancellationToken ct);
}

// Web/Infrastructure supplies the adapter
public sealed class EfOrderRepository(AppDbContext db) : IOrderRepository
{
    public Task<Order?> FindAsync(OrderId id, CancellationToken ct) =>
        db.Orders.SingleOrDefaultAsync(o => o.Id == id, ct);
}
```

The use case depends on `IOrderRepository`, not EF Core.

## Professional company-level example
```text
Domain: entities/value objects/invariants
Application: use cases + ports
Infrastructure: EF, Kafka, HTTP provider adapters
Web: controllers/serialization/authentication
Composition root: connects implementations
```

Project references should enforce the design:

```text
Domain       → no infrastructure
Application  → Domain
Infrastructure → Application/Domain
Web          → Application + Infrastructure composition
```

## Comparison
| Pattern | Main idea |
|---|---|
| Layered | Organize by technical responsibility |
| Onion | Inner domain, dependencies point inward |
| Hexagonal | Ports for policy, adapters for external systems |
| Clean | Dependency inversion + use-case/domain boundaries |
| Modular monolith | Independent business modules in one deployable |

## Interview Questions
- **[L1]** What is Hexagonal Architecture?
- **[L1]** What is a port and what is an adapter?
- **[L2]** Why should domain code not depend on EF Core or ASP.NET?
- **[L2]** How can project references enforce architecture?
- **[L3]** When is this architecture too much ceremony?
- **[L3]** How do you prevent an abstraction-first architecture from becoming difficult to navigate?

## Interview Answers
1. It isolates core policy from external systems using ports and adapters.
2. A port is an inner-owned capability contract; an adapter implements/translates an external technology into that contract.
3. Framework details change and make testing/domain reuse harder; inward dependencies protect long-lived policy.
4. Separate projects can forbid inner layers from referencing outward packages, making dependency direction a compiler-enforced rule.
5. It is excessive for simple CRUD or short-lived tools with no valuable domain policy or replacement/testing boundary.
6. Create abstractions at real volatility/ownership boundaries, keep interfaces small, avoid one-interface-per-class rules, and make the dependency graph discoverable.

## Expert perspective
Architecture patterns are useful when they protect a real policy or change boundary. The test is not whether folders look clean; it is whether an infrastructure change can occur without rewriting business rules and whether the resulting code remains understandable.
