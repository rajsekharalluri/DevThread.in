---
id: architecture-application-security
slug: application-security
title: Application and API Security
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Platform agnostic"
prerequisites: [dotnet-aspnet-core-backend, architecture-fundamentals]
tags: [security, api-security, owasp, authorization, threat-modeling]
relatedTopics: [dotnet-caching-auth-serialization, devops-observability]
order: 190
status: published
---
# Application and API Security

## Introduction
Application security protects confidentiality, integrity, availability, and accountability across the UI, API, services, data stores, and deployment pipeline.

```text
Request
  ↓ TLS + authentication
Identity
  ↓ authorization/resource ownership
Validation
  ↓ safe business operation
Data access
  ↓ parameterized + audited
Response
  ↓ safe serialization/error handling
```

## Purpose
Security is a system property. A secure UI with an unsafe API is insecure; a protected API with leaked secrets/logs is insecure; a correct service deployed with excessive cloud permissions is insecure.

## Threat-model questions
For each feature ask:

- What assets are protected?
- Who are the users/attackers?
- What trust boundary does data cross?
- What happens if input is malicious?
- What if a token is stolen?
- What if a dependency is compromised?
- What evidence/audit is required?

## Simple API example
```csharp
[Authorize(Policy = "orders:read")]
[HttpGet("{id:guid}")]
public async Task<ActionResult<OrderResponse>> Get(Guid id, CancellationToken ct)
{
    var tenantId = User.GetTenantId();
    var order = await orders.GetForTenantAsync(id, tenantId, ct);
    return order is null ? NotFound() : Ok(order);
}
```
The route does not trust an ID alone; it combines identity, tenant ownership, validation, and a safe DTO response.

## Professional company-level controls

```text
Identity: JWT/OIDC validation, key rotation, MFA/roles/scopes
Authorization: policy + tenant/resource ownership
Input: schema/business validation, size/rate limits
Data: parameterized SQL, least privilege, encryption
Output: DTOs, safe errors, security headers
Operations: secrets manager, audit logs, dependency scanning
```

Do not rely on Angular guards for authorization. Do not return stack traces. Do not log tokens. Do not build SQL/HTML/URLs from untrusted input without the correct escaping/parameterization strategy.

## Comparison
| Control | Protects |
|---|---|
| Authentication | Identity assertion |
| Authorization | Allowed action/resource |
| Validation | Shape/business input |
| Rate limit | Resource exhaustion/abuse |
| Encryption | Data in transit/at rest |
| Audit | Accountability/investigation |
| Secret manager | Credential exposure |

## Interview Questions
- **[L1]** What is the difference between authentication and authorization?
- **[L1]** Why is client-side authorization insufficient?
- **[L2]** What is the difference between input validation and output encoding?
- **[L2]** How do you prevent SQL injection?
- **[L3]** How would you threat-model a multi-tenant API?
- **[L3]** How do you balance security controls with developer usability and operations?

## Interview Answers
1. Authentication verifies identity; authorization decides whether that identity can perform an operation on a resource.
2. Browser code can be modified/bypassed. The backend must enforce authorization for every protected operation.
3. Validation checks whether input is acceptable; encoding makes output safe for a specific context such as HTML/SQL/URL. They solve different problems.
4. Use parameterized queries/ORM parameters, least-privilege database users, safe dynamic identifier allow-lists, and tests that attempt injection patterns.
5. Map tenants/assets/trust boundaries, identify token and data threats, enforce tenant predicates at service/data boundaries, test cross-tenant access, and audit sensitive actions.
6. Use secure defaults, reusable middleware/policies, automation/scanning, least privilege, clear runbooks, and risk-based controls rather than manual friction everywhere.

## Expert perspective
Security architecture is boundary design plus operational discipline. Senior engineers assume every client can be hostile, every dependency can fail, and every secret can leak unless the system limits blast radius and produces useful audit evidence.
