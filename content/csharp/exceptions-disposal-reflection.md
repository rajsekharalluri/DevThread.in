---
id: csharp-exceptions-disposal-reflection
slug: exceptions-disposal-reflection
title: C# Exceptions, Disposal, Reflection, and Attributes
category: csharp
categoryTitle: C#
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-classes, csharp-concurrency-tasks, dotnet-runtime-internals]
tags: [csharp, exceptions, disposable, reflection, attributes, resources]
relatedTopics: [dotnet-runtime-services, dotnet-aspnet-core-backend]
order: 100
status: published
---
# C# Exceptions, Disposal, Reflection, and Attributes

## Introduction
These features solve different boundary problems:

```text
Exceptions  → failure propagation
IDisposable → deterministic resource cleanup
Reflection  → inspect/use runtime metadata
Attributes  → attach metadata to code
```

## Purpose
Production services must distinguish recoverable/transient failures from programming bugs, release unmanaged/external resources deterministically, and sometimes inspect metadata for serializers, DI, test frameworks, or plugins.

## Exceptions
```csharp
public sealed class PaymentDeclinedException(string reason) : Exception(reason);

try
{
    await gateway.ChargeAsync(payment, ct);
}
catch (TransientGatewayException error)
{
    // retry policy may handle this
    throw;
}
catch (PaymentDeclinedException)
{
    // expected business outcome: map to a safe response
    return CheckoutResult.Declined();
}
```

Do not catch `Exception` everywhere and continue. Catch only what the current layer can handle. Translate infrastructure exceptions at boundaries and preserve cause:

```csharp
throw new PaymentUnavailableException(payment.Id, error);
```

## Disposal
`IDisposable` releases synchronous resources such as streams, handles, and connections. `IAsyncDisposable` releases resources whose cleanup requires asynchronous I/O.

```csharp
await using var response = await client.GetStreamAsync(uri, ct);
await response.CopyToAsync(destination, ct);
```

```text
using / await using
        ↓
try/finally cleanup
        ↓
resource released even on exception
```

Never dispose a dependency you do not own. A DI container normally disposes services it created; a method should dispose resources it created/owns.

## Attributes and reflection
```csharp
[Obsolete("Use NewApi instead")]
public void OldApi() { }

var attribute = typeof(Order).GetCustomAttributes(typeof(ObsoleteAttribute), inherit: true);
```

Attributes are metadata. Reflection can inspect members and invoke them, but it adds runtime cost and weakens compile-time visibility. Source generators or explicit registration are often better for hot paths/AOT-compatible code.

## Professional example
A plugin system may load assemblies and find classes marked with a custom attribute:

```csharp
[Handler("OrderPaid")]
public sealed class OrderPaidHandler : IEventHandler { }
```

At startup, scan known plugin assemblies, validate constructor/dependency requirements, register handlers, and fail clearly. Do not load arbitrary user-controlled assemblies into the main trusted process.

## Comparison
| Feature | Use | Main risk |
|---|---|---|
| Exception | Propagate/handle failure | Broad swallowing |
| `IDisposable` | Sync cleanup | Disposing non-owned resource |
| `IAsyncDisposable` | Async cleanup | Forgetting `await using` |
| Attribute | Declarative metadata | Hidden behavior |
| Reflection | Dynamic inspection | Runtime cost/AOT/security |
| Source generator | Build-time generated code | Build complexity |

## Interview Questions
- **[L1]** Why use `using`/`IDisposable`?
- **[L1]** What is the difference between `IDisposable` and `IAsyncDisposable`?
- **[L2]** Why is catching every `Exception` usually dangerous?
- **[L2]** What are attributes and reflection used for?
- **[L3]** When should reflection be replaced with source generation or explicit registration?
- **[L3]** How do you design exception ownership and cleanup across service boundaries?

## Interview Answers
1. They guarantee deterministic cleanup of owned resources, even when the body throws.
2. `IDisposable.Dispose` is synchronous; `IAsyncDisposable.DisposeAsync` supports asynchronous cleanup and is used with `await using`.
3. It can hide bugs, swallow cancellation, misclassify failures, and leave the system appearing successful when work failed.
4. Attributes attach metadata; reflection reads/uses it at runtime. Frameworks use them for serialization, testing, validation, and discovery.
5. Use explicit/source-generated approaches when startup/hot-path performance, trimming/AOT, type safety, or security makes dynamic discovery too costly/risky.
6. A layer handles only failures it can recover/translate; ownership is explicit, resources are disposed by their creator/owner, and remote failures are mapped without assuming a remote action did not happen.

## Expert perspective
Resource ownership and exception ownership should be visible in code. Reflection and attributes are powerful infrastructure tools, but hidden runtime behavior must be bounded, observable, and safe for deployment/trimming/security constraints.
