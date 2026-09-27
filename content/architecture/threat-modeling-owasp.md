---
id: architecture-threat-modeling-owasp
slug: threat-modeling-owasp
title: Threat Modeling and OWASP Application Security
category: architecture
categoryTitle: Architecture & Distributed Systems
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "OWASP concepts"
prerequisites: [architecture-application-security, dotnet-aspnet-core-backend]
tags: [security, threat-modeling, owasp, api-security]
relatedTopics: [devops-cloud-secrets-security, architecture-reliability-engineering]
order: 210
status: published
---
# Threat Modeling and OWASP Application Security

## Introduction
Threat modeling identifies what can go wrong before implementation or deployment. A practical model maps assets, actors, trust boundaries, entry points, threats, controls, and residual risk.

```text
Client → API gateway → service → database
  │          │           │          │
assets   auth boundary  tenant   secrets/data
```

## Purpose
Security becomes cheaper when designed before code. Threat modeling prevents teams from relying on a checklist after deployment and helps prioritize controls based on impact and likelihood.

## Simple example
For an order API:

```text
Asset: order/payment data
Attacker: unauthenticated client or compromised account
Threat: IDOR/cross-tenant access
Control: authenticated tenant scope in every query + authorization policy
Test: request tenant B order using tenant A identity → 404/403
```

## Professional company-level approach
Review common risks:

- Broken access control/IDOR.
- Injection into SQL/HTML/commands.
- Authentication/session/token errors.
- Sensitive data exposure.
- SSRF and unsafe outbound requests.
- Insecure deserialization/file handling.
- Rate/resource exhaustion.
- Vulnerable dependencies and supply chain.

For every high-risk boundary, record the threat, control, owner, test, and monitoring signal. Security controls should be enforced server-side and verified with automated tests.

## Comparison
| Approach | Value |
|---|---|
| Threat model | Finds risks before implementation |
| SAST | Finds code patterns |
| DAST | Tests running application behavior |
| Dependency scan | Finds known vulnerable packages |
| Pen test | Human/adversarial assessment |
| Runtime monitoring | Detects suspicious production behavior |

## Interview Questions
- **[L1]** What is threat modeling?
- **[L1]** What is IDOR/broken object-level authorization?
- **[L2]** Why are input validation and output encoding different?
- **[L2]** How do you protect against SQL injection and SSRF?
- **[L3]** How would you threat-model a multi-tenant API?
- **[L3]** How do you integrate security into CI/CD without making it ineffective noise?

## Interview Answers
1. It systematically identifies assets, actors, boundaries, threats, controls, and residual risk before/during design.
2. It occurs when a user can access another user's object by changing an identifier because resource ownership was not checked server-side.
3. Validation limits acceptable input; encoding makes output safe in its destination context. SQL uses parameters; HTML uses safe encoding/sanitization.
4. Parameterize database values, allow-list dynamic identifiers, validate outbound URLs/hosts, block private metadata ranges, and use network egress controls.
5. Map tenant identity through API/service/data layers, enforce tenant predicates, test cross-tenant IDs, inspect caches/logs/events for leakage, and audit sensitive actions.
6. Automate fast checks (secret/dependency/static scans), gate high-confidence findings, run dynamic/security tests in staging, and assign owners/runbooks instead of ignoring noisy alerts.

## Expert perspective
Security is not a final phase. Threat modeling makes security requirements concrete, and OWASP-style risks become useful only when mapped to specific controls, tests, owners, and observable production signals.
