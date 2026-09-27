---
id: programming-solid
slug: solid
title: SOLID Principles
category: programming-fundamentals
categoryTitle: Programming Fundamentals
difficulty: intermediate
estimatedMinutes: 40
version:
  minimum: "Language agnostic"
prerequisites: [programming-oop]
tags: [solid, design, maintainability]
relatedTopics: [programming-oop, architecture-clean]
order: 20
status: published
---
# SOLID Principles

## Overview
SOLID is a set of five design heuristics — Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, and Dependency Inversion — for keeping object-oriented code changeable and testable as it grows.

## Why This Exists
Large systems become expensive to change when a small requirement change forces unrelated components to change or redeploy together. SOLID names the specific coupling and cohesion problems that cause this, so teams have a shared vocabulary for diagnosing and fixing them.

## Fundamentals
- **S**ingle Responsibility — a class should have one reason to change.
- **O**pen/Closed — behavior should be extensible without modifying existing, tested code.
- **L**iskov Substitution — a subtype must be usable anywhere its base type is expected, without surprising behavior.
- **I**nterface Segregation — clients shouldn't depend on methods they don't use.
- **D**ependency Inversion — high-level policy should depend on abstractions, not on low-level details.

## Syntax and API
```csharp
// Dependency Inversion: the domain depends on an abstraction it defines...
public interface IPaymentAuthorizer
{
    Task<AuthorizationResult> AuthorizeAsync(Payment payment, CancellationToken cancellationToken);
}

// ...and infrastructure implements it, isolated behind an adapter.
public sealed class StripePaymentAuthorizer(IStripeClient stripeClient) : IPaymentAuthorizer
{
    public async Task<AuthorizationResult> AuthorizeAsync(Payment payment, CancellationToken cancellationToken)
    {
        var response = await stripeClient.ChargeAsync(payment.Amount, payment.Token, cancellationToken);
        return response.Success
            ? AuthorizationResult.Approved(response.ChargeId)
            : AuthorizationResult.Declined(response.FailureReason);
    }
}
```

## How It Works
The principles guide dependency direction (which module knows about which) and responsibility boundaries (what one unit of code is allowed to change for). They are not rules requiring exactly one interface per class or one method per class — applied mechanically, they can create more indirection than value.

## Internal Implementation
Dependency inversion is realized through DI containers at the application's composition root: the order workflow (high-level policy) declares a dependency on `IPaymentAuthorizer`, and the container supplies `StripePaymentAuthorizer` (a low-level detail) at startup. The domain assembly never references the Stripe SDK directly, which is what "depending on abstractions, not details" means in practice.

## Real-World Example
An order workflow depends on `IPaymentAuthorizer`, not a concrete Stripe client — swapping to a new payment provider, or adding a second one, does not require touching the order workflow's code.

## Production Example
```csharp
public sealed class OrderCheckoutHandler(IPaymentAuthorizer authorizer, IOrderRepository orders)
{
    public async Task<CheckoutResult> HandleAsync(CheckoutCommand command, CancellationToken cancellationToken)
    {
        var order = await orders.FindAsync(command.OrderId, cancellationToken)
            ?? throw new OrderNotFoundException(command.OrderId);

        var authorization = await authorizer.AuthorizeAsync(
            new Payment(order.Total, command.PaymentToken), cancellationToken);

        if (!authorization.IsApproved)
            return CheckoutResult.Declined(authorization.Reason);

        order.MarkPaid(authorization.TransactionId);
        await orders.SaveAsync(order, cancellationToken);
        return CheckoutResult.Approved(order.Id);
    }
}
```
`StripePaymentAuthorizer` isolates retries, authentication, and Stripe-specific error mapping — `OrderCheckoutHandler` only ever sees `AuthorizationResult`, so a provider outage or API change never leaks into the order domain.

## Common Mistakes
Bad — a Liskov Substitution violation: a subtype that breaks the base type's contract:
```csharp
public class ReadOnlyRepository<T> : IRepository<T>
{
    public Task SaveAsync(T entity, CancellationToken ct) =>
        throw new NotSupportedException(); // callers expecting IRepository<T> now get surprise exceptions
}
```
Better — model the real capability instead of forcing an incompatible type into the hierarchy:
```csharp
public interface IReadOnlyRepository<T> { Task<T?> FindAsync(Guid id, CancellationToken ct); }
public interface IRepository<T> : IReadOnlyRepository<T> { Task SaveAsync(T entity, CancellationToken ct); }
```
Other common mistakes: splitting genuinely cohesive code into meaningless classes chasing "single responsibility"; treating Open/Closed as "never modify existing code" (fixing a bug is still modifying code); and adding an interface/abstraction with no second implementation or test seam to justify it.

## Performance
The extra indirection from interfaces and dependency injection is normally insignificant compared with network and database I/O. Excessive allocations, reflection-heavy DI resolution, or overly generic abstraction layers are separate, measurable concerns — profile before assuming SOLID-driven indirection is a bottleneck.

## Security
Keep security policy (authorization checks, input validation) in the high-level workflow, and make sure low-level adapters cannot bypass it — for example, a caching decorator around `IPaymentAuthorizer` must not accidentally skip authorization on a cache hit.

## Testing
Unit-test policy code (`OrderCheckoutHandler`) with fakes for `IPaymentAuthorizer`, and integration-test each concrete adapter (`StripePaymentAuthorizer`) against the real provider's sandbox/contract to make sure the abstraction's assumptions hold in practice.

## When to Use
Use SOLID as a diagnostic vocabulary when code is difficult to change, test, or reason about — e.g., "this class has three unrelated reasons to change" (SRP violation) or "this subtype throws where the base type promises success" (LSP violation).

## When Not to Use
Do not apply SOLID mechanically to small scripts, prototypes, or genuinely stable code with no meaningful variation — introducing interfaces and abstractions there adds cost with no corresponding flexibility benefit.

## Trade-offs
Better boundaries improve long-term maintainability and testability but introduce more interfaces, DI registrations, and files to navigate — the right amount of SOLID is proportional to how much a given piece of code is expected to change.

## Related Topics
See [Object-Oriented Programming](/programming-fundamentals/oop) and [C# Interfaces](/csharp/interfaces).

## Practical Exercise
Refactor a `PaymentService` that validates a payment, charges a card, sends a confirmation email, and writes an audit record all in one method into separate, independently testable policies connected through interfaces.

## Interview Questions
- **[L1]** What problem does the Single Responsibility Principle address?
- **[L1]** What is Dependency Inversion, illustrated with a payment provider example?
- **[L2]** What is a practical, concrete violation of the Liskov Substitution Principle?
- **[L2]** How does Interface Segregation differ from just "having small interfaces"?
- **[L3]** When is introducing an abstraction premature, and how do you recognize it in code review?

## Interview Answers
1. **[L1] What problem does the Single Responsibility Principle address?**
   It addresses classes that have more than one reason to change — for example, a class that both validates orders and formats them for a report will need to change for validation-rule reasons and for reporting-format reasons, and a change to one risks breaking the other. SRP says to split those into separate classes so each has one clear reason to change, reducing accidental coupling between unrelated concerns.
2. **[L1] What is Dependency Inversion, illustrated with a payment provider example?**
   Dependency Inversion means high-level policy (the order checkout workflow) should depend on an abstraction it defines (`IPaymentAuthorizer`), and low-level details (a concrete Stripe or PayPal client) should implement that abstraction — not the other way around. In code, this means the order workflow's assembly has no reference to the Stripe SDK at all; the concrete `StripePaymentAuthorizer` is wired in only at the application's composition root via dependency injection.
3. **[L2] What is a practical, concrete violation of the Liskov Substitution Principle?**
   A subtype that throws `NotSupportedException` for a method the base type/interface promises to support — e.g., a `ReadOnlyRepository<T>` implementing `IRepository<T>.SaveAsync` by throwing. Any code written against `IRepository<T>` reasonably assumes `SaveAsync` works, and substituting `ReadOnlyRepository<T>` silently breaks that assumption at runtime instead of at compile time — the fix is to model the real capability with a smaller interface (`IReadOnlyRepository<T>`) rather than forcing an incompatible type into the hierarchy.
4. **[L2] How does Interface Segregation differ from just "having small interfaces"?**
   Interface Segregation is about matching an interface to what a specific client actually needs, not merely keeping method counts low. A small interface can still be wrong if it forces unrelated clients to depend on methods they don't use — the real test is "does every consumer of this interface use (or reasonably need) every member?" If not, split it per client need, even if that means several small, client-specific interfaces rather than one "small-ish" general one.
5. **[L3] When is introducing an abstraction premature, and how do you recognize it in code review?**
   An abstraction is premature when it has exactly one implementation, no test seam that requires it, and no concrete near-term plan for a second implementation — it's speculative flexibility that adds indirection without paying for itself. In code review, warning signs include: an interface whose only implementation lives in the same commit, a "just in case" comment justifying the abstraction, and reviewers being unable to name a second real implementation or testing reason for it. The fix is usually to keep the concrete type until a second real need appears, then extract the interface at that point — extraction is cheap; premature abstraction is a standing maintenance cost.

## Senior Developer Perspective
SOLID is about managing change and dependency direction, not maximizing interface or class count. Senior engineers apply the smallest boundary that protects a genuinely volatile policy or a valuable invariant, and they can articulate, for every abstraction in the codebase, what real-world change or test need it exists to isolate.
