---
id: devops-capacity-cost-engineering
slug: capacity-cost-engineering
title: Capacity Planning and Cloud Cost Engineering
category: devops
categoryTitle: DevOps & Cloud
difficulty: senior
estimatedMinutes: 45
version:
  minimum: "Cloud agnostic"
prerequisites: [devops-observability, architecture-system-design]
tags: [capacity, cloud-cost, autoscaling, performance, operations]
relatedTopics: [devops-aws, devops-azure, architecture-reliability-engineering]
order: 110
status: published
---
# Capacity Planning and Cloud Cost Engineering

## Introduction
Capacity planning connects workload demand to CPU, memory, connections, storage, network, queues, and budget. Cloud cost engineering makes those resources visible and ties spend to business value.

```text
Demand forecast
resource model + bottleneck
capacity/autoscale plan
load test + production metrics
cost/performance decision
```

## Purpose
Scaling one tier does not help if another tier is the bottleneck. An API can add instances while a database connection pool, downstream provider, queue, or network limit remains saturated.

## Simple example
If each worker processes 20 messages/second and peak arrival is 100 messages/second:

```text
workers needed ≈ 100 / 20 = 5
```

Add headroom for failure, variance, and processing-time distribution. Measure queue age, not only worker CPU.

## Professional company-level planning
Track:

- Requests/second and peak multiplier.
- p95/p99 latency/error SLO.
- CPU/memory per request.
- Database connections/queries/locks.
- Queue arrival and service rates.
- Storage growth/retention.
- Data transfer/NAT/CDN cost.
- Cost per customer/order/request.

Autoscaling should use signals related to demand (queue age, concurrency, request rate) and have scale-down protection so it does not oscillate or remove capacity too aggressively.

## Cost decisions

```text
Idle overprovisioning → right-size/scale
Repeated object storage → lifecycle/archive
Large DB query cost → index/projection/read model
NAT/data transfer → architecture review
Unused resources → ownership/cleanup automation
```

Do not optimize cost by reducing reliability below the business requirement. A cheaper system that loses orders is expensive in every other dimension.

## Interview Questions
- **[L1]** What is capacity planning?
- **[L1]** Why can adding API instances fail to solve latency?
- **[L2]** What metrics are useful for autoscaling workers?
- **[L2]** How do you calculate rough worker capacity?
- **[L3]** How do you balance cloud cost, performance, and reliability?
- **[L3]** How would you find the largest cost opportunity in a cloud system?

## Interview Answers
1. It estimates resources needed for expected demand while meeting performance/reliability goals and failure headroom.
2. A database, dependency, connection pool, network, queue, lock, or CPU bottleneck may remain saturated while API capacity increases.
3. Queue age, arrival/processing rate, concurrency, latency, error rate, CPU/memory, and downstream capacity.
4. Estimate demand/service rate, divide demand by per-worker throughput, then add headroom and test against variance/failure.
5. Set reliability/performance SLOs first, measure workload/cost, right-size and autoscale, remove waste, and refuse savings that violate user/data requirements.
6. Attribute cost by service/workload, compare spend to traffic/business output, inspect data transfer/storage/idle capacity, and validate changes with performance and reliability metrics.

## Expert perspective
Cost is an architecture signal. Senior engineers monitor cost per useful business unit, connect it to capacity and SLOs, and optimize the largest measured waste without blindly reducing redundancy or operational safety.
