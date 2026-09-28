---
id: genai-security-guardrails
slug: security-guardrails
title: Generative AI Security and Guardrails
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Framework agnostic"
prerequisites: [genai-langchain-agents, genai-mcp, architecture-application-security]
tags: [genai, security, prompt-injection, guardrails, privacy]
relatedTopics: [genai-production-evaluation, genai-rag-fundamentals]
order: 100
status: published
---
# Generative AI Security and Guardrails

## Introduction
LLM security must protect data, tools, users, and downstream systems from untrusted prompts, retrieved documents, model output, and automated actions.

```text
Untrusted user/document
LLM reasoning (not a security boundary)
Tool/API request
Server-side auth + validation + policy
Limited side effect
```

## Purpose
Prompt instructions can be manipulated. Retrieved documents can contain indirect prompt injection. Tool calls can cause real financial/data effects. Guardrails must exist outside the model prompt.

## Common threats

- Direct prompt injection.
- Indirect injection in documents/web pages.
- Sensitive data leakage in context/output/logs.
- Tool poisoning or overbroad tool descriptions.
- Excessive agent permissions/loops.
- Insecure output used in SQL/HTML/commands.
- Cross-tenant retrieval.
- Model/provider data-retention risk.

## Professional controls

- Authenticate/authorize every tool server-side.
- Filter retrieval by tenant/access before context construction.
- Validate structured model output against schemas.
- Use allow-lists for tool arguments/URLs/actions.
- Require human approval for irreversible/high-value actions.
- Bound token/tool-call/time budgets.
- Redact secrets and sensitive data before model/logging.
- Treat generated text as untrusted output.
- Audit model/tool decisions with safe correlation IDs.

```python
if tool.name == "issue_refund":
    authorize(user, order_id)
    validate_amount(amount)
    require_approval_if(amount > AUTO_LIMIT)
    use_idempotency_key(request_id)
```

## Comparison
| Control | Boundary |
|---|---|
| Prompt instruction | Behavioral hint, not security |
| Output schema | Shape validation |
| Tool authorization | Real permission boundary |
| Retrieval filter | Data exposure boundary |
| Human approval | High-impact decision boundary |
| Rate/budget limit | Resource/exhaustion boundary |

## Interview Questions
- **[L1]** What is prompt injection?
- **[L1]** Why is a system prompt not a security boundary?
- **[L2]** What is indirect prompt injection in RAG?
- **[L2]** How should tool calls be secured?
- **[L3]** How would you design guardrails for an agent that can issue refunds?
- **[L3]** How do you prevent cross-tenant data leakage in RAG?

## Interview Answers
1. An input attempts to manipulate model instructions/reasoning into ignoring intended behavior or performing an unsafe action.
2. The model may follow conflicting/untrusted text; users can influence input and the model cannot enforce infrastructure permissions.
3. A malicious instruction inside a retrieved document tries to influence the model as if it were trusted system guidance.
4. Authenticate client, authorize operation/resource, validate schema/limits, use idempotency, audit, rate-limit, and require approval for dangerous actions.
5. Enforce user/tenant authorization in the tool service, cap amounts, require confirmation/approval, use provider idempotency, and audit every attempt.
6. Store access metadata, filter during retrieval, never mix unrestricted candidates, recheck before context/output, and test adversarial cross-tenant queries.

## Expert perspective
The model may decide what it wants to do, but it must never decide what it is allowed to do. Permissions, validation, tenant isolation, rate limits, and approval belong in trusted application services outside the model.
