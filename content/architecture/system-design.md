---
id: architecture-system-design
slug: system-design
title: System Design Methodology
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 55
version:
  minimum: "Concept-level"
prerequisites: [architecture-fundamentals, architecture-distributed-systems]
tags: [architecture, system-design, scale, reliability, trade-offs]
relatedTopics: [architecture-microservices, architecture-kafka, architecture-resilience]
order: 60
status: published
---
# System Design Methodology

## Introduction
System design is a disciplined way to turn requirements into APIs, data, components, capacity, reliability, security, and operational behavior.

```text
Clarify requirements
Estimate scale/quality attributes
API + data model
Components and communication
Capacity/cache/partitioning
Failure/security/observability
Trade-offs and rollout
```

## Purpose
A diagram without assumptions is decoration. A design becomes credible when every major component connects to a requirement, scale assumption, failure mode, or ownership decision.

## Simple example: URL shortener
Requirements:

```text
POST /links        → create short code
GET /{code}        → redirect quickly
Reads ≫ writes
Codes must be unique
Redirect path needs low latency
```

A reasonable first design:

```text
Client → API → relational store (code → URL)
           cache for popular redirects
```

Do not introduce Kafka/Kubernetes/search infrastructure before the workload needs it.

## Professional company-level design
For a notification platform:

```text
API
 ↓ validate + persist intent
Outbox/queue
Worker pool
 ├── email provider
 ├── SMS provider
 ├── retry/backoff
 └── dead-letter/replay
status/audit store + metrics/traces
```

The API acknowledges durable acceptance rather than waiting for a slow third-party provider. The design must specify retryability, idempotency, provider quotas, status semantics, and operator recovery.

## Scale questions
Estimate:

- Requests/second average and peak.
- Read/write ratio.
- Payload size and storage growth.
- Latency target/p95/p99.
- Availability and recovery objectives.
- Number of tenants/users.
- Cache hit rate.
- Dependency capacity.

Order-of-magnitude estimates are better than choosing technology without constraints.

## Comparison
| Decision | Question |
|---|---|
| Sync vs async | Must the user wait for completion? |
| SQL vs NoSQL | What are relationships/consistency/access patterns? |
| Cache | Is repeated data safe to serve stale? |
| Monolith vs services | Is independent ownership/scaling proven? |
| Queue vs stream | Is work consumed once or replayed by groups? |
| Read model | Is live computation too expensive? |

## Interview Questions
- **[L1]** What should you clarify before designing a system?
- **[L1]** Why are scale assumptions necessary?
- **[L2]** How do you decide between synchronous and asynchronous processing?
- **[L2]** What should a design include beyond boxes and arrows?
- **[L3]** How do you communicate trade-offs in a system-design review?
- **[L3]** How do you know when a simple design should evolve?

## Interview Answers
1. Clarify users, core flows, scale, latency/availability, consistency, security, retention, and explicit out-of-scope items.
2. They reveal whether a database, cache, queue, partitioning, or replication strategy is necessary instead of selecting technology from habit.
3. Use synchronous processing for immediate decisions; use async for slow/retryable/independently scalable work when delayed completion is acceptable.
4. Include data ownership/model, APIs, capacity, failure/retry behavior, security, observability, deployment, cost, and operational ownership.
5. State criteria, compare alternatives, explain what the selected option gives up, identify failure modes, and define evidence that would cause a revisit.
6. Measure real load/latency/failure/ownership pressure. Evolve when a quality attribute or team boundary is measurably harmed, not because the architecture trend changed.

## Expert perspective
System design is structured decision-making under constraints. Senior engineers explain not only how the system works when everything is healthy, but how it behaves when dependencies are slow, data is inconsistent, traffic spikes, deployments overlap, and operators need to recover it.
