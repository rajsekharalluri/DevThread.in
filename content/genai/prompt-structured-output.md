---
id: genai-prompt-structured-output
slug: prompt-structured-output
title: Prompt Engineering and Structured LLM Outputs
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Framework agnostic"
prerequisites: [genai-transformers, genai-production-evaluation]
tags: [genai, prompts, structured-output, json-schema, tool-calling]
relatedTopics: [genai-langchain-agents, genai-security-guardrails]
order: 120
status: published
---
# Prompt Engineering and Structured LLM Outputs

## Introduction
Prompt engineering designs instructions, context, examples, constraints, and output contracts so a model can perform a task consistently. Structured output constrains/validates the response so application code does not parse arbitrary prose.

```text
Task + role + trusted context + constraints
                  ↓
              LLM response
                  ↓ schema validation
            application behavior
```

## Purpose
Good prompts improve clarity, but prompts are not authorization or deterministic business logic. Structured output reduces integration failures and makes invalid model behavior observable.

## Simple example
```python
class Classification(BaseModel):
    category: Literal["billing", "technical", "other"]
    urgency: Literal["low", "high"]
    reason: str

result = model.with_structured_output(Classification).invoke(
    "Classify this customer message: ..."
)
```

Validate the result and handle refusal/invalid/provider errors; do not assume a library setting guarantees a valid business decision.

## Professional company-level prompt
A maintainable prompt separates stable instructions from dynamic data:

```text
Role: classify support request.
Rules: choose exactly one category; do not invent customer/account facts.
Output schema: category, urgency, reason, evidence.
Trusted context: <retrieved documents>
User content: <quoted/untrusted message>
```

Version prompts, test representative/adversarial examples, redact sensitive data, and include an instruction that retrieved/user content is data—not higher-priority instructions.

## Tool calling and output safety
A model-generated SQL query, HTML, shell command, or tool argument is untrusted. Use schema validation, allow-lists, authorization, parameterization, sandboxing, and human approval for high-impact operations. Structured JSON is not safe by itself; it still contains model-generated values.

## Comparison
| Output | Strength | Risk |
|---|---|
| Free prose | Flexible/human-friendly | Hard to integrate/validate |
| JSON schema | Machine-readable contract | Schema/refusal handling |
| Tool call | Actionable structured intent | Real side effects/permissions |
| Function/code generation | Flexible automation | Injection/execution risk |

## Interview Questions
- **[L1]** What is prompt engineering?
- **[L1]** Why use structured outputs?
- **[L2]** Why is JSON schema validation not the same as authorization?
- **[L2]** How should trusted instructions and untrusted retrieved content be separated?
- **[L3]** How do you test prompt changes safely?
- **[L3]** How would you secure an LLM-generated tool call?

## Interview Answers
1. It designs task instructions/context/examples/constraints to make model behavior clearer and more consistent.
2. It gives application code a predictable shape that can be parsed/validated instead of relying on prose parsing.
3. A valid shape can still request a forbidden action or contain false values; authorization/business rules must be enforced outside the model.
4. Keep system/application instructions separate, quote/label user/retrieved content as untrusted data, and authorize tools independently.
5. Version prompts, run labeled regression/adversarial datasets, compare quality/safety/cost/latency, review critical cases, and canary changes.
6. Validate schema and argument limits, authorize user/resource, parameterize downstream calls, use idempotency/approval, audit, and reject unsupported actions.

## Expert perspective
Prompt quality is useful, but reliable AI applications come from contracts around the model: schemas, validators, retrieval boundaries, authorization, tests, budgets, and safe failure behavior.
