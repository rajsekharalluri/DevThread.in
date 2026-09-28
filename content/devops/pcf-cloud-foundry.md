---
id: devops-pcf-cloud-foundry
slug: pcf-cloud-foundry
title: PCF/Cloud Foundry and Platform Migration
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Cloud Foundry/Tanzu concepts"
prerequisites: [devops-docker, devops-kubernetes]
tags: [pcf, cloud-foundry, tanzu, paas, migration]
relatedTopics: [devops-source-control-migrations, devops-kubernetes]
order: 60
status: published
---
# PCF/Cloud Foundry and Platform Migration

## Introduction
Cloud Foundry/PCF is a Platform as a Service. Developers push application code or an artifact; the platform handles buildpacks, routing, instances, health checks, service bindings, and much of the orchestration.

```text
Source/artifact
      ↓ cf push
Buildpack/container staging
App instances + route
      ├── service bindings
      ├── health checks
      └── platform logs
```

## Purpose
A PaaS lets application teams focus on code while a platform team owns deployment/runtime mechanics. The trade is less low-level control than Kubernetes in exchange for simpler delivery.

## Simple example
```bash
cf push academy-api -m 512M -i 3
cf create-service postgresql-db small-plan academy-db
cf bind-service academy-api academy-db
cf scale academy-api -i 5
```

The platform injects service information; application code should not hardcode database credentials.

## Professional PCF-to-Kubernetes migration

```text
PCF app inventory
  ├── routes/domains
  ├── buildpack/runtime version
  ├── env vars/service bindings
  ├── certificates/secrets
  ├── scheduled jobs
  ├── persistence/dependencies
  └── CPU/memory/traffic baseline
Container image + Kubernetes Deployment
        ├── ConfigMaps/Secrets
        ├── Service/Ingress
        ├── readiness/liveness probes
        ├── requests/limits
        └── managed service mapping
```

Migrate in waves. Run old/new platforms in parallel, verify business/latency/error behavior, shift traffic gradually, and retain rollback capacity until a stability window completes.

## Important differences

| PCF | Kubernetes |
|---|---|
| Opinionated `cf push` experience | Declarative low-level primitives |
| Buildpacks commonly build apps | Developer/platform controls images |
| Service bindings are platform abstraction | Secrets/config/service operators vary |
| Simpler app-team workflow | More control and operational responsibility |

## Failure scenarios
- Buildpack runtime behavior is not reproduced in the container.
- `VCAP_SERVICES` assumptions are not mapped to Kubernetes configuration.
- PCF routes/certificates are not migrated correctly.
- No resource/probe settings in Kubernetes.
- Data migration/rollback is not tested.
- Old/new versions are incompatible with the same schema.

## Interview Questions
- **[L1]** What does `cf push` do?
- **[L1]** What is a buildpack?
- **[L2]** How are PCF service bindings different from Kubernetes Secrets/ConfigMaps?
- **[L2]** What should be inventoried before a platform migration?
- **[L3]** How would you migrate a production PCF application to Kubernetes safely?
- **[L3]** How do you prove the new platform has not changed user behavior?

## Interview Answers
1. It uploads source/artifact, stages it with a buildpack, deploys instances, and connects routes/services according to manifest/platform configuration.
2. A buildpack detects/builds the application runtime and dependencies without an explicit Dockerfile; Kubernetes usually expects a prepared image.
3. Bindings expose service credentials/configuration through platform mechanisms; Kubernetes needs deliberate Secret/ConfigMap/operator/managed-service design and access controls.
4. Inventory routes, env vars, bindings, secrets, certificates, schedules, storage, dependencies, resource use, health endpoints, dashboards, and rollback.
5. Build/rehearse image and manifests, map services/secrets, run contract/load/security tests, deploy alongside PCF, canary traffic, compare metrics, and retain rollback.
6. Capture pre-migration baseline, run identical business workflows, compare errors/latency/throughput/data/retries, and monitor after gradual traffic shifts.

## Expert perspective
A platform migration is successful only when users and operators see equivalent or better behavior. “The Pod is running” proves deployment, not migration correctness; dependencies, data, security, observability, and rollback prove the real result.
