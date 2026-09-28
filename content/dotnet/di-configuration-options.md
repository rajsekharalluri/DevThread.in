---
id: dotnet-di-configuration-options
slug: di-configuration-options
title: .NET Dependency Injection, Configuration, and Options
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: ".NET 8+"
prerequisites: [dotnet-aspnet-core-backend, csharp-interfaces]
tags: [dotnet, di, configuration, options, dependency-injection]
relatedTopics: [dotnet-logging-diagnostics, dotnet-runtime-services]
order: 60
status: published
---
# .NET Dependency Injection, Configuration, and Options

## Introduction
.NET applications normally build an object graph at startup. Dependency Injection (DI) creates objects and supplies their dependencies; configuration combines sources such as JSON, environment variables, command-line values, and secret providers; the Options pattern binds that configuration into typed objects.

```text
Configuration sources
JSON → environment → command line → secret provider
                 typed Options object
Composition root → DI container → application services
```

## Purpose
These features keep implementation choices out of business code and allow the same compiled application to run in development, test, staging, and production with different safe settings.

## Service lifetimes
```csharp
services.AddSingleton<IClock, SystemClock>();       // one per process
services.AddScoped<IOrderService, OrderService>();  // one per scope/request
services.AddTransient<IEmailFormatter, EmailFormatter>(); // new per resolution
```

A singleton must be thread-safe and must not capture a scoped service. A scoped `DbContext` should not be stored in a singleton. A transient service is not automatically cheap if its object graph is expensive.

## Configuration and Options
```csharp
public sealed class PaymentOptions
{
    public required string BaseUrl { get; init; }
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);
}

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payment")
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _), "Payment:BaseUrl must be a URI")
    .ValidateOnStart();
```

```json
{
  "Payment": {
    "BaseUrl": "https://payments.internal",
    "Timeout": "00:00:05"
  }
}
```

Use `IOptions<T>` for stable startup values, `IOptionsSnapshot<T>` for scoped refresh behavior, and `IOptionsMonitor<T>` when long-lived services need to react to reloadable configuration. Never put production secrets in committed JSON.

## Professional production example
```csharp
builder.Services.AddHttpClient<IPaymentClient, PaymentClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<PaymentOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = options.Timeout;
});
```

A production composition root should register interfaces at the boundary, validate options before accepting traffic, use managed identity/secret-manager integration for credentials, and make service lifetime choices explicit. Tests should replace external clients through DI rather than requiring real network access.

## Common mistakes and fixes
```text
Mistake: singleton captures DbContext
Result: stale state, memory retention, concurrent access bugs
Fix: create a scope per operation or inject IDbContextFactory<T>

Mistake: configuration value assumed to exist
Result: failure deep inside a request
Fix: bind and ValidateOnStart()

Mistake: secret in appsettings.json
Result: source-control leak
Fix: environment/secret manager + least privilege
```

## Comparison
| Choice | Use when | Watch for |
|---|---|---|
| `IOptions<T>` | Stable settings | Does not automatically refresh |
| `IOptionsSnapshot<T>` | Scoped request reads | Scoped lifetime only |
| `IOptionsMonitor<T>` | Long-lived service/reload | Thread-safe change handling |
| Singleton | Shared stateless/thread-safe component | Capturing scoped dependencies |
| Scoped | Request/unit-of-work state | Not available outside a scope |
| Transient | Lightweight short-lived service | Expensive object graphs |

## Interview Questions
- **[L1]** Explain Singleton, Scoped, and Transient lifetimes.
- **[L1]** What is the Options pattern?
- **[L2]** Why can a singleton not safely hold a scoped `DbContext`?
- **[L2]** What is the difference between `IOptions`, `IOptionsSnapshot`, and `IOptionsMonitor`?
- **[L3]** How would you design configuration and secrets for dev, staging, and production?
- **[L3]** When is an interface or DI abstraction unnecessary?

## Interview Answers
1. Singleton is one instance per process, Scoped is normally one instance per request/scope, and Transient is created for each resolution.
2. Options bind configuration into a typed object so callers avoid string keys and can validate required values at startup.
3. `DbContext` is scoped and not thread-safe. A singleton would retain it beyond its intended scope and allow concurrent requests to use the same context.
4. `IOptions` reads a stable value, `IOptionsSnapshot` provides scoped snapshots, and `IOptionsMonitor` supports observing current values in long-lived services.
5. Keep non-secret defaults in source, override by environment, store secrets in a managed secret provider, validate on startup, and give each environment/deployment identity only required access.
6. Keep concrete types when there is no substitution, volatility, or testing seam. Add an abstraction when it protects a real boundary or policy.

## Expert perspective
DI and configuration are not just framework plumbing. They define object lifetimes, security boundaries, startup failure behavior, and how safely the same artifact moves across environments. Treat the composition root as production architecture.
