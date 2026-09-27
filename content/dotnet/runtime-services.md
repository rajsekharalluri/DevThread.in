---
id: dotnet-runtime-services
slug: runtime-services
title: .NET Runtime, Configuration, Logging, and Background Services
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 70
version:
  minimum: ".NET 8+"
prerequisites: [csharp-fundamentals, csharp-concurrency-tasks]
tags: [dotnet, clr, gc, configuration, logging, background-services, hosting]
relatedTopics: [dotnet-runtime-internals, dotnet-di-configuration-options, dotnet-logging-diagnostics]
order: 30
status: published
---
# .NET Runtime, Configuration, Logging, and Background Services

## Introduction
A .NET application is a process hosted by the Generic Host. The host builds configuration, logging, dependency injection, and lifetime management, then starts the web server and registered hosted services.

```text
Operating system
      │
      ▼
.NET Generic Host
      ├── Configuration
      ├── Dependency Injection container
      ├── Logging
      ├── Kestrel/web server
      ├── Hosted/background services
      └── Graceful shutdown
```

The CLR/JIT/GC execute managed code underneath this host. The host controls how the application is composed and operated; the runtime controls how managed code executes.

## Purpose
These services answer practical production questions:

- Where does configuration come from in each environment?
- How are dependencies created and disposed?
- How are background jobs started and stopped?
- How do operators understand failures?
- What happens when deployment sends a shutdown signal?
- How do you prevent a worker from exhausting database connections?

## Starting a .NET application

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOptions<CleanupOptions>()
    .BindConfiguration("Cleanup")
    .ValidateOnStart();
builder.Services.AddHostedService<CleanupWorker>();

var app = builder.Build();
app.MapControllers();
app.Run();
```

The important order is:

```text
Create builder
   ↓
Load configuration and logging defaults
   ↓
Register services
   ↓
Build host and DI container
   ↓
Start hosted services/web server
   ↓
Accept requests
   ↓
Receive shutdown signal
   ↓
Cancel services and dispose resources
```

## Configuration

Configuration is layered. A later provider can override an earlier value:

```text
appsettings.json
        ↓
appsettings.Production.json
        ↓
environment variables
        ↓
command-line arguments
        ↓
secret provider
```

For a nested setting:

```json
{
  "Payment": {
    "BaseUrl": "https://payments.internal",
    "TimeoutSeconds": 5
  }
}
```

The environment-variable form is:

```text
Payment__BaseUrl=https://payments.internal
Payment__TimeoutSeconds=5
```

Use typed Options instead of scattering string lookups throughout the code:

```csharp
public sealed class PaymentOptions
{
    public required string BaseUrl { get; init; }
    public int TimeoutSeconds { get; init; } = 5;
}

builder.Services.AddOptions<PaymentOptions>()
    .BindConfiguration("Payment")
    .Validate(o => Uri.TryCreate(o.BaseUrl, UriKind.Absolute, out _),
        "Payment:BaseUrl must be an absolute URI")
    .Validate(o => o.TimeoutSeconds is > 0 and <= 60,
        "Payment timeout must be between 1 and 60 seconds")
    .ValidateOnStart();
```

`ValidateOnStart()` is important in production. Without it, a missing value may not fail until a real request reaches the broken code path.

## Logging

Use structured logging:

```csharp
logger.LogInformation(
    "Payment {PaymentId} authorized for order {OrderId} in {ElapsedMs}ms",
    paymentId,
    orderId,
    stopwatch.ElapsedMilliseconds);
```

Do not do this:

```csharp
logger.LogInformation($"Payment {paymentId} authorized for order {orderId}");
```

Named properties can be indexed by a log platform. Interpolated text is harder to query reliably.

Log levels should have a purpose:

```text
Trace/Debug → local troubleshooting, usually disabled in production
Information → important lifecycle/business events
Warning     → unexpected but recoverable conditions
Error       → operation failed and needs attention
Critical    → process/system cannot continue
```

Never log:

- Passwords
- Access tokens
- Connection strings
- Full payment-card data
- Sensitive request bodies
- Secret configuration values

## BackgroundService

Use `BackgroundService` for work tied to the application host lifecycle:

```csharp
public sealed class CleanupWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<CleanupOptions> options,
    ILogger<CleanupWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(options.Value.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<ICleanupService>();
                await cleanup.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Cleanup iteration failed");
            }
        }
    }
}
```

Why each part matters:

- `PeriodicTimer` avoids manually managing delay loops.
- `stoppingToken` allows graceful shutdown.
- A scope is created per iteration because hosted services are singleton-like and scoped services such as `DbContext` cannot be injected directly and retained forever.
- Cancellation is not logged as an application failure during normal shutdown.
- An exception in one iteration does not necessarily terminate the worker permanently.

## Background work architecture

```text
HTTP request
    │
    ├── Validate command
    ├── Save durable work/outbox record
    └── Return accepted response
             │
             ▼
       Background worker
             │
             ├── Read work
             ├── Process with cancellation/timeout
             ├── Retry transient failures
             └── Record success/dead letter
```

Do not start important work like this inside a controller:

```csharp
_ = SendEmailAsync(order); // fire-and-forget: scope may be disposed, exceptions may vanish
```

Use a durable queue, outbox, or hosted worker. In-process background work can disappear if the process restarts; important work needs persistence and retry semantics.

## Graceful shutdown

When a deployment or instance stop sends a shutdown signal:

```text
Shutdown signal
      ↓
Host cancels stoppingToken
      ↓
New work stops being accepted
      ↓
Current work gets a grace period
      ↓
Connections/resources are disposed
      ↓
Process exits
```

Workers should stop accepting new work, finish safe in-flight work when possible, and persist unfinished work so it can be retried after restart.

## Performance and runtime behavior

A managed runtime does not make allocations free. Monitor:

- Allocation rate
- GC collection frequency
- Gen 0/1/2 pauses
- LOH size
- Thread-pool queue length
- CPU and working set
- Request p95/p99 latency
- Background queue age

Do not tune GC or create manual threads before measuring. Most application performance problems are caused by blocking I/O, excessive database calls, unbounded queues, large payloads, or inefficient allocations rather than a missing runtime switch.

## Comparison

| Requirement | Good default |
|---|---|
| Request database/HTTP work | Async API + `await` |
| Repeating lightweight host work | `BackgroundService` + `PeriodicTimer` |
| Durable important work | Queue/outbox + worker |
| Configuration | Typed Options + validation |
| Operational diagnosis | Structured logs + metrics + traces |
| Scoped dependency in worker | Create scope per iteration/message |
| Secret configuration | Secret manager/environment injection |

## Interview Questions
- **[L1]** What does the .NET Generic Host provide?
- **[L1]** Why should background services use a cancellation token?
- **[L2]** Why must a `BackgroundService` create a scope before resolving `DbContext`?
- **[L2]** Why is fire-and-forget work dangerous inside an ASP.NET Core request?
- **[L3]** How would you design a reliable background processing system for an API?
- **[L3]** What runtime and application metrics would you monitor during a production incident?

## Interview Answers
1. The Generic Host composes configuration, logging, DI, hosted services, application lifetime, and the web server into one managed process.
2. Cancellation tells the worker to stop accepting new work and exit gracefully during deployment or shutdown instead of running forever or being killed abruptly.
3. A hosted service is long-lived, while `DbContext` is scoped and not thread-safe. Create a scope for each iteration/message and dispose it after the operation.
4. The request may finish and dispose scoped services while the task is still running; exceptions can be lost and the process may shut down before the work completes.
5. Persist work in an outbox/queue, process it in a worker with bounded concurrency, cancellation, timeouts, retries, idempotency, dead-letter handling, and metrics for backlog and age.
6. Check error rate, p95/p99 latency, CPU, memory/GC, allocation rate, thread-pool queue, database latency/pool usage, outgoing HTTP latency, queue depth/age, retry volume, and deployment changes.

## Expert perspective
The .NET host is an operational system, not just a startup helper. Correct lifetimes, configuration validation, cancellation, structured diagnostics, and durable background processing determine whether a service survives deployment, dependency failure, and production load.
