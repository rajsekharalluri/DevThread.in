---
id: architecture-fundamentals
slug: fundamentals
title: Software Architecture Fundamentals
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 45
version:
  minimum: "Language and platform agnostic"
prerequisites: []
tags: [architecture, design, trade-offs, boundaries]
relatedTopics: [architecture-clean, architecture-distributed-systems, architecture-system-design]
order: 10
status: published
---
# Software Architecture Fundamentals

## Overview
Software architecture is the set of important decisions about a system's boundaries, responsibilities, data ownership, communication, deployment, and quality attributes — not a diagram or a list of fashionable technologies.

## Why This Exists
Architecture determines how easily a system can change, scale, recover from failure, and be operated by a team. A locally elegant component can still create an expensive system if it couples teams, makes data ownership unclear, or makes deployment and failure handling impossible to reason about.

## Fundamentals
Start with business capabilities and quality attributes: latency, availability, consistency, security, cost, change frequency, and team ownership. Draw boundaries around responsibilities that change together, define contracts at boundaries, and make dependencies point in a direction that keeps volatile implementation details replaceable.

## Syntax and API
An architecture decision record makes a trade-off explicit:

```markdown
# ADR-007: Keep checkout as a modular monolith

## Context
The team has six engineers and the product is still validating its payment workflow.

## Decision
Keep checkout, orders, and payments as modules in one deployable application.

## Trade-offs
We give up independent service scaling but avoid distributed transactions, network failure,
multiple deployment pipelines, and premature operational overhead.

## Revisit when
The payment domain needs independent compliance ownership or a separate scaling profile.
```

## How It Works
Architecture turns constraints into boundaries and trade-offs. A decision is valuable when it makes future change safer or cheaper, not because it introduces more components.

## Internal Implementation
Dependency direction, data ownership, synchronous/asynchronous communication, and deployment boundaries are the mechanisms behind architectural qualities. Changing a boundary changes failure modes, observability needs, and team coordination cost.

## Real-World Example
A small product starts as a modular monolith with explicit Order, Catalog, and Payment modules. The modules share a process but not internal classes or tables casually, keeping a future extraction possible without paying distributed-system cost today.

## Production Example
A platform team separates a high-volume notification worker from the request API only after queue depth and request latency demonstrate a distinct scaling need. The extraction is driven by a measurable quality attribute, not by a diagram preference.

## Common Mistakes
- Choosing microservices before identifying service boundaries.
- Treating an architecture diagram as proof of quality.
- Ignoring operational ownership and deployment cost.
- Sharing a database while pretending services own data independently.
- Optimizing for hypothetical scale instead of current constraints.

## Performance
Architecture influences network hops, serialization, cache boundaries, database access, and deployment scale. Evaluate end-to-end latency and throughput instead of optimizing one component in isolation.

## Security
Every boundary needs an explicit trust model, authentication, authorization, data classification, and audit strategy. More boundaries create more security configuration, not automatically more security.

## Testing
Use architecture fitness checks, contract tests at module/service boundaries, failure-injection tests for distributed calls, and production-like performance tests for important quality attributes.

## When to Use
Use explicit architecture decisions when a choice affects multiple teams, long-term change cost, reliability, security, or deployment topology.

## When Not to Use
Do not create heavyweight diagrams, frameworks, or abstraction layers for a small, reversible decision.

## Trade-offs
Good architecture makes important trade-offs visible. It never eliminates trade-offs; it makes the chosen costs intentional and revisitable.

## Related Topics
See [Clean Architecture](/architecture/clean-architecture), [Distributed Systems](/architecture/distributed-systems), and [System Design](/architecture/system-design).

## Practical Exercise
Write ADRs comparing a modular monolith and microservices for an e-commerce checkout with a six-person team. Include scaling, deployment, data ownership, and failure implications.

## Interview Questions
- **[L1]** What is software architecture?
- **[L1]** What quality attributes should influence an architecture decision?
- **[L2]** Why are boundaries and data ownership more important than technology names?
- **[L2]** What makes an architecture decision reversible or irreversible?
- **[L3]** How would you choose between a modular monolith and microservices for a new product?

## Interview Answers
1. **[L1] What is software architecture?** Architecture is the collection of important, long-lived decisions about system responsibilities, boundaries, data, communication, deployment, and operational behavior. It is not simply a diagram or framework selection.
2. **[L1] What quality attributes should influence an architecture decision?** At minimum, consider latency, throughput, availability, consistency, security, cost, operability, team ownership, and expected rate of change. The relevant priorities come from the product and organizational context.
3. **[L2] Why are boundaries and data ownership more important than technology names?** Boundaries determine who can change what, where failures stop, how teams coordinate, and whether data can be trusted. Technology choices are implementation details inside those decisions and can often be replaced later.
4. **[L2] What makes an architecture decision reversible or irreversible?** A reversible decision is cheap to change without migrating large data sets, retraining teams, or coordinating many deployables. Persistent schemas, public contracts, and service ownership are usually harder to reverse than an internal library choice.
5. **[L3] How would you choose between a modular monolith and microservices for a new product?** Start with team size, domain uncertainty, deployment needs, scaling profiles, compliance, and operational maturity. Prefer a modular monolith when boundaries are still being learned and independent scaling is not proven; extract a service when ownership, failure isolation, or scaling needs justify the operational cost.

## Senior Developer Perspective
Architecture is engineering judgment made explicit. Senior engineers explain why a boundary exists, what quality attribute it protects, what cost it introduces, and what evidence would justify changing it later.
