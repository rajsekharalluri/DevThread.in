---
id: devops-observability
slug: observability
title: Observability, Monitoring, and Production Troubleshooting
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Platform agnostic"
prerequisites: [devops-kubernetes, devops-cicd-fundamentals]
tags: [observability, monitoring, logging, metrics, tracing, incidents]
relatedTopics: [dotnet-logging-diagnostics, architecture-resilience]
order: 80
status: published
---
# Observability, Monitoring, and Production Troubleshooting

## Introduction
Observability is the ability to infer system behavior from external signals. The three main signals are logs, metrics, and traces. Monitoring turns selected signals into dashboards and alerts; observability supports investigation of failures the team did not predict in advance.

```text
User request
   ├── trace/span: path and timing
   ├── structured logs: event details
   ├── metrics: rates/latency/saturation
   └── alert/runbook: operator action
```

## Purpose
Production problems need evidence. “The server is slow” becomes actionable when you know error rate, p99 latency, dependency timing, queue age, CPU/GC/thread-pool behavior, and the exact trace for a failing request.

## Real-World Simple Example
```csharp
logger.LogInformation(
    "Order {OrderId} submitted for tenant {TenantId} in {ElapsedMs}ms",
    orderId, tenantId, elapsedMilliseconds);
```

A metric can count submissions/failures, and a trace can show whether time was spent in the database, payment provider, or application code.

## Professional company-level troubleshooting flow

```text
Alert: checkout p99 latency increased
Check error rate and deployment timeline
Trace slow requests
Compare spans: API / DB / payment / queue
Check saturation: CPU, GC, threads, connections
Mitigate: rollback, circuit/fallback, scale, or disable feature
Root-cause analysis + prevention
```

Do not page on every CPU spike. Page on sustained user-impact symptoms with an owner and runbook; use lower-severity dashboards for supporting signals.

## Important production signals

- Request rate and error rate.
- p50/p95/p99 latency.
- Database duration, pool exhaustion, locks.
- HTTP dependency timeouts/retries.
- Queue depth and oldest message age.
- CPU, memory, GC, thread pool, restarts/OOMKills.
- Deployment version and feature flags.
- Business metrics such as payment failures or order completion.

## Comparison
| Signal | Answers |
|---|---|
| Log | What happened in this event? |
| Metric | Is the system trending unhealthy? |
| Trace | Where did this request spend time? |
| Dashboard | What is normal/current status? |
| Alert | What requires action now? |
| Runbook | What should the operator do? |

## Interview Questions
- **[L1]** What are logs, metrics, and traces used for?
- **[L1]** What is structured logging?
- **[L2]** How does distributed tracing connect services?
- **[L2]** Why should alerts focus on symptoms and user impact?
- **[L3]** How would you investigate a latency incident after a deployment?
- **[L3]** How do you reduce alert fatigue without missing incidents?

## Interview Answers
1. Logs provide event detail, metrics provide numeric trends/health, and traces show an individual request's path/timing across dependencies.
2. It stores named fields such as IDs and status values so log systems can filter/aggregate them reliably instead of parsing free text.
3. A trace/context ID propagates across service boundaries; each service records spans with parent/child relationships so a backend can reconstruct the request timeline.
4. Operators need a clear action signal. Internal metrics can fluctuate without user impact; symptom alerts reduce noise and focus response on real service degradation.
5. Establish whether errors/latency started with the deployment, inspect traces to locate the slow span, check DB/HTTP/queue/resource saturation, mitigate with rollback/fallback/scale, then use logs and profiles for root cause.
6. Tune duration/thresholds, alert on SLIs, deduplicate related alerts, assign owners/runbooks, review false positives, and retire alerts that do not lead to action.

## Expert perspective
Observability is part of feature delivery. A new endpoint or worker should define its important logs, metrics, trace boundaries, health behavior, and alert/runbook expectations before production, not after the first incident.
