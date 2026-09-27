---
id: csharp-classes
slug: classes
title: C# Classes, Objects, Constructors, Properties, and Encapsulation
category: csharp
categoryTitle: C#
difficulty: beginner
estimatedMinutes: 45
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-fundamentals, programming-oop]
tags: [csharp, classes, objects, constructors, properties, encapsulation]
relatedTopics: [csharp-types-nullability-patterns, csharp-interfaces]
order: 20
status: published
---
# C# Classes, Objects, Constructors, Properties, and Encapsulation

## Introduction
A class defines a reference type. An object is an instance of that class. Constructors establish valid initial state; fields hold internal state; properties expose controlled access; methods express behavior; access modifiers define the public boundary.

```text
Class definition
   ↓ new
Object instance
   ├── fields/state
   ├── properties
   ├── methods/behavior
   └── type metadata
```

## Purpose
Classes exist to put data and the rules that protect that data in one place. Encapsulation prevents arbitrary callers from creating invalid state or changing a lifecycle without permission.

## Simple example
```csharp
public sealed class InventoryReservation
{
    private readonly List<string> _history = [];

    public InventoryReservation(string sku, int quantity)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        Sku = sku;
        Quantity = quantity;
        Status = ReservationStatus.Reserved;
    }

    public string Sku { get; }
    public int Quantity { get; }
    public ReservationStatus Status { get; private set; }

    public void Cancel()
    {
        if (Status != ReservationStatus.Reserved)
            throw new InvalidOperationException("Only reserved stock can be cancelled.");
        Status = ReservationStatus.Cancelled;
    }
}
```

The caller cannot set `Status` directly or insert a negative quantity. The class owns those rules.

## Professional company-level example
An order aggregate should expose commands, not a mutable public list:

```csharp
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];

    public OrderId Id { get; }
    public IReadOnlyList<OrderLine> Lines => _lines;
    public OrderStatus Status { get; private set; } = OrderStatus.Draft;
    public Money Total => _lines.Aggregate(Money.Zero("USD"), (total, line) => total + line.Total);

    public void AddLine(ProductId productId, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Submitted orders cannot be changed.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new OrderLine(productId, quantity, unitPrice));
    }

    public void Submit()
    {
        if (_lines.Count == 0)
            throw new InvalidOperationException("An empty order cannot be submitted.");
        Status = OrderStatus.Submitted;
    }
}
```

A database/API layer can call `Submit()` but cannot bypass the aggregate's rules through public setters.

## Important behavior

- `private` hides implementation from other types.
- `protected` exposes behavior to derived classes and can weaken encapsulation.
- `internal` exposes members inside the assembly.
- `public` becomes part of the external contract.
- `init` allows assignment during construction/object initialization but not normal mutation afterward.
- `sealed` prevents inheritance and communicates that extension is not supported.
- Constructors should validate state, not make network/database calls.

## Comparison
| Type | Best fit | Key behavior |
|---|---|---|
| Class | Identity/lifecycle/behavior | Reference type |
| Record | Value/data semantics | Value equality by default |
| Struct | Small value | Copied by value |
| DTO | Transport contract | Usually no domain behavior |
| Entity | Identity and lifecycle | Enforces domain rules |

## Interview Questions
- **[L1]** What is the difference between a class and an object?
- **[L1]** Why should fields usually be private?
- **[L2]** Why should constructors avoid network/database calls?
- **[L2]** What does `sealed` communicate?
- **[L3]** How do you prevent an aggregate from exposing invalid mutations?
- **[L3]** Why should API DTOs usually be separate from domain entities?

## Interview Answers
1. A class is a definition/type; an object is a runtime instance created from that definition.
2. Private fields protect invariants and prevent unrelated callers from changing state without going through valid behavior.
3. Constructors should create in-memory valid state. I/O makes construction slow, failure-prone, hard to test, and difficult to retry/compose.
4. It means the type is not designed for inheritance. It can also enable runtime/JIT optimizations in some call paths.
5. Keep collections private, expose read-only views, use behavior methods, validate transitions, and keep setters private or absent.
6. Entities contain internal identity/invariants; DTOs are deliberate public contracts. Separating them prevents persistence/domain refactors from unintentionally breaking API clients or leaking fields.

## Expert perspective
A class is not merely a bag of properties. The most valuable question is: “Can any caller put this object into an invalid state?” If the answer is yes, the public boundary is probably too permissive.
