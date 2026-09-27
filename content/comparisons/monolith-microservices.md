---
id: comparisons-monolith-microservices
slug: monolith-microservices
title: Modular Monolith vs Microservices
category: comparisons
categoryTitle: Decision Guides
difficulty: senior
estimatedMinutes: 35
version:
  minimum: "Architecture pattern"
prerequisites: [architecture-modular-monolith, architecture-microservices]
tags: [architecture, monolith, microservices, comparison]
relatedTopics: [architecture-cqrs, architecture-event-driven]
order: 30
status: published
---
# Modular Monolith vs Microservices

## Decision table

| Concern | Modular monolith | Microservices |
|---|---|---|
| Deployment | One | Independent |
| Calls | In-process | Network |
| Transactions | Local/simple | Eventual/saga |
| Operations | Simpler | Platform/on-call cost |
| Scaling | Whole app or modules | Per-service |
| Boundaries | Enforced in code | Process/data ownership |
| Best starting point | Uncertain domain/small team | Proven autonomy/scale need |

## Example

```text
Small team:
One process → Orders module → Payments module → Notifications module

Larger proven boundary:
Orders service ↔ Payment service ↔ Notification service
                 APIs/events/retries/timeouts
```

The second design adds failure modes and operational work. Choose it only when independent ownership, scaling, compliance, or failure isolation repays that cost.

## Interview Questions
- **[L1]** What is a modular monolith?
- **[L1]** What is the main cost of microservices?
- **[L2]** What is a distributed monolith?
- **[L2]** How does data consistency change?
- **[L3]** What evidence justifies extracting a service?
- **[L3]** Why is a modular monolith often the safer first architecture?

## Interview Answers
1. One deployable with explicit business modules, ownership, contracts, and dependency rules.
2. Distributed-system operations: network failures, deployment, observability, data consistency, security, and on-call overhead.
3. Multiple deployables that remain tightly coupled through shared schemas, synchronous chains, or coordinated releases.
4. One local transaction becomes APIs/events/sagas/reconciliation with eventual consistency.
5. Independent change/scale/compliance/failure ownership measured in the current system and supported by an operating team.
6. It preserves simple deployment/local transactions while allowing boundaries to be learned and enforced before adding network cost.
