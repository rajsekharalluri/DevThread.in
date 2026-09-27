---
id: devops-aws
slug: aws
title: AWS Core Services and Cloud Architecture
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "AWS current concepts"
prerequisites: [devops-cicd-fundamentals, devops-iac]
tags: [aws, cloud, ec2, s3, rds, iam, vpc, lambda]
relatedTopics: [devops-azure, devops-iac, devops-observability]
order: 40
status: published
---
# AWS Core Services and Cloud Architecture

## Introduction
AWS provides managed cloud primitives for compute, storage, databases, networking, identity, messaging, and monitoring. The important skill is mapping workload requirements to managed services while controlling security, cost, and failure behavior.

```text
Users
  ↓
Route/CDN/load balancer
  ↓
Compute: EC2 / containers / Lambda
  ├── RDS relational data
  ├── S3 objects
  ├── queues/events
  └── IAM roles + VPC security
```

## Purpose
Cloud removes physical hardware management and enables elastic capacity, managed backups/failover, and global deployment. It does not remove architecture decisions, security responsibility, or cost management.

## Core services

| Service | Role | Main decision |
|---|---|---|
| EC2 | Virtual machine | You manage OS/capacity |
| S3 | Object storage | Durable files/backups/static assets |
| RDS | Managed relational DB | Managed operations, engine constraints |
| Lambda | Event-driven function | Stateless execution/time/cold starts |
| IAM | Identity/policy | Least privilege |
| VPC | Network boundary | Subnets/routes/security groups |
| CloudWatch | Metrics/logs/alarms | Operational visibility |

## Real-World Simple Example
An application stores static uploads in S3, transactional orders in RDS, runs an API on containers/EC2, and gives the API an IAM role allowing only the required S3 bucket actions.

## Professional company-level example
```text
Public traffic
    ↓
Load balancer / CDN
    ↓
Private application subnets
    ├── API instances/containers
    ├── worker instances
    └── managed database in private subnet

IAM role → only required service actions
CloudTrail → audit
CloudWatch → metrics/logs/alarms
Backup/restore → tested recovery
```

Never use a broad policy merely to “make it work”:

```json
{ "Effect": "Allow", "Action": "s3:*", "Resource": "*" }
```

Scope to specific actions/resources and prefer IAM roles/managed identities over long-lived keys.

## Reliability and cost
Choose Availability Zone redundancy for important workloads, test restore rather than assuming backups work, and monitor:

- Instance/database utilization
- Request latency/error rate
- Storage growth
- NAT/data-transfer cost
- S3 lifecycle/retention
- Reserved/spot capacity suitability
- Recovery time/objective

Cloud architecture is a cost model as much as a diagram.

## Interview Questions
- **[L1]** What are EC2, S3, RDS, Lambda, IAM, and VPC used for?
- **[L1]** What does least privilege mean in AWS?
- **[L2]** Why prefer IAM roles over long-lived access keys?
- **[L2]** How does Multi-AZ improve database availability?
- **[L3]** How would you design a secure, highly available AWS application?
- **[L3]** How do you control cloud cost as a system scales?

## Interview Answers
1. EC2 provides virtual machines, S3 object storage, RDS managed relational databases, Lambda event-driven compute, IAM identity/policy, and VPC networking boundaries.
2. Grant each identity only the actions/resources required for its job, not broad account/service access.
3. Roles provide temporary rotated credentials attached to workloads; static keys can leak, require manual rotation, and remain valid until revoked.
4. A standby/replica in another Availability Zone can be promoted during an instance/AZ failure while the stable database endpoint is retained.
5. Use multiple AZs, private subnets, load balancing/autoscaling, least-privilege roles, encryption, backups/restore testing, health checks, observability, and tested failure/rollback procedures.
6. Use budgets/alerts, right-size resources, autoscale, lifecycle storage, monitor data transfer/NAT, remove unused resources, and choose managed/reserved/serverless options from measured workload economics.

## Expert perspective
Cloud services are building blocks, not architecture by themselves. Senior engineers design identity, network paths, failure recovery, data protection, observability, and cost controls together before choosing a service name.
