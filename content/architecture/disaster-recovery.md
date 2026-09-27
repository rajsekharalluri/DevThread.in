---
id: architecture-disaster-recovery
slug: disaster-recovery
title: Disaster Recovery, Backups, and Restore Testing
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: senior
estimatedMinutes: 45
version:
  minimum: "Concept-level"
prerequisites: [architecture-reliability-engineering, devops-aws]
tags: [reliability, disaster-recovery, backups, restore, rto, rpo]
relatedTopics: [architecture-resilience, devops-observability]
order: 220
status: published
---
# Disaster Recovery, Backups, and Restore Testing

## Introduction
Disaster recovery is the ability to restore an acceptable service/data state after a major failure. Backups are inputs to recovery; they are not proof that recovery works.

```text
Failure
  ↓ detect/declare
Choose recovery plan
  ├── failover
  ├── restore backup
  ├── rebuild infrastructure
  └── reconcile missed/duplicate work
  ↓ validate data/service
Resume + learn
```

## Purpose
Define Recovery Time Objective (RTO: how quickly service returns) and Recovery Point Objective (RPO: how much data/time may be lost). These are business decisions that drive replication, backup frequency, cost, and testing.

## Simple example
```text
RTO: 60 minutes
RPO: 15 minutes
```

A nightly backup cannot meet a 15-minute RPO. A highly replicated database may meet RPO but still fail RTO if DNS, credentials, application artifacts, and operators are not ready.

## Professional company-level plan
A recovery plan covers:

- Data backup frequency/retention/encryption.
- Cross-AZ/region/account isolation.
- Infrastructure-as-code rebuild.
- Secrets/certificate recovery.
- DNS/traffic failover.
- Dependency recovery order.
- Data integrity/reconciliation.
- Owner/runbook/escalation.
- Restore drills and evidence.

```text
Database restore → service start → migrations/compatibility
→ queue replay/reconciliation → traffic enablement → business validation
```

## Common failure scenarios
- Backup exists but cannot be decrypted/accessed.
- Restore takes longer than the RTO.
- Backup contains corruption copied from source.
- Region/account failure affects backup too.
- Application version is incompatible with restored schema.
- Recovery ignores queues, caches, certificates, or external providers.

## Interview Questions
- **[L1]** What are RTO and RPO?
- **[L1]** Why are backups not enough?
- **[L2]** What should a disaster-recovery runbook contain?
- **[L2]** Why should backups be isolated from the primary environment?
- **[L3]** How would you design recovery for a payment system?
- **[L3]** How do you prove a recovery plan works?

## Interview Answers
1. RTO is acceptable restoration time; RPO is acceptable data-loss window measured in time.
2. Restore may fail due to corruption, access, key, dependency, compatibility, or duration problems; only a test validates the whole path.
3. Dependencies/order, contacts, commands, credentials, failover/restore steps, validation, reconciliation, rollback, and communication criteria.
4. A compromised/deleted primary environment must not be able to destroy every backup; use separate account/region/permissions and immutable retention where needed.
5. Define strict payment/order state, use idempotency/reconciliation, protect ledger data, preserve audit history, and rehearse provider/database recovery without double-charging.
6. Run scheduled restore/failover drills, measure RTO/RPO, validate business workflows/data integrity, and fix gaps discovered by the exercise.

## Expert perspective
Recovery is an operational capability, not a document. Senior engineers schedule restore drills and measure them; an untested backup is an assumption, not a recovery plan.
