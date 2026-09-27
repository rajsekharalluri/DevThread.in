---
id: dotnet-logging-diagnostics
slug: logging-diagnostics
title: .NET Logging, Diagnostics, Health Checks, and Observability
category: dotnet
categoryTitle: .NET Backend
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: ".NET 8+"
prerequisites: [dotnet-aspnet-core-backend, devops-observability]
tags: [dotnet, logging, diagnostics, health-checks, observability]
relatedTopics: [dotnet-runtime-internals, devops-observability]
order: 70
status: published
---
# .NET Logging, Diagnostics, Health Checks, and Observability

## Introduction
A production .NET service needs more than log lines. Logs explain individual events, metrics show behavior over time, traces show a request across dependencies, and health checks tell an orchestrator whether an instance should receive traffic.

```text
Request
  │
  ├── trace/span: where time was spent
  ├── structured logs: what happened
  ├── metrics: how often/how badly
  └── readiness: should this instance receive traffic?
```

## Purpose
The goal is to answer an incident question quickly: is the problem the application, database, dependency, queue, deployment, resource limit, or a specific tenant/request? Observability should reduce guesswork without leaking secrets or creating unaffordable noise.

## Real-World Simple Example
```csharp
logger.LogInformation(
    "Order {OrderId} submitted for tenant {TenantId}",
    orderId,
    tenantId);
```

Named properties remain queryable. This is better than:

```csharp
logger.LogInformation($"Order {orderId} submitted");
```

The interpolated version turns useful data into an unstructured string.

## Professional production example
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
```

Use separate probes:

```text
Liveness  → Is the process alive enough to restart?
Readiness → Can this instance safely receive traffic?
```

Add OpenTelemetry traces/metrics, propagate trace IDs through HTTP/message headers, and alert on error rate, p95/p99 latency, queue age, dependency failures, and saturation rather than CPU alone.

## Practical diagnostics

```bash
dotnet-counters monitor --process-id <pid> System.Runtime
dotnet-trace collect --process-id <pid> --duration 00:00:30
journalctl -u academy-api -n 100 --no-pager
```

Useful signals include allocation rate, GC pauses, thread-pool queue length, CPU, working set, request latency, database time, and outgoing HTTP timing.

## Comparison
| Signal | Best question | Common mistake |
|---|---|---|
| Structured log | What happened in this event? | Logging unqueryable strings |
| Metric | Is the system trending unhealthy? | High-cardinality labels |
| Trace | Where did this request spend time? | No propagation across services |
| Liveness | Should the process restart? | Including every dependency |
| Readiness | Should traffic be sent here? | Treating it as liveness |

## Security and cost
Never log passwords, tokens, connection strings, full payment details, or sensitive payloads. Redact before logging. Control access to logs because logs can contain sensitive business data. High-cardinality metric labels such as raw user IDs can explode storage cost; put those values in traces/logs instead.

## Interview Questions
- **[L1]** What is structured logging?
- **[L1]** What are logs, metrics, and traces used for?
- **[L2]** Why should liveness and readiness be separate?
- **[L2]** How does a trace ID help debug a distributed request?
- **[L3]** How would you design alerts without creating alert fatigue?
- **[L3]** What production data should never be logged?

## Interview Answers
1. Structured logging stores named fields such as `OrderId` and `TenantId`, allowing reliable filtering and correlation instead of fragile text parsing.
2. Logs explain events, metrics show trends/health, and traces show a request's path/timing through services and dependencies.
3. A process can be alive but unable to serve traffic because a required dependency is unavailable or startup is incomplete. Separate probes let the platform restart only when appropriate and remove unready instances from traffic.
4. The same trace ID is propagated across service calls, allowing logs/spans from one user operation to be joined into a timeline.
5. Alert on sustained user-impact symptoms with a clear owner/runbook, tune durations to avoid transient noise, and regularly retire alerts that do not lead to action.
6. Never log credentials, tokens, passwords, full payment/card data, or sensitive request bodies. Redact and protect the logging system itself.

## Expert perspective
Observability is part of the service contract. A feature that cannot reveal its failure mode in production is incomplete, even if its happy-path tests pass.
