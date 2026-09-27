---
id: dotnet-memory-performance
slug: memory-performance
title: .NET Memory, Garbage Collection, and Performance
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: ".NET 8+"
prerequisites: [dotnet-runtime-internals, csharp-concurrency-tasks]
tags: [dotnet, gc, memory, loh, performance, profiling]
relatedTopics: [csharp-types-nullability-patterns, dotnet-runtime-services]
order: 50
status: published
---
# .NET Memory, Garbage Collection, and Performance

## Introduction / Definition
.NET manages most object memory with a generational garbage collector. Short-lived objects are collected frequently in Gen 0; survivors move to older generations. Large objects are handled on the Large Object Heap (LOH), where allocation/collection behavior differs.

## Purpose
Most application performance problems are resource problems: allocation rate, GC pauses, thread-pool starvation, database calls, or excessive serialization. Understanding GC helps you measure and fix the real cause without premature unsafe optimization.

## Real-World Simple Example
```csharp
// Avoid allocating an intermediate list when only a sum is needed.
var total = orders.Sum(order => order.Total);

// Be careful in hot paths: repeated interpolation creates strings.
var message = $"Order {order.Id} total {order.Total}";
```
The correct optimization depends on allocation frequency and workload, not on whether a single line looks expensive.

## Professional Company-Level Example
A high-throughput API measures allocation rate, Gen 0/1/2 collections, LOH size, pause time, CPU, and p95/p99 latency. It uses projections, pooling only where measured, bounded buffers, and avoids retaining large object graphs in caches.

```bash
dotnet-counters monitor --process-id <pid> System.Runtime
dotnet-trace collect --process-id <pid> --duration 00:00:30
```

## Advantages and Disadvantages
GC prevents many manual memory errors and supports productive application development. It does not make allocation free: high allocation rate, long-lived references, large objects, and unbounded caches still cause memory pressure and latency.

## Comparison
| Technique | Benefit | Risk |
|---|---|---|
| Normal allocation | Simple and safe | Can create pressure in hot paths |
| `ArrayPool<T>` | Reuses buffers | Must return/clear buffers correctly |
| Cache | Faster repeated reads | Retention, stale data, memory growth |
| `struct` | Can avoid object allocation | Copies and boxing can become expensive |
| `Span<T>` | Efficient memory views | Short lifetime; cannot cross async boundaries normally |

## Interview Questions
- **[L1]** Why does .NET use generational garbage collection?
- **[L1]** What is the Large Object Heap?
- **[L2]** How can an application have a memory leak with a garbage collector?
- **[L2]** What metrics help diagnose GC-related latency?
- **[L3]** How would you fix high allocation rate in a production API?
- **[L3]** When is object pooling worth its complexity?

## Interview Answers
1. Most objects die young, so collecting Gen 0 frequently is cheaper than scanning the entire heap every time. Survivors move to older generations collected less often.
2. LOH stores large allocations and has different collection/compaction behavior. Large buffers and retained objects can create memory pressure and fragmentation.
3. A GC only collects unreachable objects. Static references, unbounded caches, event subscriptions, timers, and long-lived collections can retain objects indefinitely.
4. Watch allocation rate, heap size, Gen 0/1/2 collection frequency, pause time, LOH size, CPU, working set, and latency percentiles.
5. Profile allocation hot paths, reduce unnecessary materialization/strings, project data, stream payloads, bound caches, use pooling only where measured, and verify under production-like load.
6. Pooling helps when repeated large buffers cause measurable allocation/GC cost. It adds lifecycle and data-clearing risks, so the measured benefit must exceed the complexity.
