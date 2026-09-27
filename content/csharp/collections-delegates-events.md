---
id: csharp-collections-delegates-events
slug: collections-delegates-events
title: C# Collections, Delegates, Lambdas, and Events
category: csharp
categoryTitle: C#
difficulty: intermediate
estimatedMinutes: 45
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-generics, csharp-types-nullability-patterns]
tags: [csharp, collections, delegates, events, lambdas]
relatedTopics: [csharp-linq, csharp-async-await]
order: 80
status: published
---
# C# Collections, Delegates, Lambdas, and Events

## Introduction / Definition
Collections store groups of values; delegates represent callable methods; lambdas create inline functions; events provide a controlled publish/subscribe notification mechanism.

## Purpose
Choosing the right collection affects lookup and memory cost. Delegates and events let application code compose policies and notify interested components without hard-coding every consumer.

## Real-World Simple Example
```csharp
var byId = new Dictionary<Guid, Order>();
var pending = new List<Order>();
var uniqueTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

Func<Order, bool> isLarge = order => order.Total > 1000;
var largeOrders = pending.Where(isLarge).ToList();
```
Use a `Dictionary` for key lookup, `List` for ordered iteration, and `HashSet` for uniqueness/membership.

## Professional Company-Level Example
```csharp
public sealed class OrderService
{
    public event EventHandler<OrderSubmittedEventArgs>? Submitted;

    public void Submit(Order order)
    {
        order.Submit();
        Submitted?.Invoke(this, new OrderSubmittedEventArgs(order.Id));
    }
}
```
An event should communicate that something happened. For cross-process reactions, use an integration event/message rather than an in-process .NET event.

## Advantages and Disadvantages
Generic collections provide type safety and avoid boxing. Concurrent collections help coordinate multiple threads but do not make compound business operations automatically atomic. Delegates are lightweight and composable, but long-lived subscriptions can retain objects and cause memory leaks. Events decouple publishers from consumers but can hide control flow.

## Comparison
| Need | Best choice |
|---|---|
| Ordered indexed values | `List<T>` |
| Key lookup | `Dictionary<TKey,TValue>` |
| Unique membership | `HashSet<T>` |
| LIFO/FIFO | `Stack<T>` / `Queue<T>` |
| Concurrent producer/consumer | `ConcurrentQueue<T>` / Channels |
| Cross-process notification | Message broker/event |

## Interview Questions
- **[L1]** When would you use a List, Dictionary, or HashSet?
- **[L1]** What is a delegate?
- **[L2]** What is the difference between an event and a public delegate?
- **[L2]** Why are concurrent collections not enough to make every operation thread-safe?
- **[L3]** How do you choose a collection for a high-throughput service?
- **[L3]** When should an in-process event become a durable integration event?

## Interview Answers
1. Use `List` for order/indexing, `Dictionary` for key lookup, and `HashSet` for uniqueness and membership.
2. A delegate is a type-safe reference to one or more callable methods and can be passed as data.
3. An event restricts external code to subscribing/unsubscribing; only the declaring type can raise it. A public delegate can be invoked/replaced by external code.
4. A thread-safe collection protects individual operations; a multi-step check-then-update still needs a lock or atomic design.
5. Start with access pattern, expected size, ordering, uniqueness, mutation/concurrency, allocation, and measured hot paths.
6. Use a durable integration event when consumers are separate processes/services, delivery must survive restarts, or replay/audit is required.
