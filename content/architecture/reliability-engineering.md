---
id: architecture-reliability-engineering
slug: reliability-engineering
title: Reliability Engineering, SLOs, and Disaster Recovery
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 50
version:
  minimum: "Concept-level"
prerequisites: [architecture-resilience, devops-observability, architecture-system-design]
tags: [reliability, slo, sla, rto, rpo, disaster-recovery]
relatedTopics: [architecture-application-security, devops-observability]
order: 200
status: published
---
# Reliability Engineering, SLOs, and Disaster Recovery

## Introduction
Reliability engineering defines measurable service behavior and designs systems to meet it under normal operation and failure. Availability is not a feeling; it is observed against a target and an agreed user experience.

```text
User journey
   ↓ SLI measurement
SLO target
   ↓ error budget
Release/operations decision
   ↓ incidents + learning
Reliability improvement
```

## Purpose
SLOs connect engineering effort to user impact. Disaster recovery ensures the organization can restore service/data after serious failure instead of assuming backups and redundancy are enough.

## Core terms

- **SLI:** measured indicator, such as successful request ratio or p99 latency.
- **SLO:** target, such as 99.9% successful requests in a month.
- **SLA:** external commitment with consequences.
- **RTO:** acceptable time to restore service.
- **RPO:** acceptable amount of data loss measured in time.
- **Error budget:** permitted unreliability before release/risk policy changes.

## Professional company-level example
```text
SLO: 99.9% successful checkout requests/month
SLIs: error rate, p99 latency, payment completion
Budget: ~43.2 minutes/month unavailable

Budget healthy → normal release pace
Budget exhausted → prioritize reliability, freeze risky releases
```

For recovery:

```text
Primary failure
   ↓ detect/declare incident
Failover/restore
   ↓ validate data/service
Traffic recovery
   ↓ reconcile missed/duplicate work
Post-incident learning
```

Backups are only useful if restore is tested. A multi-AZ database is not a full disaster-recovery plan if the region, account, credentials, or data corruption affects all copies.

## Interview Questions
- **[L1]** What is an SLO?
- **[L1]** What is the difference between RTO and RPO?
- **[L2]** What is an error budget used for?
- **[L2]** Why are backups not enough without restore testing?
- **[L3]** How would you design reliability goals for a payment system?
- **[L3]** How do you balance feature delivery and reliability investment?

## Interview Answers
1. An SLO is a measurable target for a service behavior, such as availability, latency, or successful completion.
2. RTO is how quickly service must be restored; RPO is how much recent data loss is acceptable.
3. It quantifies permitted unreliability and helps decide whether to continue risky releases or invest in reliability work.
4. Backups can be corrupt, incomplete, inaccessible, or too slow to restore; only a tested restore proves the recovery process works.
5. Define success/latency SLIs for the user journey, protect idempotency/reconciliation, set strict recovery/data goals, test provider failure, and audit/alert on error budget consumption.
6. Use error-budget policy: when reliability is within target, deliver normally; when the budget is consumed, prioritize fixing causes and reduce release risk until service recovers.

## Expert perspective
Reliability is a product promise supported by engineering evidence. Senior engineers make targets measurable, test failure/recovery before incidents, and use error budgets to resolve feature-versus-reliability arguments with shared data rather than opinion.
