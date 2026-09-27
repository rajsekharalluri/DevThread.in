---
id: csharp-types-nullability-patterns
slug: types-nullability-patterns
title: C# Types, Boxing, Nullability, and Pattern Matching
category: csharp
categoryTitle: C#
difficulty: intermediate
estimatedMinutes: 40
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-fundamentals, csharp-classes]
tags: [csharp, value-types, reference-types, boxing, nullable, pattern-matching]
relatedTopics: [csharp-generics, csharp-collections]
order: 70
status: published
---
# C# Types, Boxing, Nullability, and Pattern Matching

## Introduction / Definition
C# has value types, reference types, nullable annotations, conversions, boxing/unboxing, and pattern matching. These features determine how data is stored, copied, validated, and safely inspected.

## Purpose
Understanding types prevents subtle bugs: accidentally sharing mutable objects, boxing millions of values, dereferencing null, or using casts that fail at runtime.

## Real-World Simple Example
```csharp
int quantity = 3;              // value copied by assignment
var order = new Order();       // reference points to a heap object
object boxed = quantity;       // boxing allocates an object
int restored = (int)boxed;     // unboxing requires the exact value type

string? coupon = null;
if (coupon is { Length: > 0 } code)
    Console.WriteLine(code.ToUpperInvariant());
```

## Professional Company-Level Example
Use domain types, nullable annotations, and patterns at input boundaries:

```csharp
public abstract record PaymentResult;
public sealed record Approved(string TransactionId) : PaymentResult;
public sealed record Declined(string Reason) : PaymentResult;

public string Describe(PaymentResult result) => result switch
{
    Approved { TransactionId: var id } => $"Approved: {id}",
    Declined { Reason: var reason } => $"Declined: {reason}",
    _ => throw new ArgumentOutOfRangeException(nameof(result))
};
```
This makes supported states visible and forces new states to be considered when the compiler/analyzers are configured strictly.

## Advantages and Disadvantages
Value types can avoid separate heap allocations but may be copied. Reference types support identity and shared state but are GC-managed. Nullable annotations catch many mistakes at compile time but do not validate untrusted runtime data. Pattern matching is expressive, but overly complex patterns can hide business rules.

## Comparison
| Feature | Meaning | Typical use |
|---|---|---|
| `class` | Reference type with identity | Entities/services |
| `struct` | Value type copied by value | Small values |
| `record` | Value-oriented equality/data | DTOs/value objects |
| `T?` | Nullable reference/value annotation | Optional values |
| `is` pattern | Safe type/shape test | Parsing/result handling |

## Interview Questions
- **[L1]** What is the difference between value and reference types?
- **[L1]** What is boxing?
- **[L2]** Why can nullable reference types not replace runtime validation?
- **[L2]** How does pattern matching improve type/state handling?
- **[L3]** How would you choose between class, record, and struct for a domain concept?
- **[L3]** How would you find and reduce boxing in a high-throughput service?

## Interview Answers
1. Value types are copied by value; reference types hold references to heap objects and can share mutable identity.
2. Boxing wraps a value type in a heap object so it can be treated as `object` or an interface; it adds allocation and GC pressure.
3. Nullable analysis is compile-time guidance. JSON, database, reflection, casts, and external callers can still violate assumptions at runtime.
4. Patterns combine type checks, property checks, and exhaustive branching in readable expressions, reducing unsafe casts and nested conditionals.
5. Use a class for identity/lifecycle, a record for value/data semantics, and a struct only for small values where copying/layout are understood and measured.
6. Profile allocation/GC counters, inspect non-generic collections/interfaces, replace them with generics where appropriate, and re-measure allocation rate.
