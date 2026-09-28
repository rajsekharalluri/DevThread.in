---
id: projects-file-storage-platform
slug: file-storage-platform
title: "Build: Multi-Tenant File Storage Platform"
category: projects
categoryTitle: Build Real Systems
difficulty: architect
estimatedMinutes: 80
version:
  minimum: "Cloud architecture exercise"
prerequisites: [devops-aws, architecture-application-security, architecture-system-design]
tags: [project, object-storage, uploads, security, multi-tenant]
relatedTopics: [devops-aws, architecture-application-security, devops-cloud-secrets-security]
order: 50
status: published
---
# Build: Multi-Tenant File Storage Platform

## Introduction
Design a service for secure tenant-scoped file upload/download, metadata, sharing, virus scanning, retention, and audit.

```text
Client → API authorize metadata
          ↓ signed upload URL
       Object storage
          ↓ event
       Scan/metadata worker
       Available/quarantined file
```

## Purpose
Practice separating large binary transfer from API compute, object-storage security, asynchronous processing, tenant isolation, and lifecycle policies.

## Professional design
The API should issue a short-lived signed upload URL after validating tenant quota/type/size. The browser uploads directly to object storage; an event starts scanning and metadata processing. Downloads use an authorization check and short-lived signed URL.

```text
Tenant → API → authorization/quota
              ↓ signed URL
Browser ─────→ private bucket
                    ↓ event
              scanner/worker
                    ├── available
                    └── quarantined
```

## Important decisions

- Never make the bucket public by default.
- Store tenant/object ownership metadata separately and enforce it.
- Validate content type and inspect actual bytes.
- Virus/malware scan before making files available.
- Encrypt objects and protect keys/secrets.
- Use lifecycle/archive/retention policies.
- Audit upload/download/share/delete.
- Handle incomplete multipart uploads and duplicate events.

## Interview Questions
- **[L1]** Why upload directly to object storage instead of through the API?
- **[L1]** What is a signed URL?
- **[L2]** How do you prevent cross-tenant file access?
- **[L2]** Why is client-provided content type insufficient?
- **[L3]** How would you design malware scanning and quarantine?
- **[L3]** How do you meet retention/deletion requirements while preserving audit evidence?

## Interview Answers
1. Large files do not consume API bandwidth/threads/storage; the API controls authorization and issues a limited upload permission.
2. A short-lived scoped URL grants temporary access to a specific object/action without exposing permanent storage credentials.
3. Check tenant/object ownership in API authorization, scope keys/metadata, use non-guessable object IDs, and test cross-tenant access.
4. It can be falsified and does not prove file content; inspect bytes, size, signature/extension, and scan before availability.
5. Store quarantine state, scan asynchronously, deny downloads until clean, make scan events idempotent, and provide operator retry/review.
6. Apply retention/delete workflows to object and metadata stores, keep required immutable audit records without retaining prohibited content, and document legal policy.

## Expert Perspective
File storage is an authorization and lifecycle problem, not merely an upload endpoint. The design must secure object access, handle untrusted bytes, isolate tenants, and make asynchronous scanning/recovery observable.
