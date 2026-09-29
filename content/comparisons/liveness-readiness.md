---
id: comparisons-liveness-readiness
slug: liveness-readiness
title: Liveness vs Readiness vs Startup Probes
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 20
version:
  minimum: "Kubernetes 1.20+, ASP.NET Core 8+"
prerequisites: [devops-kubernetes, devops-observability]
tags: [kubernetes, health-checks, liveness, readiness, startup]
relatedTopics: [architecture-reliability-engineering, devops-observability]
order: 90
status: published
---
# Liveness vs Readiness vs Startup Probes

## Introduction

Kubernetes probes let the platform ask your container three different questions, and each answer triggers a different action:

- **Startup probe:** "Have you finished starting?" Until it succeeds, the other probes are not run.
- **Liveness probe:** "Are you stuck beyond recovery?" If it fails repeatedly, Kubernetes **restarts** the container.
- **Readiness probe:** "Can you serve traffic right now?" If it fails, Kubernetes **removes the pod from Service endpoints** but does not restart it.

Mixing these up is one of the most common causes of self-inflicted outages: a liveness probe that checks the database can restart every replica during a brief database blip and turn a minor incident into a full outage.

## Quick Decision Table

| Probe | Question | Action on Failure | Should Check | Should NOT Check |
|---|---|---|---|---|
| **Startup** | Has initialization completed? | Keeps waiting; restart after `failureThreshold x periodSeconds` | App finished warm-up, migrations done, model loaded | Anything slow that runs forever |
| **Liveness** | Is the process healthy enough to keep running? | Restart container | Process responsive, no deadlock, event loop alive | External dependencies (DB, cache, other services) |
| **Readiness** | Should this instance receive requests? | Remove from load balancing | Critical dependencies for serving, warm caches, not draining | Non-critical dependencies, slow deep checks |

## Startup Probe

### When to Use

- Applications with slow or variable startup: JIT warm-up, loading ML models, large caches, running migrations
- Anything where a fixed `initialDelaySeconds` is either too long (slow rollouts) or too short (killed during boot)

### Example

```yaml
startupProbe:
  httpGet: { path: /health/startup, port: 8080 }
  periodSeconds: 5
  failureThreshold: 60      # allows up to 5 minutes to start
```

## Liveness Probe

### When to Use

- To recover from states the application cannot fix by itself: deadlocks, exhausted thread pools, corrupted in-memory state

### When Not to Use (or Keep Minimal)

- Never to report dependency health - restarting your service does not fix the database
- If your app crashes on fatal errors anyway, a liveness probe adds little; keep it very cheap

### Example

```yaml
livenessProbe:
  httpGet: { path: /health/live, port: 8080 }
  periodSeconds: 10
  timeoutSeconds: 2
  failureThreshold: 3       # restart only after ~30s of consecutive failures
```

## Readiness Probe

### When to Use

- To stop sending traffic while an instance cannot serve: dependencies required for every request are unreachable, the instance is overloaded, or it is shutting down
- During rolling deployments, so new pods only receive traffic once they are ready

### Example

```yaml
readinessProbe:
  httpGet: { path: /health/ready, port: 8080 }
  periodSeconds: 5
  timeoutSeconds: 2
  failureThreshold: 2
```

## Implementing the Three Endpoints in ASP.NET Core

```csharp
builder.Services.AddSingleton<StartupState>();
builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddCheck<StartupCompletedCheck>("startup", tags: ["startup"])
    .AddNpgSql(builder.Configuration.GetConnectionString("Orders")!, name: "orders-db",
               timeout: TimeSpan.FromSeconds(1), tags: ["ready"])
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!, name: "redis",
              failureStatus: HealthStatus.Degraded, tags: ["ready"]); // degraded does not fail readiness

var app = builder.Build();

app.MapHealthChecks("/health/live",    new HealthCheckOptions { Predicate = c => c.Tags.Contains("live") });
app.MapHealthChecks("/health/startup", new HealthCheckOptions { Predicate = c => c.Tags.Contains("startup") });
app.MapHealthChecks("/health/ready",   new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

public sealed class StartupState { public volatile bool Completed; }

public sealed class StartupCompletedCheck(StartupState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default) =>
        Task.FromResult(state.Completed ? HealthCheckResult.Healthy() : HealthCheckResult.Unhealthy("warming up"));
}
```

**Why each part exists:**
- **Tags** separate what each endpoint checks, so liveness never touches the database
- **Short timeouts** keep probes from hanging and piling up
- **Degraded** status for optional dependencies (cache) keeps the pod in rotation while logging the issue
- **StartupState** is flipped to `true` by a hosted service after warm-up completes

`AddNpgSql` and `AddRedis` come from the community `AspNetCore.HealthChecks.*` packages.

## Graceful Shutdown and Readiness

When Kubernetes terminates a pod, endpoint removal and `SIGTERM` happen concurrently. Without care, in-flight requests fail.

```yaml
spec:
  terminationGracePeriodSeconds: 45
  containers:
    - name: api
      lifecycle:
        preStop:
          exec: { command: ["sleep", "10"] }   # give load balancers time to stop routing
```

```csharp
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));
```

## Same Scenario: Database Outage for 60 Seconds

| Probe Design | What Happens |
|---|---|
| Liveness checks the database | All replicas fail liveness, all restart at once, cold starts add load when the DB recovers, outage extends |
| Readiness checks the database, liveness checks only the process | Pods leave rotation, clients get fast 503s from the gateway or retry, pods rejoin automatically when the DB recovers - no restarts |
| No readiness probe | Pods keep receiving traffic and return slow errors or time out |

## Decision Matrix: What Belongs Where

| Check | Startup | Liveness | Readiness |
|---|---|---|---|
| Process responds to HTTP | Yes | Yes | Yes |
| Warm-up / migrations / model loaded | Yes | No | Optional |
| Primary database reachable | Optional | **No** | Yes (if every request needs it) |
| Optional cache / feature service | No | No | Degraded, not failing |
| Downstream microservices | No | No | Usually no (use circuit breakers instead) |
| Instance draining for shutdown | No | No | Yes |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Same endpoint for liveness and readiness | Separate endpoints with different checks |
| Liveness depends on the database | Liveness checks only the process |
| Readiness checks every downstream service | Only dependencies required to serve; use circuit breakers for the rest |
| Long-running probe handlers | Fast, bounded checks with timeouts; cache expensive results briefly |
| `initialDelaySeconds` guesswork for slow apps | Startup probe |
| Aggressive thresholds (fail after 1 miss) | Tolerate transient blips with sensible `failureThreshold` |

## Interview Questions
- **[L1]** What does a readiness probe control?
- **[L1]** What does a liveness probe control?
- **[L2]** Why should they be separate?
- **[L2]** What problem does a startup probe solve?
- **[L3]** What should a production readiness check include?
- **[L3]** How can a bad liveness probe create cascading failure?

## Interview Answers
1. A readiness probe controls whether a pod receives traffic. When it fails, Kubernetes removes the pod's IP from the Service endpoints so load balancers stop routing requests to it, without restarting the container. When it passes again, the pod is added back. It is used during startup, rolling deployments, temporary dependency issues, overload, and graceful shutdown.
2. A liveness probe controls whether Kubernetes restarts the container. If the probe fails `failureThreshold` times in a row, the kubelet kills and restarts the container according to its restart policy. It exists to recover from states the application cannot escape on its own, such as deadlocks or hung event loops.
3. They answer different questions with different consequences. A process can be alive but temporarily unable to serve because a dependency is down or it is warming up; the right response is to stop routing traffic, not to restart. Combining them either restarts healthy processes unnecessarily, causing restart storms and lost in-flight work, or keeps routing traffic to instances that cannot serve.
4. A startup probe handles slow or unpredictable initialization such as loading large models, warming caches, or running migrations. While it has not succeeded, liveness and readiness probes are disabled, so the container is not killed during boot, and once it succeeds the normal probes take over with tight thresholds. This avoids choosing between a long `initialDelaySeconds` that slows every rollout and a short one that kills slow starters.
5. It should check what the instance needs to serve requests successfully: that startup and warm-up are complete, that essential dependencies used by every request (for example the primary database) are reachable with a short timeout, that configuration and secrets loaded correctly, and that the instance is not draining for shutdown. It should be fast and bounded, treat optional dependencies as degraded rather than failing, and avoid deep checks of downstream microservices, which are better handled by circuit breakers.
6. If the liveness probe includes a shared dependency such as a database or another service, a brief outage makes every replica fail liveness at roughly the same time. Kubernetes restarts all of them, dropping in-flight requests, and the restarting pods are unavailable and cold, so when the dependency recovers they reconnect simultaneously and add load. Slow probes under heavy load can also fail liveness and trigger restarts that increase load on the remaining pods, creating a restart spiral that turns a small incident into a full outage.
