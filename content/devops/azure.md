---
id: devops-azure
slug: azure
title: Azure Core Services and Cloud Deployment
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Azure current concepts"
prerequisites: [devops-cicd-fundamentals, devops-iac]
tags: [azure, app-service, aks, functions, devops]
relatedTopics: [devops-aws, devops-kubernetes, devops-iac]
order: 50
status: published
---
# Azure Core Services and Cloud Deployment

## Introduction
Azure provides managed compute, identity, storage, databases, networking, and delivery services. It is especially integrated with .NET and Microsoft identity, while supporting Linux and open-source workloads fully.

```text
Azure DevOps pipeline
       ↓ artifact
App Service / AKS / Functions
       ├── Managed database/storage
       ├── Managed Identity
       ├── Key Vault
       └── Monitor/Application Insights
```

## Purpose
Azure lets teams deploy and operate cloud applications without managing every physical server. The important skills are environment separation, identity, deployment safety, monitoring, and rollback.

## Services and decisions

| Service | Use |
|---|---|
| App Service | Managed web/API hosting |
| Azure Functions | Event-driven serverless code |
| AKS | Managed Kubernetes control plane |
| Azure DevOps | Repos/work items/CI/CD |
| Entra ID | Identity and access |
| Key Vault | Secrets/certificates/keys |
| Application Insights | Logs, metrics, traces |
| Bicep/ARM | Infrastructure as code |

## Real-World Simple Example
Deploy a .NET API to an App Service staging slot, run smoke tests, and swap the slot into production only after the checks pass.

```bash
az webapp deployment source config-zip \
  --resource-group academy-rg \
  --name academy-api \
  --slot staging \
  --src build.zip

az webapp deployment slot swap \
  --resource-group academy-rg \
  --name academy-api \
  --slot staging \
  --target-slot production
```

## Professional company-level example
Use Managed Identity to access Key Vault instead of storing database/API secrets in app settings. Use separate subscriptions/resource groups/service connections for dev/staging/production. Configure readiness/health checks, autoscale, alerts, and deployment slots or canary traffic.

```text
Commit
  ↓ Azure DevOps build/test/scan
Artifact
  ↓ staging slot
Smoke + contract checks
  ↓ approval
Production swap/rollout
  ↓ monitor
Rollback swap if required
```

## Failure scenarios
- Subscription-wide Owner permissions on pipeline identity.
- Secret copied into repository or plain pipeline logs.
- Production deployment without a warm-up/rollback path.
- App Service resource scales but database becomes the bottleneck.
- AKS cluster exists but workloads lack requests, probes, or network policy.

## Interview Questions
- **[L1]** What is Azure App Service?
- **[L1]** What is an App Service deployment slot?
- **[L2]** What is Managed Identity and why is it safer than a stored key?
- **[L2]** What does AKS manage versus what does the application team manage?
- **[L3]** How would you design a secure multi-environment Azure deployment?
- **[L3]** How would you implement safe rollback for a production release?

## Interview Answers
1. App Service is managed hosting for web apps/APIs where Azure handles much of the VM/OS/load-balancing work.
2. A slot is a separate running deployment environment that can be warmed/tested and swapped into production with a fast rollback path.
3. Azure assigns temporary workload credentials to a resource, avoiding static secrets in code/configuration and allowing scoped resource permissions.
4. Azure manages the AKS control plane; teams manage worker capacity, manifests, workloads, networking policies, resource sizing, and application operations.
5. Separate environments/identities, least privilege, Key Vault/Managed Identity, protected production approvals, IaC, policy checks, logs, and restore/rollback tests.
6. Keep a known-good artifact and deployment state, use slots/blue-green/canary, health checks, backward-compatible migrations, and an explicit rollback command/runbook.

## Expert perspective
Azure's managed services reduce infrastructure work but do not remove operational responsibility. Identity, secrets, rollout strategy, database capacity, and observability still determine whether a deployment is safe.
