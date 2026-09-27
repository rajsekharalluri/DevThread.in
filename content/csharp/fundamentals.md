---
id: csharp-fundamentals
slug: fundamentals
title: C# Fundamentals
category: csharp
categoryTitle: C#
difficulty: beginner
estimatedMinutes: 35
version:
  minimum: "12 / .NET 8+"
prerequisites: []
tags: [csharp, fundamentals, type-system, runtime]
relatedTopics: [csharp-classes, csharp-interfaces, dotnet-runtime-services]
order: 10
status: published
---
# C# Fundamentals

## Introduction / Definition
C# is a strongly typed, managed language running on .NET. It gives developers compile-time type checking, modern object-oriented features, asynchronous programming, and a runtime that manages memory through the garbage collector.

A C# application is compiled into an assembly containing IL and metadata. The CLR loads it and the JIT compiles methods into native instructions when they execute. This explains why C# combines static safety during development with runtime services such as GC, reflection, diagnostics, and exception handling.

## Purpose
C# exists to make large application codebases safer to change. Explicit types prevent accidental mixing of concepts such as `OrderId` and `CustomerId`; access control protects invariants; async APIs support scalable I/O; and the .NET ecosystem supplies production libraries for APIs, databases, messaging, and cloud services.

Think of types as executable documentation. This is stronger than passing primitives everywhere:

```csharp
public readonly record struct OrderId(Guid Value);
public readonly record struct CustomerId(Guid Value);

public sealed record Money(decimal Amount, string Currency)
{
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Currencies must match.");
        return this with { Amount = Amount + other.Amount };
    }
}
```

The compiler now rejects accidentally passing a `CustomerId` where an `OrderId` is required.

## Real-World Simple Example
An order service should accept a validated amount and a meaningful identifier, not three unrelated strings and numbers:

```csharp
public sealed class OrderService(IOrderRepository repository)
{
    public async Task<OrderId> CreateAsync(
        CustomerId customerId,
        Money total,
        CancellationToken cancellationToken)
    {
        if (total.Amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        var order = new Order(OrderId.New(), customerId, total);
        await repository.SaveAsync(order, cancellationToken);
        return order.Id;
    }
}
```
The method communicates its contract, rejects invalid input, and propagates cancellation to the database operation.

## Professional Company-Level Example
A production API normally separates transport DTOs from domain objects, validates at the boundary, applies authorization, and returns a stable response:

```csharp
[HttpPost("api/v1/orders")]
public async Task<ActionResult<CreateOrderResponse>> Create(
    CreateOrderRequest request,
    CancellationToken cancellationToken)
{
    var customerId = User.GetCustomerId();
    var orderId = await orders.CreateAsync(
        customerId,
        new Money(request.Amount, request.Currency),
        cancellationToken);

    return Created($"/api/v1/orders/{orderId.Value}",
        new CreateOrderResponse(orderId.Value));
}
```
The controller is intentionally thin. Business invariants belong in the application/domain layer; persistence, logging, authentication, and error mapping are handled by their appropriate boundaries. This separation makes the same use case testable without starting ASP.NET or a database.

## Advantages and Disadvantages
**Advantages**
- Strong compile-time feedback for large teams.
- Excellent async, HTTP, database, diagnostics, and cloud support.
- Mature tooling and a large ecosystem.
- Runtime memory management and deployment options.

**Disadvantages**
- Runtime behavior still requires understanding allocations, GC, thread pools, and configuration.
- Abstractions can become verbose or over-engineered.
- Incorrect async usage can still cause starvation.
- Version-specific framework behavior must be tracked.

## Comparison
| Choice | Best fit | Important trade-off |
|---|---|---|
| C# class | Stateful behavior and invariants | Reference-type lifecycle and mutability |
| C# record | Immutable data/value semantics | Not automatically a domain aggregate |
| C# struct | Small value with value semantics | Copying and boxing can be costly if misused |
| `Task`/`async` | I/O concurrency | Requires end-to-end async discipline |
| Thread | Dedicated execution control | Expensive and rarely needed for web I/O |

## Interview Questions
- **[L1]** What does the C# compiler produce before execution?
- **[L1]** What is the difference between a value type and a reference type?
- **[L1]** What is the difference between a class and a record?
- **[L1]** What problem does nullable reference type analysis address?
- **[L2]** What is boxing and why can it create performance problems?
- **[L2]** What roles do the CLR and JIT play?
- **[L2]** Why should domain identifiers use explicit types instead of raw `Guid` values?
- **[L2]** How does `async` improve server scalability?
- **[L3]** How would you investigate unexpected memory growth in a .NET API?
- **[L3]** How do you decide whether a value should be a class, record, or struct?
- **[L3]** How should a large team control language/framework version-specific behavior?
- **[L3]** What makes a C# backend production-ready beyond compiling successfully?

## Interview Answers
1. **[L1]** The compiler produces an assembly containing IL and metadata. The CLR loads it and the JIT compiles methods to native machine code.
2. **[L1]** Value types are copied by value; reference-type variables point to heap objects. The distinction affects mutation, allocation, copying, and nullability.
3. **[L1]** A class is a reference type commonly used for identity and behavior; a record emphasizes value-based equality and immutable data modeling by default.
4. **[L1]** It lets the compiler warn when a possibly-null reference is dereferenced, reducing runtime `NullReferenceException` bugs. It is not a runtime security or validation guarantee.
5. **[L2]** Boxing wraps a value type in a heap object so it can be treated as `object` or an interface. It adds allocation and GC pressure; generic collections usually avoid it.
6. **[L2]** The CLR provides runtime services and assembly execution; the JIT translates IL into native instructions, often optimizing based on runtime information.
7. **[L2]** Explicit identifiers make invalid argument swaps fail at compile time, document intent, and prevent every caller from treating domain concepts as interchangeable primitives.
8. **[L2]** Async releases worker threads while database/HTTP I/O waits, allowing more concurrent requests. It does not make CPU work faster and blocking calls undermine the benefit.
9. **[L3]** Measure allocation rate and GC behavior with counters/traces/profilers, identify high-allocation code paths, check caches and lifetimes, fix the measured cause, and verify under representative load.
10. **[L3]** Use a class for identity/lifecycle and behavior, a record for immutable value/data semantics, and a struct only for small value types where copying and layout are understood and measured.
11. **[L3]** Pin supported language/runtime versions, document version-sensitive behavior, run CI analyzers/tests against the supported matrix, and review upgrades as explicit engineering changes.
12. **[L3]** It has stable contracts, validation, authorization, safe error responses, async I/O, bounded resources, structured observability, secure configuration, meaningful tests, and deployment/rollback procedures.
