---
id: devops-cloud-secrets-security
slug: cloud-secrets-security
title: Cloud Identity, Secrets, and Supply-Chain Security
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Cloud agnostic"
prerequisites: [devops-aws, devops-azure, devops-iac]
tags: [security, secrets, iam, key-vault, supply-chain, cloud]
relatedTopics: [architecture-application-security, devops-observability]
order: 100
status: published
---
# Cloud Identity, Secrets, and Supply-Chain Security

## Introduction
Cloud security is primarily identity and blast-radius management. A workload needs permission to perform specific actions on specific resources; credentials, images, dependencies, and pipelines are part of the software supply chain.

```text
Developer/CI identity
        ↓ scoped role
Cloud resource/workload identity
        ↓ short-lived credentials
Database/storage/queue
        ↓ audit logs + policy checks
```

## Purpose
Prevent credential leaks, excessive permissions, compromised dependencies, public storage exposure, and deployment identities that can change an entire environment.

## Simple example
Bad:

```json
{ "Effect": "Allow", "Action": "s3:*", "Resource": "*" }
```

Better:

```json
{
  "Effect": "Allow",
  "Action": ["s3:GetObject"],
  "Resource": "arn:aws:s3:::academy-uploads/*"
}
```

## Professional company-level controls

- Use workload identities/managed identities instead of long-lived access keys.
- Store secrets in AWS Secrets Manager, Azure Key Vault, or an equivalent.
- Scope CI identities per environment/resource group.
- Scan dependencies, containers, IaC, and source for secrets/vulnerabilities.
- Protect artifact registries and sign/verify important images.
- Encrypt data in transit and at rest.
- Log IAM/policy/deployment activity centrally.
- Rotate/revoke credentials and test recovery.

A Kubernetes Secret object alone is not a complete secret-management solution; control RBAC, encryption, external secret integration, and who can read it.

## Failure scenarios
- A key committed to Git remains in history after deleting the current file.
- A CI job can modify production because it was granted subscription/account Owner.
- A public S3 bucket exposes user files.
- A base container image contains an unpatched vulnerability.
- Logs print an access token during an error.

## Comparison
| Credential approach | Strength | Risk |
|---|---|---|
| Workload identity | Short-lived/provider-managed | Requires correct role setup |
| Static access key | Simple integration | Leak/rotation risk |
| Secret manager | Centralized rotation/audit | Runtime dependency |
| Plain config | Easy to read | High exposure risk |

## Interview Questions
- **[L1]** What does least privilege mean?
- **[L1]** Why should secrets not be stored in source control?
- **[L2]** Why are workload identities safer than long-lived keys?
- **[L2]** What is supply-chain security for containers/dependencies?
- **[L3]** How would you limit the blast radius of a compromised CI pipeline?
- **[L3]** How would you respond after discovering a secret in Git history?

## Interview Answers
1. Give an identity only the actions/resources required for its job, with no broad wildcard access by default.
2. Git history, forks, logs, caches, and artifacts can retain secrets long after the current file is removed.
3. They use temporary/rotated credentials attached to a workload, reducing exposure duration and manual secret distribution.
4. It protects the code/dependency/image/build path from tampering and known vulnerabilities through pinning, scanning, signing, review, and provenance.
5. Separate accounts/subscriptions/environments, scope roles, protect production approvals, use short-lived credentials, require trusted runners, and audit every deployment.
6. Revoke/rotate immediately, identify access/use, remove from history where appropriate, audit logs, scan repositories/artifacts, and document the incident; deleting the file alone is not enough.

## Expert perspective
Security controls are most effective when they reduce blast radius automatically. “The secret is private” or “the network is internal” is not a security design; identity, authorization, rotation, auditing, and recovery are.
