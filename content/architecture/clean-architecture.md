---
id: architecture-clean
slug: clean-architecture
title: Clean Architecture and Dependency Inversion
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 45
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-fundamentals]
tags: [architecture, clean-architecture, dependency-inversion, boundaries]
relatedTopics: [architecture-fundamentals, architecture-microservices]
order: 20
status: published
---
# Clean Architecture and Dependency Inversion

## Overview
Clean Architecture organizes a system so business rules are independent of frameworks, databases, UI, and external services. Dependencies point inward toward policy, while infrastructure details implement interfaces owned by inner layers.

## Why This Exists
Frameworks and databases change, but core business rules should not need to change because an HTTP library was replaced or a database vendor changed. Dependency inversion keeps volatile delivery mechanisms outside the domain and makes policy easier to test.

## Fundamentals
A common arrangement is Domain → Application → Infrastructure → Web. The domain owns business entities and rules. Application coordinates use cases. Infrastructure implements persistence and integrations. The Web layer translates HTTP requests into application commands.

## Syntax and API
```csharp
// Application layer owns the port
public interface IOrderRepository
{
    Task<Order?> FindAsync(OrderId id, CancellationToken cancellationToken);
    Task SaveAsync(Order order, CancellationToken cancellationToken);
}

public sealed class SubmitOrderHandler(IOrderRepository repository)
{
    public async Task HandleAsync(OrderId id, CancellationToken cancellationToken)
    {
        var order = await repository.FindAsync(id, cancellationToken)
            ?? throw new OrderNotFoundException(id);
        order.Submit();
        await repository.SaveAsync(order, cancellationToken);
    }
}
```

## How It Works
The inner layer defines abstractions based on business needs. The outer infrastructure layer references the inner layer and implements those abstractions. Dependency injection connects the implementation at startup without making the use case depend on EF Core or ASP.NET.

## Internal Implementation
The dependency graph is enforced by project references: Domain should reference no infrastructure package; Application references Domain; Infrastructure references Application/Domain; Web composes everything. This is an enforceable build-time boundary, not merely a folder convention.

## Real-World Example
The order submission use case can be tested with an in-memory fake repository without starting a web server or database.

## Production Example
An EF Core repository implements `IOrderRepository`, while an API controller maps a request DTO to `SubmitOrderCommand`. The domain never sees HTTP status codes, EF tracking, or JSON serialization attributes.

## Common Mistakes
- Creating interfaces for every class without a boundary need.
- Letting domain entities depend on EF Core attributes or HTTP types.
- Making Application an anemic pass-through layer.
- Treating folders as boundaries while project references still point everywhere.

## Performance
Additional abstractions usually cost far less than database/network I/O. Avoid excessive mapping and generic indirection only when profiling shows it matters.

## Security
Keep authorization policy in application/domain decisions, not only controllers. Infrastructure must not bypass tenant or permission checks simply because it has database access.

## Testing
Unit-test domain/application layers with fakes; integration-test infrastructure adapters; contract-test API boundaries. Architecture tests can reject forbidden project references.

## When to Use
Use Clean Architecture when business rules are valuable, long-lived, and need protection from framework churn or infrastructure replacement.

## When Not to Use
Do not introduce four projects and extensive mapping for a tiny CRUD tool with no meaningful domain behavior.

## Trade-offs
It improves testability and replaceability but adds project boundaries, mapping, and navigation overhead.

## Related Topics
See [Architecture Fundamentals](/architecture/fundamentals) and [Microservices Trade-offs](/architecture/microservices).

## Practical Exercise
Split an order API into Domain, Application, Infrastructure, and Web projects. Make the application handler testable without EF Core or ASP.NET.

## Interview Questions
- **[L1]** What is the Dependency Inversion Principle?
- **[L1]** What belongs in the domain layer?
- **[L2]** Why should inner layers not reference infrastructure frameworks?
- **[L2]** How do project references enforce Clean Architecture?
- **[L3]** When is Clean Architecture over-engineering?

## Interview Answers
1. **[L1]** High-level policy should depend on abstractions, while low-level details implement those abstractions. The direction of source-code dependencies points toward stable business policy.
2. **[L1]** The domain contains business concepts and invariants independent of HTTP, databases, UI, and external providers.
3. **[L2]** This prevents framework and infrastructure changes from forcing business-rule changes and makes core behavior testable without external systems.
4. **[L2]** Separate projects can forbid inward layers from referencing outward packages. The compiler then enforces the dependency direction rather than relying on developer discipline.
5. **[L3]** It is excessive when the system is simple, short-lived, or has no meaningful domain logic. Use the smallest boundary that protects a real change or testing need.

## Senior Developer Perspective
Clean Architecture is valuable when it protects a volatile boundary or important policy. It is not valuable because a diagram has concentric circles; if the layers do not enforce dependency direction, the architecture is mostly naming.
