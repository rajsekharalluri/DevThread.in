---
id: csharp-async-await
slug: async-await
title: C# Async/Await and Asynchronous I/O
category: csharp
categoryTitle: C#
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "C# 12 / .NET 8+"
prerequisites: [csharp-concurrency-tasks, csharp-fundamentals]
tags: [csharp, async, await, io, scalability, cancellation]
relatedTopics: [csharp-concurrency-tasks, dotnet-aspnet-core-backend]
order: 60
status: published
---
# C# Async/Await and Asynchronous I/O

## Introduction
`async`/`await` is a way to compose operations that may complete later. For I/O, it allows the current thread to return to the pool while the database/network/file operation is pending.

```text
Request thread
    │ starts database I/O
    ├──────────────→ database works
    │ thread returns to pool
    │
    └──────────────→ response arrives
                     continuation resumes
```

Async is not automatically parallelism and does not automatically create a new thread.

## Purpose
Asynchronous I/O improves server throughput because a limited thread pool is not occupied doing nothing while external systems respond. It also gives cancellation and timeout behavior a natural path through the call chain.

## Simple example
```csharp
public async Task<Order?> GetAsync(Guid id, CancellationToken ct)
{
    return await db.Orders
        .AsNoTracking()
        .SingleOrDefaultAsync(order => order.Id == id, ct);
}
```

The database remains the slow operation. Async does not reduce the database's 200 ms time; it prevents a worker thread from blocking during those 200 ms.

## Sequential versus concurrent I/O

```csharp
// Sequential: total time approximately T1 + T2 + T3
var user = await GetUserAsync(ct);
var orders = await GetOrdersAsync(ct);
var alerts = await GetAlertsAsync(ct);
```

```csharp
// Concurrent when independent: total time approximately max(T1, T2, T3)
var userTask = GetUserAsync(ct);
var ordersTask = GetOrdersAsync(ct);
var alertsTask = GetAlertsAsync(ct);

await Task.WhenAll(userTask, ordersTask, alertsTask);
```

Only do this when the operations are independent and the database/API capacity can handle the concurrency.

## Professional company-level example
```csharp
public async Task<Dashboard> GetDashboardAsync(
    Guid customerId,
    CancellationToken requestCancellation)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestCancellation);
    timeout.CancelAfter(TimeSpan.FromSeconds(2));

    var userTask = users.GetAsync(customerId, timeout.Token);
    var ordersTask = orders.GetRecentAsync(customerId, timeout.Token);
    var notificationsTask = notifications.GetAsync(customerId, timeout.Token);

    await Task.WhenAll(userTask, ordersTask, notificationsTask);

    return new Dashboard(
        await userTask,
        await ordersTask,
        await notificationsTask);
}
```

The request cancellation and service-level timeout share one budget. A client disconnect or exceeded deadline stops downstream work when supported.

## `async void`, exceptions, and blocking

```csharp
// Bad in application code: caller cannot await or reliably catch the exception.
async void ProcessAsync() { }

// Correct:
async Task ProcessAsync() { }
```

Avoid:

```csharp
var result = GetAsync(ct).Result;
GetAsync(ct).Wait();
```

Blocking consumes a thread while waiting, can create starvation, and in some synchronization-context environments can deadlock. Async should flow through the call chain.

## CPU-bound versus I/O-bound

| Work | Normal approach |
|---|---|
| Database/HTTP/file I/O | Async API + `await` |
| CPU-heavy compression | Parallel/worker capacity |
| Image processing | CPU worker or parallelism |
| Many external calls | Async + bounded concurrency |
| Long durable workflow | Queue + background worker |

`Task.Run` is not a universal async wrapper. In ASP.NET Core, wrapping database I/O in `Task.Run` wastes a thread; use the database provider's async API.

## Interview Questions
- **[L1]** What does `await` do to a thread during asynchronous I/O?
- **[L1]** Is async the same as creating a new thread?
- **[L2]** Why is `.Result` dangerous in server code?
- **[L2]** How do `Task.WhenAll` and sequential awaits differ?
- **[L3]** How do you propagate cancellation and timeout budgets across services?
- **[L3]** How do you decide whether to parallelize independent calls?

## Interview Answers
1. If the task is incomplete, the method suspends and the current thread returns to the pool; a continuation resumes when the I/O completes.
2. No. Async I/O commonly uses no waiting thread; `Task.Run` explicitly schedules synchronous work on a thread-pool thread.
3. It blocks a worker, increases latency/thread-pool pressure, may deadlock in some contexts, and prevents the async call chain from scaling.
4. Sequential awaits wait one operation before starting the next; `WhenAll` starts independent operations together and waits for every result.
5. Link the incoming request token with per-call timeout tokens, pass them through every async API, classify cancellation/timeout separately, and ensure downstream clients honor cancellation.
6. Confirm independence, compare downstream capacity/connection pools, bound concurrency, measure tail latency, and avoid parallelizing operations that share one non-thread-safe context.

## Expert perspective
Async is resource management. Senior engineers design the complete budget: thread usage, timeout, cancellation, downstream capacity, retries, and failure semantics. Adding `await` without that model is syntax, not concurrency engineering.
