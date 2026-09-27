---
id: architecture-modular-monolith
slug: modular-monolith
title: Modular Monolith Architecture
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 50
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-fundamentals, architecture-clean]
tags: [modular-monolith, architecture, boundaries, migration]
relatedTopics: [architecture-microservices, architecture-cqrs]
order: 170
status: published
---
# Modular Monolith Architecture

## Introduction
A modular monolith is one deployable process organized into strongly bounded modules. Modules share deployment and often infrastructure, but their code, data ownership, and contracts are intentionally separated.

```text
One deployment/process
 ├── Orders module ── owns order rules/data
 ├── Payments module ── owns payment rules/data
 └── Notifications module ── owns delivery rules/data
        explicit contracts, no internal shortcuts
```

## Purpose
It provides many benefits of good service boundaries—ownership, cohesion, testability, and controlled dependencies—without paying network latency, distributed transactions, deployment multiplication, and operational overhead before those costs are justified.

## Simple example
```text
Orders module
  public: SubmitOrderHandler, OrderSubmitted contract
  internal: Order aggregate, repository implementation

Payments module
  public: AuthorizePayment command/contract
  internal: Provider adapters
```

A module should not reach into another module's tables/classes “because they are in the same process.” Use a public application contract or event.

## Professional company-level example
A modular monolith can later extract a module:

```text
Modular monolith
      ↓ identify stable ownership/scale/compliance boundary
Extract Payments adapter/service
      ↓ keep Orders contract stable
Orders module ↔ Payments API/events
```

The architecture makes extraction possible without requiring the team to operate ten services on day one.

## Common failure scenarios
- Modules are only folders; every module accesses every database table.
- Shared “common” project contains business models from all domains.
- Internal classes become de facto public APIs.
- One transaction spans unrelated modules forever.
- Teams call it modular but cannot identify data ownership.

## Comparison
| Modular monolith | Microservices |
|---|---|
| One deployable | Independent deployables |
| Local calls | Network calls/failure |
| Simpler transaction/ops | Independent scaling/ownership |
| Lower operational cost | Higher platform cost |
| Boundaries can be tested early | Strong process isolation |

## Interview Questions
- **[L1]** What is a modular monolith?
- **[L1]** How is it different from an unstructured monolith?
- **[L2]** How do modules communicate inside one process?
- **[L2]** What signs show a modular monolith is actually tightly coupled?
- **[L3]** Why can it be a better starting point than microservices?
- **[L3]** How would you extract one module later?

## Interview Answers
1. One deployable application with explicit internal modules, ownership, contracts, and dependency rules.
2. An unstructured monolith allows global access; a modular monolith restricts internals and makes module boundaries enforceable/testable.
3. Use public application contracts/events; do not access another module's internal classes/tables directly.
4. Shared table writes, circular dependencies, imports of internals, coordinated changes, and unclear owners indicate coupling.
5. It allows teams to learn domain boundaries and deploy simply while avoiding premature network/distributed-system complexity.
6. Identify stable ownership/contract, extract data access, deploy the module independently, introduce API/events, migrate data/traffic gradually, and retain rollback.

## Expert perspective
A modular monolith is not a “less advanced” architecture. It is often the most responsible architecture when domain boundaries and independent scaling have not been proven. Good modules create optionality without forcing operational complexity early.
