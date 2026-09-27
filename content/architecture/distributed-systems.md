---
id: architecture-distributed-systems
slug: distributed-systems
title: Distributed Systems Fundamentals
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Concept-level"
prerequisites: [architecture-event-driven, architecture-fundamentals]
tags: [distributed-systems, consistency, availability, failure]
relatedTopics: [architecture-kafka, architecture-system-design]
order: 50
status: published
---
# Distributed Systems Fundamentals

## Overview
A distributed system has multiple independent processes communicating over a network to provide one logical capability. Its defining challenge is partial failure: one component can fail or become slow while others continue running.

## Why This Exists
Distributed deployment can improve scale, isolation, and team ownership, but a network call is not a local method call. Messages can be delayed, duplicated, reordered, or lost; clocks disagree; nodes fail independently. Architecture must account for these facts explicitly.

## Fundamentals
Key concerns include consistency, availability, partition tolerance, latency, timeouts, retries, idempotency, leader election, replication, and observability. CAP is about choosing consistency or availability during a network partition for a replicated data system; it does not say a system can only have two of the three qualities in normal operation.

## Syntax and API
```csharp
public async Task<Response> CallWithBudgetAsync(CancellationToken requestToken)
{
    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(requestToken);
    timeout.CancelAfter(TimeSpan.FromSeconds(2));

    try
    {
        return await client.GetAsync(timeout.Token);
    }
    catch (OperationCanceledException) when (!requestToken.IsCancellationRequested)
    {
        throw new DownstreamTimeoutException();
    }
}
```
A timeout is a correctness and capacity boundary, not just a performance setting.

## How It Works
A distributed request crosses failure boundaries. A timeout tells the caller to stop waiting, but it does not prove the remote operation did not complete. Retrying a non-idempotent operation can create duplicates, so request IDs/idempotency keys and durable state are required for safe retries.

## Internal Implementation
Replication improves availability but creates consistency choices. Quorum reads/writes, leader-based replication, version vectors, and consensus algorithms solve different coordination problems at different costs. No algorithm removes network latency or independent failure; it only makes guarantees explicit.

## Real-World Example
A payment request times out after the provider may have charged the card. The system must query/reconcile by idempotency key rather than blindly charging again.

## Production Example
```text
Request API -> Payment provider (timeout 2s)
     │
     ├── timeout: mark PaymentPending, store idempotency key
     ├── background reconciliation queries provider status
     └── publish PaymentCaptured or PaymentFailed after authoritative result
```
This avoids treating timeout as proof of failure.

## Common Mistakes
- Retrying every error with no backoff or limit.
- Assuming timeout means the remote action did not happen.
- Sharing distributed state without defining consistency.
- Using wall-clock timestamps as a globally ordered truth.
- No correlation IDs across service boundaries.

## Performance
Latency is bounded by the slowest dependency in a synchronous chain. Use budgets, parallelize independent calls carefully, cache appropriate data, and monitor queue depth and tail latency (p95/p99), not only averages.

## Security
Authenticate every service boundary, authorize each operation, encrypt network traffic, and avoid trusting network location as identity. Failure handling must not bypass authorization or duplicate sensitive actions.

## Testing
Inject latency, dropped messages, duplicate messages, process crashes, clock skew, partial dependency failure, and network partitions in integration/chaos tests.

## When to Use
Distributed architecture is justified by scale, availability, independent ownership, geographic needs, or distinct workload profiles — after the team can operate it.

## When Not to Use
Avoid distribution merely to appear scalable or because microservices are fashionable; a modular monolith is often safer while boundaries are uncertain.

## Trade-offs
Distribution enables independent scaling and failure isolation but introduces network failure, eventual consistency, operational tooling, deployment coordination, and harder debugging.

## Related Topics
See [Event-Driven Architecture](/architecture/event-driven) and [System Design](/architecture/system-design).

## Practical Exercise
Design a payment flow that handles timeout, duplicate request, provider retry, and reconciliation without double-charging.

## Interview Questions
- **[L1]** Why is a network call different from a local method call?
- **[L1]** What does idempotency mean in a distributed system?
- **[L2]** What does CAP describe, and what does it not describe?
- **[L2]** Why can retries make an outage worse?
- **[L3]** How would you design a reliable operation when a timeout does not reveal whether the remote side completed?

## Interview Answers
1. **[L1]** A network call has variable latency and can fail, time out, duplicate, or complete after the caller stops waiting. A local method normally shares process memory and failure behavior, so treating them identically creates bugs.
2. **[L1]** An operation is idempotent if repeating it produces the same final effect as performing it once. It is essential when retries or duplicate messages are possible.
3. **[L2]** CAP describes a replicated data system during a network partition: it must choose between serving potentially stale data (availability) and refusing/limiting operations to preserve a consistency guarantee. It is not a general statement that only two qualities are possible at all times.
4. **[L2]** Retries multiply traffic against an already struggling dependency, causing retry storms and cascading failure. Use bounded retries, exponential backoff, jitter, and circuit breaking.
5. **[L3]** Assign an idempotency key, persist the request as pending, apply a timeout, and reconcile with the authoritative remote system before retrying a side effect. Model uncertain outcomes explicitly instead of treating timeout as failure.

## Senior Developer Perspective
Distributed systems reward humility about uncertainty. Senior engineers design around partial failure, make uncertain outcomes explicit, and ensure every retryable side effect has an idempotency and reconciliation story.
