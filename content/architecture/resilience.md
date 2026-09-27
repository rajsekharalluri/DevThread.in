---
id: architecture-resilience
slug: resilience
title: "Resilience: Timeouts, Retries, Circuit Breakers, and Bulkheads"
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Concept-level"
prerequisites: [architecture-distributed-systems, architecture-messaging-patterns]
tags: [architecture, resilience, timeout, retry, circuit-breaker, bulkhead]
relatedTopics: [architecture-microservices, devops-observability]
order: 90
status: published
---
# Resilience: Timeouts, Retries, Circuit Breakers, and Bulkheads

## Introduction
Resilience controls how a system behaves when dependencies are slow, unavailable, overloaded, or returning errors. The goal is not to hide failures; it is to bound their impact and recover deliberately.

```text
Caller
  ↓ timeout budget
Retry? → only transient/idempotent failures
  ↓ repeated failure
Circuit opens → fail fast
  ↓ separate resources
Bulkhead → one dependency cannot starve others
  ↓
Fallback/degraded response or explicit failure
```

## Purpose
A failing downstream service can otherwise consume all request threads, connection pools, queues, and retry capacity, causing a healthy service to fail too. Resilience patterns protect capacity and communicate degraded behavior.

## Simple example
```csharp
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
timeout.CancelAfter(TimeSpan.FromSeconds(2));

var result = await catalogClient.GetAsync(productId, timeout.Token);
```

The timeout is part of the request budget. It does not prove that the remote operation stopped; the remote operation may still complete after the caller stopped waiting.

## Production policy

```text
Overall request budget: 2 seconds
Catalog attempt: 500 ms
Retry attempts: 2 with exponential backoff + jitter
Bulkhead: 50 concurrent catalog calls
Circuit: open after measured failure threshold
Fallback: cache or explicit degraded response
Metrics: timeout/retry/circuit/fallback counts
```

Do not retry payments blindly. Use idempotency keys and provider reconciliation because a timeout does not prove the charge failed.

## Pattern behavior

- **Timeout:** stop waiting within a defined limit.
- **Retry:** repeat a transient operation with bounded backoff.
- **Circuit breaker:** stop calling a failing dependency temporarily.
- **Bulkhead:** isolate concurrency/connection pools by dependency.
- **Fallback:** return a safe cached/degraded result when business rules allow it.

Retry policy order matters. An inner per-attempt timeout plus an outer total timeout prevents multiple retries from exceeding the request's SLA.

## Comparison
| Pattern | Protects against | Main risk |
|---|---|---|
| Timeout | Hanging/slow dependency | Remote work may continue |
| Retry | Transient failure | Retry storm/duplicate effect |
| Circuit breaker | Repeated failure | Incorrect threshold/fallback |
| Bulkhead | Resource starvation | Rejected work/capacity tuning |
| Fallback | User-visible outage | Stale/wrong data |

## Interview Questions
- **[L1]** Why does every network call need a timeout?
- **[L1]** What does a circuit breaker do?
- **[L2]** Why can retries make an outage worse?
- **[L2]** What is a bulkhead?
- **[L3]** How would you design resilience for a payment call?
- **[L3]** How do you choose a timeout budget across a service chain?

## Interview Answers
1. Without one, a connection can wait indefinitely and consume resources until the caller or infrastructure fails.
2. It temporarily rejects calls after repeated failures so the dependency can recover and the caller fails quickly.
3. Retries multiply load against a struggling dependency and can duplicate non-idempotent effects. Use bounded backoff/jitter and retry only safe transient operations.
4. It limits resources/concurrency for one dependency or workload so it cannot starve unrelated requests.
5. Use an idempotency key, short bounded attempts, explicit timeout, provider status reconciliation, durable pending state, and safe retry/recovery.
6. Start from the end-to-end SLA, reserve time for application work, divide remaining time among dependencies, ensure nested retries fit the budget, and propagate cancellation.

## Expert perspective
Resilience is controlled failure. Senior engineers define retryability from business semantics, cap resource usage, instrument every policy transition, and make degraded/fallback behavior visible to operators and users.
