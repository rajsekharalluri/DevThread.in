---
id: devops-kubernetes
slug: kubernetes
title: "Kubernetes: Pods, Deployments, Services, and Operations"
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 65
version:
  minimum: "Kubernetes 1.28+"
prerequisites: [devops-docker]
tags: [kubernetes, k8s, pods, deployments, services, configmaps, secrets]
relatedTopics: [devops-docker, architecture-microservices, devops-observability]
order: 30
status: published
---
# Kubernetes: Pods, Deployments, Services, and Operations

## Introduction
Kubernetes is a declarative container orchestration platform. You declare desired state; controllers continuously reconcile the running cluster toward it.

```text
Manifest: “3 healthy API replicas”
Kubernetes API
Scheduler assigns Pods to nodes
Controllers create/restart/replace Pods
Service routes traffic to ready Pods
```

## Purpose
Kubernetes provides scheduling, self-healing, service discovery, rolling deployments, configuration, secrets, and autoscaling for containerized workloads. It is powerful, but the operational cost is real.

## Core objects

```text
Pod        → smallest scheduled unit; one or more containers
Deployment → desired replicas + rolling update controller
Service    → stable virtual network endpoint for Pods
Ingress    → external HTTP routing
ConfigMap  → non-secret configuration
Secret     → sensitive configuration object
Namespace  → logical isolation/ownership boundary
```

## Simple example
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: academy-api
spec:
  replicas: 3
  selector:
    matchLabels: { app: academy-api }
  template:
    metadata:
      labels: { app: academy-api }
    spec:
      containers:
        - name: api
          image: registry.example/academy-api:1.5.0
          ports: [{ containerPort: 8080 }]
          resources:
            requests: { cpu: "250m", memory: "256Mi" }
            limits: { cpu: "500m", memory: "512Mi" }
          readinessProbe:
            httpGet: { path: /health/ready, port: 8080 }
---
apiVersion: v1
kind: Service
metadata: { name: academy-api }
spec:
  selector: { app: academy-api }
  ports: [{ port: 80, targetPort: 8080 }]
```

## Professional company-level example
A production Deployment uses readiness/liveness/startup probes, requests/limits, rolling update settings, PodDisruptionBudgets, workload identity, and controlled configuration:

```yaml
strategy:
  type: RollingUpdate
  rollingUpdate:
    maxUnavailable: 0
    maxSurge: 1
```

`maxUnavailable: 0` keeps existing capacity while a replacement becomes ready. A readiness probe prevents traffic from reaching a starting/unhealthy Pod; a liveness probe decides whether a stuck process should be restarted. They answer different questions.

A `Secret` is not automatically safe merely because it is called a Secret. Control RBAC, encryption at rest, external secret integration, and who can read it.

## Operations and failure

```text
Pod crashes          → controller creates replacement
Node fails           → scheduler places Pod elsewhere
Readiness fails      → Service removes Pod from traffic
Memory limit exceeded→ container may be OOMKilled
Bad deployment       → rollout pauses/rollback required
```

Monitor CPU/memory, restart count, OOMKills, pending Pods, scheduling failures, readiness, request latency, error rate, and application logs. Kubernetes can restart a process; it cannot repair incorrect business logic or lost data.

## Comparison
| Platform choice | Strength | Cost |
|---|---|---|
| Single VM | Simple | Manual scaling/self-healing |
| Managed container service | Less operations | Less orchestration control |
| Managed Kubernetes | K8s flexibility + managed control plane | Still complex workloads/networking |
| Self-managed Kubernetes | Maximum control | Highest operations burden |

## Interview Questions
- **[L1]** What is a Pod?
- **[L1]** What does a Kubernetes Service do?
- **[L2]** What is the difference between readiness and liveness probes?
- **[L2]** How does Kubernetes self-heal a failed Pod?
- **[L3]** How would you design a zero-downtime deployment?
- **[L3]** When should a team not choose Kubernetes?

## Interview Answers
1. A Pod is the smallest scheduled unit, containing one or more tightly coupled containers sharing network/storage context.
2. A Service gives a stable DNS/virtual endpoint and routes to matching healthy Pods whose IPs can change.
3. Readiness controls traffic eligibility; liveness determines whether a process should be restarted. A running process is not necessarily ready for traffic.
4. Controllers compare desired replicas with actual state and create/restart/reschedule Pods to restore the declaration.
5. Use rolling/canary strategy, compatible schema changes, readiness probes, graceful termination, resource limits, health metrics, and rollback criteria.
6. Avoid it when the workload is small/simple, the team cannot operate it, or no scheduling/scaling/failure-isolation need justifies its complexity.

## Expert perspective
Kubernetes is not a synonym for production readiness. The application still needs resource modeling, graceful shutdown, health semantics, data durability, observability, security, and a team that can operate the cluster and workloads under failure.
