---
id: genai-model-routing-cost
slug: model-routing-cost
title: LLM Model Selection, Routing, and Cost Control
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 50
version:
  minimum: "Framework agnostic"
prerequisites: [genai-transformers, genai-production-evaluation]
tags: [genai, llm, model-selection, routing, cost, latency]
relatedTopics: [genai-production-evaluation, genai-security-guardrails]
order: 110
status: published
---
# LLM Model Selection, Routing, and Cost Control

## Introduction
Model selection is a product/architecture decision involving quality, latency, context, availability, privacy, and cost. The largest model is not automatically the best model for every request.

```text
Request classification
simple/low-risk → fast/cheap model
complex/reasoning → stronger model
sensitive/private → approved provider/local model
failure/limit → fallback policy
```

## Purpose
Routing can reduce cost and latency while preserving quality. It must be driven by evaluation, not by model-name assumptions.

## Simple example
```python
def choose_model(request: Request) -> str:
    if request.contains_sensitive_data:
        return "approved-private-model"
    if request.requires_complex_reasoning:
        return "strong-model"
    return "fast-model"
```

Each route needs the same output/evaluation contract; otherwise a cheap route may silently return unsafe or incomplete results.

## Professional company-level approach
Track per request:

```text
model/provider/version
prompt version
input/output tokens
latency/retries
retrieval/tool calls
quality/safety result
cost estimate
```

Use timeouts, provider fallbacks, circuit breakers, token budgets, caching for safe deterministic work, and rate limits. Do not retry every model error identically; context-limit, validation, auth, and policy errors need different behavior.

## Comparison
| Strategy | Benefit | Risk |
|---|---|
| One model | Simple behavior | Cost/availability rigidity |
| Route by task | Cost/latency optimization | Evaluation/routing complexity |
| Fallback model | Availability | Quality/prompt differences |
| Local/private model | Data/control | Operations/quality/capacity |
| Cache | Lower cost/latency | Stale/wrong response risk |

## Interview Questions
- **[L1]** What factors influence model selection?
- **[L1]** Why is the largest model not always best?
- **[L2]** What should be tracked for each LLM request?
- **[L2]** Why should different model routes share an evaluation contract?
- **[L3]** How would you design model fallback during a provider outage?
- **[L3]** How do you reduce LLM cost without reducing answer trustworthiness?

## Interview Answers
1. Quality, task complexity, latency, context size, availability, privacy, cost, tooling, and operational support.
2. Simple tasks can be served faster/cheaper, while larger models may add latency/cost without measurable quality benefit.
3. Model/provider/version, prompt, tokens, latency, errors, retries, retrieval/tools, quality/safety outcome, and cost.
4. Without a shared contract, routing can change answer shape/quality/security invisibly; evaluate each route against the same task expectations.
5. Use bounded timeout/retry, approved compatible fallback, circuit breaker, clear degradation, budget limits, and observability; never send restricted data to an unapproved fallback.
6. Route simple tasks, budget context/output, cache safe results, use retrieval efficiently, reduce unnecessary agent loops, and keep groundedness/security evaluation gates.

## Expert perspective
LLM routing is capacity and quality engineering. Treat model/provider configuration as a versioned production dependency and make every cost-saving change prove it preserves correctness, security, and user value.
