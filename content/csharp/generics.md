---
id: csharp-generics
slug: generics
title: C# Generics, Constraints, Variance, and Type Safety
category: csharp
categoryTitle: C#
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-interfaces, csharp-collections]
tags: [csharp, generics, constraints, variance, boxing, type-safety]
relatedTopics: [csharp-types-nullability-patterns, csharp-linq]
order: 40
status: published
---
# C# Generics, Constraints, Variance, and Type Safety

## Introduction
Generics allow one algorithm/type to work with many types while preserving compile-time safety. A type parameter `T` is filled with a real type when the generic API is used.

```text
Generic algorithm<T>
        ↓ type supplied
Algorithm<Order>     or     Algorithm<Customer>
compiler checks operations and assignments
```

## Purpose
Generics prevent casts, reduce duplication, and avoid boxing for value types. Constraints tell the compiler what operations are valid for `T`.

## Simple example
```csharp
public static T Max<T>(T first, T second)
    where T : IComparable<T>
    => first.CompareTo(second) >= 0 ? first : second;

var value = Max(10, 20); // T becomes int
```

Without the constraint, the compiler cannot know that `T` supports `CompareTo`.

## Professional company-level example
```csharp
public interface IMessageHandler<in TMessage>
{
    Task HandleAsync(TMessage message, CancellationToken ct);
}

public sealed class OrderCreatedHandler : IMessageHandler<OrderCreated>
{
    public Task HandleAsync(OrderCreated message, CancellationToken ct)
    {
        // strongly typed event payload; no object cast
        return Task.CompletedTask;
    }
}
```

A typed message envelope/repository/result preserves relationships between input, output, and error types across a service rather than passing `object` around.

## Runtime behavior
For value-type instantiations, the runtime commonly generates specialized code to avoid boxing. Reference-type instantiations can often share generated code because references have the same pointer-sized representation. Generics are not only a compile-time feature; they influence JIT/code-size/allocation behavior.

## Constraints and variance

```csharp
where T : class
where T : struct
where T : notnull
where T : BaseType
where T : ICapability
where T : new()
```

`in` supports contravariance for consumers; `out` supports covariance for producers where the type relationship is safe. Do not add variance keywords without understanding which direction values flow.

## Common failure
A generic repository can hide important domain rules:

```csharp
IRepository<Order>.FindAsync(id); // may not enforce tenant/order-specific rules
```

A dedicated `IOrderRepository` may be clearer when queries require business-specific constraints. Generic reuse is not automatically good architecture.

## Comparison
| Approach | Strength | Risk |
|---|---|---|
| Generic API | Reuse/type safety | Can hide domain meaning |
| `object` | Flexible legacy boundary | Casts/boxing/runtime failures |
| Domain-specific API | Clear business intent | More explicit code |
| Generic constraint | Compiler-guaranteed capability | More complex signatures |

## Interview Questions
- **[L1]** Why use generics instead of `object`?
- **[L1]** What is a generic constraint?
- **[L2]** How do generics avoid boxing?
- **[L2]** What are covariance and contravariance used for?
- **[L3]** When is a domain-specific abstraction better than a generic repository?
- **[L3]** What runtime trade-offs can highly generic code introduce?

## Interview Answers
1. Generics preserve type safety and avoid repeated casts; value-type generic collections also avoid many boxing allocations.
2. A constraint restricts valid `T` types and grants the compiler permission to use guaranteed members/capabilities.
3. `List<int>` stores ints in a typed structure rather than wrapping each as an `object` heap allocation.
4. Covariance lets a producer of a more specific type be used where a producer of a less specific type is expected; contravariance reverses safe consumer direction.
5. Use a domain-specific repository when queries, tenant rules, and invariants are business-specific rather than simple reusable operations.
6. Value-type specialization can increase native code size; deep generic APIs can hurt readability/diagnostics; measure code size and allocations before optimizing.

## Expert perspective
Generics are strongest when the type relationship communicates a correctness guarantee. Senior engineers resist generic abstractions that erase domain language merely to reduce the number of classes.
