---
id: csharp-interfaces
slug: interfaces
title: C# Interfaces and Dependency Inversion
category: csharp
categoryTitle: C#
difficulty: intermediate
estimatedMinutes: 45
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-classes, programming-solid]
tags: [csharp, interfaces, contracts, dependency-inversion, testing]
relatedTopics: [dotnet-di-configuration-options, architecture-clean]
order: 30
status: published
---
# C# Interfaces and Dependency Inversion

## Introduction
An interface is a contract describing a capability without prescribing the implementation. A consumer depends on the contract; one or more concrete classes implement it.

```text
Order workflow
      │ depends on
      ▼
IPaymentAuthorizer
      ▲
      │ implemented by
StripeAdapter / TestFake / PayPalAdapter
```

## Purpose
Interfaces are useful at volatile boundaries: payment providers, databases, queues, clocks, file systems, and policies that need independent testing or multiple implementations.

## Simple example
```csharp
public interface INotificationSender
{
    Task<SendResult> SendAsync(Notification notification, CancellationToken ct);
}

public sealed class EmailSender(IEmailClient client) : INotificationSender
{
    public async Task<SendResult> SendAsync(Notification notification, CancellationToken ct)
    {
        await client.SendAsync(notification, ct);
        return SendResult.Success;
    }
}
```

The caller knows the capability, not the provider.

## Professional company-level example
```csharp
public sealed class CheckoutHandler(
    IPaymentAuthorizer authorizer,
    IOrderRepository orders)
{
    public async Task<CheckoutResult> HandleAsync(
        CheckoutCommand command,
        CancellationToken ct)
    {
        var order = await orders.FindAsync(command.OrderId, ct)
            ?? throw new OrderNotFoundException(command.OrderId);

        var result = await authorizer.AuthorizeAsync(
            new Payment(order.Total, command.Token), ct);

        if (!result.Approved)
            return CheckoutResult.Declined(result.Reason);

        order.MarkPaid(result.TransactionId);
        await orders.SaveAsync(order, ct);
        return CheckoutResult.Success(order.Id);
    }
}
```

A Stripe adapter can be replaced without changing checkout policy. A fake authorizer can test the policy without a real gateway.

## Important design rules

- Prefer interfaces owned by the consumer/application boundary.
- Keep the contract small and behavior-focused.
- Include failure semantics in the result/exception contract.
- Do not create an interface for every class automatically.
- Do not hide important behavior behind a vague `Task` method.
- Register implementations at the composition root.

## Common failure
A “god interface” forces unrelated implementations to support unrelated methods:

```csharp
interface IEverything
{
    Task SendEmailAsync();
    Task SendSmsAsync();
    Task WriteAuditAsync();
    Task GetPreferencesAsync();
}
```

Split contracts around actual client needs instead of making every consumer depend on methods it does not use.

## Comparison
| Choice | Use when |
|---|---|
| Interface | Capability/volatile boundary |
| Abstract class | Shared implementation/invariant |
| Concrete class | Stable internal behavior |
| Delegate | Small callable policy |
| Adapter | External API translation |

## Interview Questions
- **[L1]** What is an interface?
- **[L1]** How does DI use interfaces?
- **[L2]** When is an interface a harmful abstraction?
- **[L2]** Why should failure semantics be part of an interface contract?
- **[L3]** How do interfaces support Clean Architecture?
- **[L3]** How do you decide whether to introduce an abstraction?

## Interview Answers
1. It is a contract of members/capabilities that implementing types must provide.
2. The composition root registers a concrete implementation against the interface, and consumers request the interface in constructors/injection.
3. One implementation, no substitution need, no testing seam, or a contract combining unrelated client responsibilities are warning signs.
4. Callers need to distinguish retryable, permanent, declined, or not-found outcomes without parsing provider-specific exceptions/messages.
5. Inner application/domain layers own ports; infrastructure adapters implement them. This keeps policy independent from databases/frameworks/providers.
6. Identify a real variation, boundary, or test need. Keep a stable concrete type simple until a second real requirement justifies extraction.

## Expert perspective
An interface is valuable when it protects a decision likely to change. More interfaces do not automatically mean better design; the goal is a boundary with clear ownership and useful behavior.
