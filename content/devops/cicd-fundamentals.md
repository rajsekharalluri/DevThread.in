---
id: devops-cicd-fundamentals
slug: cicd-fundamentals
title: CI/CD Fundamentals and Release Engineering
category: devops
categoryTitle: DevOps & Cloud
difficulty: intermediate
estimatedMinutes: 50
version:
  minimum: "Platform agnostic"
prerequisites: []
tags: [devops, cicd, pipelines, releases, automation]
relatedTopics: [devops-docker, devops-source-control-migrations, devops-observability]
order: 10
status: published
---
# CI/CD Fundamentals and Release Engineering

## Introduction
Continuous Integration automatically builds and tests changes as they are integrated. Continuous Delivery keeps every passing change deployable; Continuous Deployment automatically releases approved changes to production.

```text
Commit / pull request
Build + unit tests + lint/security checks
Immutable artifact
Staging + smoke/contract tests
Approval or automated policy
Production + health checks + rollback
```

## Purpose
CI/CD reduces integration surprises, makes releases repeatable, and moves deployment risk from a rare manual event to a small, observable change that can be stopped or rolled back quickly.

## Simple pipeline example
```yaml
name: CI
on: [push, pull_request]
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '10.0.x' }
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build
```

## Professional company-level release
Build once and promote the same artifact:

```text
Build commit abc123 → artifact image:1.4.0
Deploy image:1.4.0 to staging
       ↓ smoke/contract/load checks
Promote image:1.4.0 to production
       ↓ health check
Monitor error rate/latency
       ↓ failure
Rollback image:1.3.9
```

Do not rebuild independently for production. A second build can resolve different dependencies, compiler behavior, or environment inputs; the artifact tested should be the artifact deployed.

## Release strategies

| Strategy | Behavior | Useful when |
|---|---|---|
| Rolling | Replace instances gradually | Normal stateless services |
| Blue/green | Keep old/new environments, switch traffic | Fast rollback |
| Canary | Send small traffic percentage first | High-risk changes |
| Feature flag | Deploy code, activate behavior separately | Gradual product rollout |
| Big bang | Switch everything together | Rarely justified |

## Secrets and supply chain
Secrets belong in a CI secret store/secret manager, not YAML or source. Pipeline identities should have environment-scoped permissions. Scan dependencies/images, pin important action/tool versions, and protect production approvals.

## Failure scenarios
- Tests pass but artifact differs between staging and production.
- Migration succeeds halfway and rollback is unsafe.
- Health check only tests process liveness, not readiness.
- Pipeline credential can change every production resource.
- No previous artifact is retained for rollback.

## Interview Questions
- **[L1]** What is the difference between CI, Continuous Delivery, and Continuous Deployment?
- **[L1]** Why should CI run on pull requests?
- **[L2]** Why build once and promote one immutable artifact?
- **[L2]** How should secrets and pipeline permissions be handled?
- **[L3]** How would you design a zero-downtime deployment with rollback?
- **[L3]** How do you measure whether a CI/CD process is improving delivery rather than just adding automation?

## Interview Answers
1. CI builds/tests integrated changes; Continuous Delivery keeps passing changes deployable; Continuous Deployment automatically releases them to production.
2. It catches integration problems before main becomes broken and gives authors fast feedback.
3. It proves the tested artifact is the released artifact and removes environment-specific rebuild drift.
4. Use a secret manager, short-lived/scoped identities, protected environments, and never print secrets in logs.
5. Use rolling/blue-green/canary deployment, readiness checks, backward-compatible schema changes, retained last-good artifacts, automated health verification, and one-click rollback.
6. Measure lead time, deployment frequency, change failure rate, time to restore, flaky-test rate, pipeline duration, rollback frequency, and whether developers receive useful feedback quickly.

## Expert perspective
CI/CD is production infrastructure. A pipeline is successful when it makes safe delivery boring, repeatable, observable, and reversible—not merely when it contains many automated steps.
