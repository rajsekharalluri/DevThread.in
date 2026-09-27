---
id: devops-iac
slug: iac
title: Infrastructure as Code
category: devops
categoryTitle: DevOps & Cloud
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Terraform/Bicep concepts"
prerequisites: [devops-cicd-fundamentals, devops-aws]
tags: [iac, terraform, bicep, cloudformation, drift]
relatedTopics: [devops-source-control-migrations, devops-cicd-fundamentals]
order: 70
status: published
---
# Infrastructure as Code

## Introduction
Infrastructure as Code (IaC) defines cloud/network/compute/database/permission resources in version-controlled files and applies them through a repeatable tool rather than manual console actions.

```text
Versioned config
      ↓ plan/what-if
Reviewed change
      ↓ apply
Cloud resources
      ↓ state/drift detection
Observed/reconciled infrastructure
```

## Purpose
IaC makes infrastructure reviewable, reproducible, auditable, and easier to recreate. It also makes dangerous changes easier to automate, so state security, plan review, permissions, and rollback are essential.

## Simple example
```hcl
resource "aws_s3_bucket" "uploads" {
  bucket = "academy-uploads"
}

resource "aws_s3_bucket_versioning" "uploads" {
  bucket = aws_s3_bucket.uploads.id
  versioning_configuration { status = "Enabled" }
}
```

```bash
terraform plan
terraform apply
```

`plan` shows the proposed create/update/destroy operations; `apply` performs them. Never skip plan review for production.

## Professional company-level approach
Separate reusable modules and state ownership by environment/team/service. Store state in a protected, encrypted, locked remote backend. Use CI to generate a plan artifact and require approval before applying it:

```text
Pull request
  ↓ validate/format/security checks
Plan artifact
  ↓ review/approval
Apply exact reviewed plan
  ↓
Drift/outputs/monitoring
```

Use expand-and-contract for infrastructure changes that affect live traffic. Keep credentials in a secret provider and give deployment identities only the permissions needed for that environment.

## Failure scenarios
- Someone manually changes a managed resource and creates drift.
- Two applies run concurrently and corrupt/overwrite state.
- A plan destroys a production database because a resource address changed.
- State contains secrets and is stored in a public/local repository.
- One giant state file gives every team a large blast radius.

## Comparison
| Approach | Benefit | Risk |
|---|---|---|
| Manual console | Fast experiment | Unrepeatable/poor audit |
| IaC declarative | Reviewable/repeatable | State/plan complexity |
| Scripted imperative | Flexible actions | Harder drift/reconciliation |
| Module | Reuse/standardization | Hidden assumptions/versioning |

## Interview Questions
- **[L1]** What problem does IaC solve?
- **[L1]** What does `terraform plan` do?
- **[L2]** What is infrastructure drift?
- **[L2]** Why does IaC state need remote storage and locking?
- **[L3]** How would you structure IaC for many teams/environments?
- **[L3]** How would you protect production from destructive plans?

## Interview Answers
1. It makes infrastructure versioned, reviewable, repeatable, and auditable instead of dependent on manual clicks.
2. It compares declared/current state and shows intended resource changes without applying them.
3. Drift means real infrastructure differs from the managed declaration/state, often due to manual changes; it can cause unexpected correction or destruction.
4. Shared remote state gives teams one source of coordination; locking prevents concurrent applies from corrupting state or making conflicting changes.
5. Split state/modules by ownership/environment, use reusable versioned modules, scoped identities, plan approvals, and independent blast radii.
6. Require plan review/policy checks, protect state, use approval gates, backups/restore, deletion protection, and separate production credentials.

## Expert perspective
IaC is production code for infrastructure. The dangerous operation is not writing a resource block; it is applying an unreviewed plan against state you do not fully understand.
