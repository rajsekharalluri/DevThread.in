---
id: devops-source-control-migrations
slug: source-control-migrations
title: Git, Branching, CI/CD Governance, and Platform Migration
category: devops
categoryTitle: DevOps & Cloud
difficulty: senior
estimatedMinutes: 50
version:
  minimum: "Git and CI/CD concepts"
prerequisites: [devops-cicd-fundamentals, devops-iac]
tags: [git, github, gitlab, branching, migration, pcf, kubernetes]
relatedTopics: [devops-kubernetes, devops-observability]
order: 90
status: published
---
# Git, Branching, CI/CD Governance, and Platform Migration

## Introduction
Source control and platform migration are both change-management problems. Git records code history and collaboration; a migration moves a running application between environments while preserving behavior, data, security, and rollback.

```text
Change
  ↓ source control + review
Build/test artifact
  ↓ deployment automation
New platform
  ↓ traffic shift + metrics
Rollback or complete migration
```

## Purpose
Small reviewed changes reduce integration risk. A migration succeeds only when application behavior, dependencies, configuration, secrets, networking, persistence, observability, and rollback are treated as one system.

## Git flow
```bash
git switch -c feature/order-events
git add src/ tests/
git commit -m "Publish order paid event"
git push -u origin feature/order-events
```

Protect main branches with required reviews, CI/security checks, code owners, and no direct force-push. Rebase/merge strategy should optimize integration clarity, not become a team identity debate.

## Professional PCF/Kubernetes migration
Before changing the platform, inventory:

```text
Routes/domains
Service bindings
Environment variables
Secrets/certificates
Scheduled jobs
Persistent storage
Network dependencies
Health endpoints
Resource usage
Dashboards/alerts
Rollback steps
```

Then:

```text
Existing PCF deployment
        ├── baseline latency/errors/resource usage
        ├── containerize application
        ├── map bindings → managed services/Secrets/ConfigMaps
        ├── deploy beside old platform
        ├── run contract/load/security tests
        ├── shift small traffic percentage
        └── increase or rollback based on business/technical metrics
```

## Failure scenarios
- Application and platform changes happen together, making regression diagnosis impossible.
- A PCF service binding is copied as plaintext Kubernetes configuration.
- No baseline exists, so “same behavior” cannot be measured.
- Old and new versions are incompatible with the same database schema.
- Rollback is theoretical and the old platform was removed too early.

## Comparison
| Strategy | Benefit | Risk |
|---|---|---|
| Big-bang cutover | Short migration window | Large blast radius |
| Wave migration | Limits risk/learns early | Takes longer |
| Canary | Evidence with small traffic | Requires routing/metrics |
| Blue/green | Fast traffic switch/rollback | Double capacity/cost |
| Strangler | Incremental replacement | Temporary complexity |

## Interview Questions
- **[L1]** What is a protected branch?
- **[L1]** Why build and promote one immutable artifact?
- **[L2]** What should be inventoried before a PCF-to-Kubernetes migration?
- **[L2]** Why are baseline metrics important?
- **[L3]** How would you design a migration that can be rolled back safely?
- **[L3]** How do you separate application defects from platform-migration defects?

## Interview Answers
1. A branch requiring configured reviews/checks before merge, protecting shared integration and release quality.
2. It proves the tested artifact is the deployed artifact and prevents environment-specific rebuild drift.
3. Routes, dependencies, service bindings, secrets, certificates, jobs, storage, networking, probes, resources, observability, and rollback criteria.
4. Without before/after latency, errors, throughput, and resource baselines, a migration cannot prove whether behavior regressed.
5. Run old/new in parallel, use compatible schema changes, deploy canary/blue-green, retain old capacity, define metric-based rollback, and test the rollback itself.
6. Separate variables, use the same automated contract tests, compare traces/metrics, release in small waves, and change one major dimension at a time where possible.

## Expert perspective
Modernization is not successful because the new platform accepted a deployment. It is successful when users, data, security, operations, and recovery behavior remain correct and measurable through the transition.
