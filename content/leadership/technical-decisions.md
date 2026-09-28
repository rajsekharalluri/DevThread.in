---
id: leadership-technical-decisions
slug: technical-decisions
title: Technical Decision-Making and Trade-offs
category: leadership
categoryTitle: Leadership & Soft Skills
difficulty: senior
estimatedMinutes: 40
version:
  minimum: "Role-agnostic"
prerequisites: [architecture-fundamentals, leadership-stakeholder-communication]
tags: [leadership, decisions, trade-offs, adr, architecture]
relatedTopics: [architecture-fundamentals, leadership-conflict-resolution]
order: 60
status: published
---
# Technical Decision-Making and Trade-offs

## Introduction
Technical decisions rarely have one universally correct answer. They choose among competing goals such as delivery speed, cost, reliability, maintainability, security, scale, and team capability.

```text
Context + constraints
Decision criteria
Options + trade-offs
Decision owner
Record + revisit condition
```

## Purpose
A good process prevents loudest-voice decisions, endless debate, and repeated arguments. It makes the choice understandable to people who were not in the room and revisitable when facts change.

## Real-world simple example
Choosing monolith versus microservices depends on:

```text
Team size
Domain uncertainty
Deployment independence
Scaling profile
Operational maturity
Compliance/data ownership
```

A six-person team validating a product may choose a modular monolith because simpler deployment and local transactions outweigh independent scaling benefits.

## Professional company-level example
An ADR should record:

```markdown
# Decision: Use a modular monolith for checkout

Context: six-person team, changing payment rules, no proven scaling boundary.
Options: modular monolith, microservices, managed workflow platform.
Decision: modular monolith with explicit module boundaries.
Trade-off: less independent scaling; much lower operational/data-consistency cost.
Revisit: compliance or independent payment scaling requires extraction.
```

The record stores reasoning, not just the result. A future team can evaluate whether the original assumptions still hold.

## Decision quality
Good decisions distinguish:

- Reversible from hard-to-reverse choices.
- Facts from assumptions.
- Preferences from requirements.
- Local optimization from system impact.
- Technical risk from business risk.
- Decision ownership from consensus.

Consensus is not always required. A clear owner can decide after hearing dissent, documenting the trade-off, and explaining what evidence would trigger reconsideration.

## Comparison
| Situation | Decision style |
|---|---|
| Small/reversible | Quick owner decision |
| Cross-team design | ADR/design review |
| High-risk/irreversible | Evidence, alternatives, approval |
| Incident | Time-boxed command decision |
| Uncertain domain | Small experiment/prototype |

## Interview Questions
- **[L1]** Why do technical decisions involve trade-offs?
- **[L1]** What is an Architecture Decision Record?
- **[L2]** How do you decide when time is limited and the team disagrees?
- **[L2]** What should a good ADR contain?
- **[L3]** How do you decide how much process a decision deserves?
- **[L3]** How do you prevent a technical decision from becoming a permanent assumption?

## Interview Answers
1. Improving one goal often costs another; speed, reliability, cost, simplicity, and flexibility cannot all be maximized simultaneously.
2. An ADR records context, options, decision, trade-offs, consequences, and revisit conditions.
3. Make criteria explicit, time-box discussion, hear dissent, have a clear decision owner, choose, and communicate reasoning.
4. Context/constraints, decision, alternatives, consequences, risks, assumptions, and evidence that would justify revisiting it.
5. Match rigor to blast radius and reversibility; a high-cost schema/platform choice deserves more than a small internal refactor.
6. Record assumptions and revisit triggers, measure outcomes, and schedule review when a dependency, scale, compliance, or organizational condition changes.

## Expert perspective
Senior decision-making is not predicting the future perfectly. It is making assumptions visible, choosing consciously, preserving optionality where possible, and leaving enough reasoning that the next engineer can change the decision intelligently.
