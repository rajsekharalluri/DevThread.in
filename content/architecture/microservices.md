---
id: architecture-microservices
slug: microservices
title: "Microservices: Boundaries, Data Ownership, and Operations"
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 60
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-fundamentals, architecture-distributed-systems]
tags: [architecture, microservices, bounded-context, modular-monolith, ownership]
relatedTopics: [architecture-event-driven, architecture-kafka, architecture-resilience]
order: 70
status: published
---
# Microservices: Boundaries, Data Ownership, and Operations

## Introduction
A microservice is an independently deployable process organized around a business capability, with clear ownership of code, data, contracts, and operations. Microservices are an organizational and operational strategy, not merely splitting a codebase into folders.

```text
Orders team → Orders service → Orders data
Payments team → Payments service → Payments data
                         │
                    events/APIs
```

## Purpose
Microservices can provide independent deployment, scaling, ownership, compliance boundaries, and failure isolation when a team and domain are large enough to justify distributed-system cost.

## Before splitting
Ask:

```text
Do boundaries change independently?
Do workloads scale differently?
Do teams need independent deployment?
Is data ownership clear?
Can the organization operate many services?
Are network failure/observability/tooling ready?
```

If the answers are mostly no, a modular monolith is usually safer.

## Simple example
```text
Modular monolith
  ├── Orders module
  ├── Payments module
  └── Notifications module
      one process, explicit code/data boundaries
```

This gives a small team modularity without adding network calls, distributed transactions, multiple pipelines, and many on-call surfaces.

## Professional company-level example
An order/payment workflow should not pretend one ACID transaction crosses databases:

```text
Order service
  └── commits Order + Outbox(OrderPlaced)
       Broker
          ├── Inventory service → InventoryReserved/Rejected
          └── Payment service   → PaymentCaptured/Failed
                              Order process manager
```

Each service owns its state. A saga/process manager coordinates the business workflow and models compensation/failure rather than hiding it.

## Distributed-monolith warning

```text
Service A ── synchronous call ── Service B
    │                           │
    └── shared database tables ──┘

Result: network failure + shared-schema coupling + coordinated releases
```

A system is not independent merely because it has multiple deployables. Shared database writes and long synchronous call chains often create the worst of both worlds.

## Operations
Each service needs:

- CI/CD and backward-compatible releases.
- Authentication/authorization.
- Timeouts/retries/circuit breakers.
- Structured logs/metrics/traces.
- Health/readiness behavior.
- Contract/integration tests.
- On-call owner and runbook.
- Data backup/reconciliation plan.

## Comparison
| Modular monolith | Microservices |
|---|---|
| One deployment/process | Independent deployments |
| Local calls/transactions | Network calls/partial failure |
| Simpler operations | More platform/on-call cost |
| Shared runtime/data possible | Explicit ownership required |
| Good while boundaries evolve | Good when autonomy/scale is proven |

## Interview Questions
- **[L1]** What makes a microservice boundary healthy?
- **[L1]** Why is a modular monolith often a good starting point?
- **[L2]** What is a distributed monolith?
- **[L2]** How do services coordinate workflows across databases?
- **[L3]** When should an organization not adopt microservices?
- **[L3]** How do you decide whether a monolith should be split?

## Interview Answers
1. Cohesive business rules, clear data ownership, an explicit contract, independent change/deployment needs, and an owning team.
2. It provides module boundaries without network/operational/distributed-transaction complexity while the domain is still being learned.
3. A multi-deployable system still tightly coupled through shared tables, synchronous chains, and coordinated releases.
4. APIs/events, outbox, sagas/process managers, and eventual consistency; not one local transaction across independent databases.
5. Do not adopt them for small teams, uncertain boundaries, simple workloads, or when operations/platform/on-call maturity is missing.
6. Use evidence: independent scaling/deployment, ownership conflict, reliability isolation, compliance, or distinct change rates. Extract one boundary with a clear contract and rollback plan rather than splitting everything at once.

## Expert perspective
Microservices buy autonomy by taking on distributed-systems and organizational cost. Senior engineers can explain who owns each service, how it fails, how data is repaired, and why the same result could not be achieved more simply with a modular monolith.
