---
id: genai-production-evaluation
slug: production-evaluation
title: Production LLM Evaluation, Cost, and Observability
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Framework agnostic"
prerequisites: [genai-rag-fundamentals, genai-langchain-agents]
tags: [genai, evaluation, observability, cost, llmops]
relatedTopics: [genai-advanced-rag, devops-observability]
order: 90
status: published
---
# Production LLM Evaluation, Cost, and Observability

## Introduction
An LLM application is probabilistic; traditional pass/fail unit tests are necessary but insufficient. Production quality requires evaluation datasets, groundedness/relevance measures, latency/cost tracking, safety checks, and sampled human review.

```text
Prompt/model/retrieval change
Offline evaluation dataset
quality + safety + cost gate
staged release
production traces/feedback
regression dataset
```

## Purpose
Prevent a prompt/model/retrieval change from silently reducing answer correctness, increasing hallucinations, leaking data, or multiplying cost.

## Evaluation dimensions

- Retrieval recall/precision.
- Answer relevance.
- Groundedness/faithfulness to sources.
- Citation correctness.
- Refusal behavior when evidence is missing.
- Tool-call correctness/safety.
- Latency p50/p95/p99.
- Token cost and cache hit rate.
- Privacy/security violations.

## Professional example
For a support RAG system, keep a labeled dataset:

```text
Question → expected source IDs → acceptable answer facts
```

Run every candidate model/prompt/retriever against it, compare metrics and critical failure cases, then canary the change. Log model/version/prompt/retriever/config IDs with each trace, but redact user secrets and sensitive content.

## Cost/latency model

```text
Total cost ≈ input tokens + output tokens
              + embedding/retrieval/rerank calls
              + tool/API infrastructure
```

Reduce cost with context budgets, model routing, caching safe deterministic steps, batching/streaming where useful, and avoiding unnecessary agent loops. Never optimize cost by removing security/evidence requirements blindly.

## Interview Questions
- **[L1]** Why is an LLM application difficult to test with only unit tests?
- **[L1]** What should be measured in production?
- **[L2]** What is groundedness/faithfulness?
- **[L2]** Why must model/prompt/retriever versions be traced?
- **[L3]** How would you build an evaluation gate for a RAG release?
- **[L3]** How do you reduce LLM cost without degrading trustworthiness?

## Interview Answers
1. Outputs can vary and quality depends on retrieval/context/model behavior; unit tests do not measure factuality, relevance, safety, or cost alone.
2. Quality, retrieval, citations, latency, tokens/cost, tool behavior, errors, refusals, safety events, and user feedback.
3. Whether answer claims are supported by retrieved sources and citations; it is distinct from merely sounding relevant.
4. Without exact versions/configuration, a regression cannot be reproduced or attributed.
5. Use labeled queries/source expectations, automated retrieval/groundedness/safety checks, critical-case review, thresholds, and staged/canary rollout.
6. Budget tokens/context, route simple tasks to cheaper models, cache safe results, reduce unnecessary retrieval/agent steps, and keep evidence/authorization/quality gates intact.

## Expert perspective
LLMOps is experimental software engineering with production consequences. Treat prompts, models, retrieval configuration, and tools as versioned deployable dependencies with measurable quality and cost.
