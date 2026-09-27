---
id: programming-oop
slug: oop
title: Object-Oriented Programming
category: programming-fundamentals
categoryTitle: Programming Fundamentals
difficulty: intermediate
estimatedMinutes: 35
version:
  minimum: "Language agnostic"
prerequisites: []
tags: [oop, design, maintainability]
relatedTopics: [programming-solid, csharp-classes, csharp-interfaces]
order: 10
status: published
---
# Object-Oriented Programming

## Overview
Object-oriented programming models behavior and data together so a system can represent meaningful responsibilities as cohesive units instead of scattered logic.

## Why This Exists
Purely procedural code lets any function read and mutate any data, which makes it hard to know what can change a value or what invariants hold at any point. OOP assigns behavior to the objects that own the relevant state, so change is localized and invariants can be enforced at a single boundary.

## Fundamentals
Four ideas recur across OOP languages:
- **Encapsulation** — hide internal state, expose behavior.
- **Abstraction** — model what something does, not how, from the caller's perspective.
- **Inheritance** — share and specialize behavior through a type hierarchy.
- **Polymorphism** — call the same operation on different types and get type-appropriate behavior.
Composition (building objects out of other objects) is often a better tool than inheritance for reuse.

## Syntax and API
```csharp
public interface IPaymentMethod
{
    Task<PaymentResult> AuthorizeAsync(Money amount, CancellationToken cancellationToken);
}

public sealed class CreditCardPayment(ICardGateway gateway) : IPaymentMethod
{
    public async Task<PaymentResult> AuthorizeAsync(Money amount, CancellationToken cancellationToken)
    {
        var response = await gateway.ChargeAsync(amount, cancellationToken);
        return response.Approved ? PaymentResult.Approved(response.TransactionId) : PaymentResult.Declined(response.Reason);
    }
}

public sealed class Order
{
    private readonly List<OrderLine> _lines = [];
    public IReadOnlyList<OrderLine> Lines => _lines;

    public void AddLine(ProductId productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new OrderLine(productId, quantity));
    }
}
```
`CreditCardPayment` and, say, `PayPalPayment` both implement `IPaymentMethod` — this is polymorphism: the order workflow calls `AuthorizeAsync` without caring which concrete type it holds.

## How It Works
Calls are dispatched statically (non-virtual members: resolved at compile time based on the declared type) or virtually (dispatched at run time based on the actual object type, via `virtual`/`override` or interface implementation). Composition avoids deep inheritance trees by assembling behavior from smaller, independently testable objects instead of inheriting it.

## Internal Implementation
A managed object carries runtime type metadata (its method table) alongside its instance data. Virtual/interface dispatch looks up the correct implementation through that method table at the call site, which is what makes polymorphism work — the same call site (`payment.AuthorizeAsync(...)`) invokes different code depending on the runtime type of `payment`.

## Real-World Example
An `Order` aggregate owns its line-item invariants (no negative quantities, no lines added after submission) instead of letting a controller or mapping layer mutate a public list directly.

## Production Example
```csharp
public sealed class OrderCheckoutService(IEnumerable<IPaymentMethod> paymentMethods)
{
    public async Task<PaymentResult> ChargeAsync(string methodName, Order order, CancellationToken cancellationToken)
    {
        var method = paymentMethods.OfType<IPaymentMethod>()
            .FirstOrDefault(m => m.GetType().Name.Equals(methodName, StringComparison.OrdinalIgnoreCase))
            ?? throw new UnsupportedPaymentMethodException(methodName);

        return await method.AuthorizeAsync(order.Total, cancellationToken);
    }
}
```
Each payment method implements the same authorization contract while handling its own protocol details internally — adding a new payment provider means adding a new class, not modifying `OrderCheckoutService`.

## Common Mistakes
Bad — an anemic object with no behavior forces logic into unrelated services and invites invariant violations:
```csharp
public sealed class Order
{
    public List<OrderLine> Lines { get; set; } = [];
    public decimal Total { get; set; }
}
// somewhere else entirely:
order.Lines.Add(new OrderLine(productId, -3)); // nothing prevents this
order.Total = order.Lines.Sum(l => l.Quantity * l.UnitPrice); // easy to forget or get wrong
```
Better — the object protects its own invariants and computes its own derived state:
```csharp
public sealed class Order
{
    private readonly List<OrderLine> _lines = [];
    public IReadOnlyList<OrderLine> Lines => _lines;
    public decimal Total => _lines.Sum(l => l.Quantity * l.UnitPrice);
    public void AddLine(ProductId productId, int quantity)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        _lines.Add(new OrderLine(productId, quantity));
    }
}
```
Other common mistakes: using inheritance purely for code reuse when composition would be clearer and more flexible; making every setter public "just in case" something needs to change it later.

## Performance
Object boundaries add small allocation and virtual-dispatch costs, but in most business applications, coupling, correctness, and maintainability dominate the actual cost that matters. Profile hot paths before flattening object boundaries for performance.

## Security
Keep authorization decisions near the business operation they protect (e.g., inside `AuthorizeAsync`, not only at the API controller), and avoid exposing internal object state through unrestricted serialization (a domain entity serialized directly to JSON can leak fields never meant for clients).

## Testing
Test invariants at the object's own boundary (can you construct or mutate it into an invalid state?), and write contract tests for polymorphic implementations (every `IPaymentMethod` implementation must handle a declined charge the same way).

## When to Use
Use OOP when the domain has genuine stateful behavior, invariants to protect, and interchangeable policies (multiple implementations of the same behavior).

## When Not to Use
Prefer plain functions and data pipelines for straightforward, stateless transformations with no meaningful lifecycle — wrapping a pure function in a single-method class adds ceremony without benefit.

## Trade-offs
OOP improves cohesion and encapsulation but can become ceremony when every concept — including simple data or a single function — is forced into a class hierarchy.

## Related Topics
See [SOLID Principles](/programming-fundamentals/solid) and [C# Interfaces](/csharp/interfaces).

## Practical Exercise
Model an `Order` that prevents adding a line with a non-positive quantity and prevents any mutation after the order has been submitted, then add two `IPaymentMethod` implementations and a contract test both must pass.

## Interview Questions
- **[L1]** What are the four commonly cited OOP principles?
- **[L1]** What is the difference between encapsulation and abstraction?
- **[L2]** Why is composition often preferred over inheritance?
- **[L2]** How does virtual dispatch enable polymorphism, and what does it cost?
- **[L3]** How do you prevent domain objects from becoming anemic in a large codebase?

## Interview Answers
1. **[L1] What are the four commonly cited OOP principles?**
   Encapsulation (hiding internal state behind behavior), abstraction (exposing what an object does, not how), inheritance (sharing and specializing behavior through a type hierarchy), and polymorphism (invoking the same operation on different types and getting type-appropriate behavior).
2. **[L1] What is the difference between encapsulation and abstraction?**
   Encapsulation is about hiding implementation details and protecting invariants — private fields, public behavior methods. Abstraction is about designing a simplified interface that hides irrelevant detail from the caller's perspective — for example, `IPaymentMethod.AuthorizeAsync` abstracts away whether a card, wallet, or bank transfer is used. Encapsulation is a mechanism; abstraction is a design goal that encapsulation helps achieve.
3. **[L2] Why is composition often preferred over inheritance?**
   Inheritance creates a tight, static coupling between base and derived classes — a change to the base class can silently break every subclass ("fragile base class" problem), and a class can usually only extend one base class. Composition builds behavior out of smaller, independently testable objects assembled at runtime, which is more flexible (you can swap a component), easier to test in isolation, and avoids deep, hard-to-reason-about hierarchies.
4. **[L2] How does virtual dispatch enable polymorphism, and what does it cost?**
   Virtual dispatch resolves which method implementation to call at runtime based on the object's actual type, via a lookup through the type's method table, rather than at compile time based on the declared/static type. This is what allows one call site (`payment.AuthorizeAsync(...)`) to run different code for `CreditCardPayment` vs `PayPalPayment`. The cost is a small indirect call overhead versus a direct/non-virtual call, and it can inhibit some compiler/JIT inlining optimizations — usually negligible next to I/O costs, but relevant in extremely hot code paths.
5. **[L3] How do you prevent domain objects from becoming anemic in a large codebase?**
   Push behavior and validation into the object that owns the relevant state instead of into external "manager" or "service" classes that just read and write public properties. Concretely: make collections private with controlled mutation methods, compute derived values as properties/methods on the object rather than externally, and treat any new business rule as a question of "which object should own this?" rather than "which service should implement this?" first. Code review discipline (rejecting new public setters without justification) and periodically auditing whether services are doing work that belongs on an entity both help sustain this over time.

## Senior Developer Perspective
OOP is a tool for managing change, not a goal in itself. Senior engineers optimize for stable, well-defined boundaries, explicit invariants enforced at the object level, and low coupling — and they recognize when a "simple function" is genuinely simpler than a class, rather than reflexively wrapping everything in objects.
