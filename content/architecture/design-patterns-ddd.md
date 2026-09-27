---
id: architecture-design-patterns-ddd
slug: design-patterns-ddd
title: Design Patterns and Domain-Driven Design
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 55
version:
  minimum: "Concept-level"
prerequisites: [programming-oop, programming-solid, architecture-fundamentals]
tags: [design-patterns, ddd, aggregates, domain-events]
relatedTopics: [architecture-clean, architecture-microservices, architecture-event-driven]
order: 100
status: published
---
# Design Patterns and Domain-Driven Design

## Introduction
A design pattern is a named solution to a recurring design problem. Domain-Driven Design (DDD) is a way to model complex business domains using shared language, bounded contexts, entities, value objects, aggregates, domain events, and explicit ownership.

```text
Business language
      ↓
Bounded context
      ├── entities/value objects
      ├── aggregate consistency boundary
      ├── domain services/policies
      └── domain events
```

## Purpose
Patterns give teams a shared vocabulary; DDD keeps important business rules out of controllers, database scripts, and external-provider adapters. Neither is a reason to add abstraction where simple code is clearer.

## Useful patterns

```text
Strategy   → interchangeable policy
Adapter    → translate external contract
Decorator  → add behavior without changing core
Factory    → controlled construction
Observer   → notify subscribers
Repository → persistence boundary (not automatically every class)
```

```csharp
public interface IShippingRateStrategy
{
    Money Calculate(Order order);
}

public sealed class StandardShipping : IShippingRateStrategy
{
    public Money Calculate(Order order) =>
        order.Total.Amount >= 100 ? Money.Zero("USD") : new(9.99m, "USD");
}
```

## DDD building blocks

- **Entity:** identity matters over time.
- **Value object:** values define equality; usually immutable.
- **Aggregate:** consistency boundary with an aggregate root controlling changes.
- **Domain event:** internal fact meaningful to the domain.
- **Integration event:** external/versioned contract for another context/service.
- **Bounded context:** boundary where language and model have one meaning.

## Professional company-level example
```text
Order.Submit()
      ↓ domain event: OrderSubmitted
transaction commits
      ↓ outbox
integration event: OrderSubmitted.v1
      ↓ broker
Inventory / Notifications / Analytics contexts react
```

An Order aggregate should own order invariants. A Payment context may use different language and data because “authorized,” “captured,” and “settled” mean different things from “submitted” or “fulfilled.” Do not force one giant shared domain model across contexts.

## Common failure scenarios
- Using patterns because their names sound senior.
- An aggregate spanning every table and becoming a lock bottleneck.
- Generic repositories hiding domain-specific queries.
- Domain events and integration events sharing an unstable internal class.
- Bounded contexts drawn by org chart instead of language/consistency.

## Comparison
| Concept | Meaning |
|---|---|
| Entity | Identity-based domain object |
| Value object | Value-based immutable concept |
| Aggregate | Transactional consistency boundary |
| Domain event | Internal business fact |
| Integration event | External versioned contract |
| Strategy | Pluggable behavior/policy |

## Interview Questions
- **[L1]** What is the Strategy pattern?
- **[L1]** What is the difference between an entity and a value object?
- **[L2]** What is an aggregate and why does its boundary matter?
- **[L2]** What is the difference between a domain event and an integration event?
- **[L3]** How do you decide whether a pattern or DDD abstraction is justified?
- **[L3]** How do bounded contexts prevent large systems from sharing a confused domain model?

## Interview Answers
1. Strategy puts interchangeable algorithms/policies behind a contract so callers do not grow conditional logic.
2. An entity has identity across changes; a value object is identified entirely by its values and is commonly immutable.
3. An aggregate controls changes that must be consistent together. A large boundary increases locking/transaction cost; a small one may require eventual consistency between facts.
4. A domain event is internal to a model/context; an integration event is an external, stable, versioned message for other contexts.
5. Identify a real variation, invariant, boundary, or testing need. If the pattern does not protect a decision or clarify business language, simple code is better.
6. Each context owns language, rules, data, and models for a cohesive capability; integration contracts translate between contexts instead of pretending one model fits all.

## Expert perspective
Patterns are names for useful decisions, not decorations. DDD succeeds when domain experts and engineers agree on language and boundaries; it fails when teams add aggregates, events, and repositories without a real business complexity to protect.
