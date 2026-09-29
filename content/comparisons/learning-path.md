---
id: comparisons-learning-path
slug: learning-path
title: Decision Guides - How to Make Technology Trade-offs
category: comparisons
categoryTitle: Decision Guides
difficulty: intermediate
estimatedMinutes: 15
version:
  minimum: "Architecture concepts"
prerequisites: []
tags: [comparisons, decision-making, trade-offs, architecture, learning-path]
relatedTopics: [comparisons-monolith-microservices, comparisons-rest-grpc, comparisons-efcore-dapper]
order: 1
status: published
---
# Decision Guides - How to Make Technology Trade-offs

## Introduction

Senior engineers are distinguished less by which technologies they know than by how they **choose** between them. Every decision guide in this section compares options side by side: what each is, when to use it, when not to, code for each option, a scenario walkthrough, a decision matrix, and common mistakes. This page explains how to use them and gives a repeatable framework for technology decisions.

## The Decision Guides

| Area | Guide | Core Question |
|---|---|---|
| Concurrency | Thread vs ThreadPool vs Task vs async/await vs Parallel | Is the work I/O-bound or CPU-bound? |
| Data access | EF Core vs Dapper vs Raw SQL | How much SQL control do you need versus modeling productivity? |
| Architecture | Modular Monolith vs Microservices | Do independent deployment and scaling justify distributed-systems cost? |
| Messaging | Kafka vs RabbitMQ vs Queues | Is this a replayable stream of facts or units of work? |
| Communication | REST vs gRPC vs Events | Does the caller need an immediate answer? |
| Communication | Event-Driven vs Request/Response | Should the caller wait, get a job ID, or not wait at all? |
| Data stores | SQL vs Document vs Graph | What access patterns and consistency does the data need? |
| Data layers | Cache vs Database vs Read Model | Who owns the truth, and what happens if data is lost or stale? |
| GenAI | RAG vs Fine-Tuning vs Prompting | Is the gap knowledge or behavior? |
| Infrastructure | Containers vs VMs | What isolation boundary and OS flexibility do you need? |
| Operations | Liveness vs Readiness vs Startup Probes | Should the platform restart the process or stop sending traffic? |

## A Repeatable Decision Framework

```text
1. Define the problem and constraints: requirements, scale, latency, consistency, compliance, budget, deadline
2. List realistic options, including "change nothing" and "extend what we already have"
3. Identify decision criteria and weight them (performance, cost, team skills, operational load, risk, reversibility)
4. Gather evidence: benchmarks, prototypes, production metrics, references - not opinions or popularity
5. Compare options against criteria in a decision matrix
6. Decide, and record it in an Architecture Decision Record (ADR) with context, options, decision, and consequences
7. Define how you will know it was wrong and when to revisit
```

### Architecture Decision Record Template

```markdown
# ADR-014: Use RabbitMQ for background job dispatch

## Status
Accepted - 2025-03-14

## Context
Order service needs to dispatch ~50k emails/PDF jobs per day with retries and dead-lettering.
No replay requirement. Team has RabbitMQ experience; Kafka is not operated in-house.

## Options considered
1. Kafka (managed) - strong for streams/replay, heavier ops and cost for this need
2. RabbitMQ (quorum queues) - ack/retry/DLX fit, team familiarity
3. Cloud queue (SQS) - minimal ops, but ties this service to one cloud

## Decision
RabbitMQ with quorum queues and a dead-letter exchange.

## Consequences
+ Fits work-queue semantics; low learning curve
- No native replay; if analytics needs event history later, publish events to a stream separately
Revisit if throughput exceeds 5k msg/s or replay becomes a requirement.
```

## Reversible vs Irreversible Decisions

| Type | Examples | Approach |
|---|---|---|
| **Two-way door** (cheap to reverse) | Library choice behind an interface, caching strategy, logging format | Decide quickly, measure, adjust |
| **One-way door** (expensive to reverse) | Primary database, service boundaries, public API contracts, cloud provider lock-in | Invest in analysis, prototypes, and ADRs |

Keep decisions reversible where you can: hide technologies behind application interfaces, avoid proprietary features unless they provide clear value, and prefer additive changes.

## Common Decision Traps

| Trap | Better Approach |
|---|---|
| Choosing what is popular or new ("resume-driven development") | Start from requirements and constraints |
| Ignoring operational cost | Include run, monitor, upgrade, and on-call effort |
| Benchmarking the wrong thing | Measure realistic workloads end to end |
| Treating options as mutually exclusive | Combine technologies where each fits |
| Decisions without written rationale | ADRs capture context for future teams |
| Never revisiting | Define triggers for re-evaluation |

## Interview Questions
- **[L1]** Why do engineering teams need a structured way to compare technologies?
- **[L1]** What is an Architecture Decision Record?
- **[L2]** What criteria would you use to compare two technologies for a production system?
- **[L2]** What is the difference between a reversible and an irreversible technical decision?
- **[L3]** How do you handle a disagreement in your team about which technology to adopt?
- **[L3]** How do you evaluate a decision after it has been implemented?

## Interview Answers
1. Technology choices have long-lasting consequences for cost, reliability, delivery speed, hiring, and operations, and they are often made under uncertainty and competing opinions. A structured comparison forces the team to state requirements and constraints, consider realistic alternatives, weigh trade-offs with evidence rather than popularity or familiarity, and document why a decision was made, which leads to better, more defensible, and more easily revisited decisions.
2. An ADR is a short document that records one significant architectural decision: its context and constraints, the options considered, the decision made, and its consequences, both positive and negative, along with status and date. ADRs are stored with the code, typically in the repository, so future engineers understand why the system looks the way it does and can revisit decisions when the context changes.
3. Fit to functional and non-functional requirements (performance, scalability, consistency, security, compliance), total cost of ownership including licensing, infrastructure, and operations, operational complexity and maturity, team skills and hiring market, ecosystem, tooling, and community or vendor support, integration with existing systems, reversibility and lock-in, and risk. Weight criteria by importance to the specific problem and gather evidence through prototypes and benchmarks.
4. A reversible, or two-way door, decision is cheap to undo, such as a library hidden behind an interface, a caching strategy, or a configuration choice, so it should be made quickly and adjusted based on measurements. An irreversible, or one-way door, decision is expensive to change later, such as the primary database, service boundaries, public API contracts, or deep cloud-provider dependencies, so it deserves more analysis, prototypes, and review. Good design turns more decisions into reversible ones through abstraction and additive changes.
5. Move the discussion from opinions to criteria: agree on the problem, requirements, constraints, and how each criterion is weighted, then have each side present evidence, ideally through a time-boxed prototype or benchmark on a realistic workload. Use a decision matrix and write an ADR that records the options and dissenting concerns. If consensus is not reached, the accountable owner decides, and everyone commits, with explicit criteria for revisiting the decision if the concerns materialize.
6. Compare outcomes against the expectations and success criteria recorded in the ADR: performance, reliability, cost, delivery speed, incident frequency, developer experience, and operational burden, using production metrics and team feedback. Hold a lightweight retrospective after a defined period, update the ADR with lessons learned, and if the triggers for revisiting are met, run the decision process again, planning a migration only if the benefits outweigh the cost.
