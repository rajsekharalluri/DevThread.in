---
id: comparisons-liveness-readiness
slug: liveness-readiness
title: Liveness vs Readiness vs Startup Probes
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 30
version:
  minimum: "Kubernetes/health-check concepts"
prerequisites: [devops-kubernetes, devops-observability]
tags: [kubernetes, health-checks, liveness, readiness, startup]
relatedTopics: [architecture-reliability-engineering, devops-observability]
order: 90
status: published
---
# Liveness vs Readiness vs Startup Probes

## Decision table

| Probe | Question | Failure action |
|---|---|---|
| Liveness | Is the process stuck/broken? | Restart container |
| Readiness | Should traffic arrive? | Remove from Service endpoints |
| Startup | Has slow startup completed? | Delay liveness/readiness checks |

## Example

```text
Process alive but database unavailable
  → liveness: pass (do not restart endlessly)
  → readiness: fail (stop sending traffic)

Process starting slowly
  → startup: not complete
  → liveness: not evaluated yet
```

A liveness check should not include every dependency; otherwise one temporary database outage can cause every instance to restart and make the outage worse.

## Interview Questions
- **[L1]** What does a readiness probe control?
- **[L1]** What does a liveness probe control?
- **[L2]** Why should they be separate?
- **[L2]** What problem does a startup probe solve?
- **[L3]** What should a production readiness check include?
- **[L3]** How can a bad liveness probe create cascading failure?

## Interview Answers
1. Whether the workload should receive traffic.
2. Whether the platform should restart the workload.
3. A process can be alive but unable to serve traffic; combining concerns causes incorrect restarts or traffic routing.
4. It gives a slow-starting application time to initialize before failure probes begin.
5. Dependencies required to serve requests, configuration readiness, migrations/state, and application-specific readiness—kept fast and bounded.
6. A shared dependency failure causes all replicas to fail liveness, all restart simultaneously, and increase load/recovery pressure.
