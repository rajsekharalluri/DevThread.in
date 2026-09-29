---
id: genai-eng-llmops-production
slug: llmops-production
title: "Module 14: LLMOps & Production AI"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 100
version:
  minimum: "vLLM 0.6+, Python 3.11+, any cloud or on-prem GPU environment"
prerequisites: [genai-eng-ai-engineering]
tags: [genai-engineer, llmops, deployment, inference-serving, scaling, cost-optimization, vllm, model-registry]
relatedTopics: [genai-eng-cloud-native-ai]
order: 14
status: published
---
# Module 14: LLMOps & Production AI

## Introduction

**LLMOps** is the set of practices, tools, and processes for deploying, operating, scaling, and continuously improving LLM-powered systems in production. It extends MLOps with concerns specific to LLMs: prompt versioning, API-based and self-hosted inference, token economics, GPU capacity planning, rapid model churn, and continuous evaluation.

This module covers the LLMOps lifecycle, managing artifacts (prompts, models, datasets, indexes), deployment strategies, self-hosted inference serving with vLLM, performance optimization (batching, quantization, caching, speculative decoding), scaling and capacity planning, cost engineering, reliability patterns, and continuous improvement loops.

---

## Part 1: MLOps vs LLMOps

| Concern | Classic MLOps | LLMOps |
|---|---|---|
| Primary artifact | Trained model | Prompts + model choice + retrieval config + tools (+ optional fine-tunes) |
| Training | Frequent retraining on your data | Mostly foundation models; occasional fine-tuning |
| Changes | Model retrain cycle | Prompt edits daily, model swaps monthly, provider updates anytime |
| Evaluation | Accuracy/F1 on test set | Graded quality, LLM judges, human feedback, safety |
| Inference | Small models, CPU/GPU | Large models, expensive GPUs or per-token APIs |
| Cost driver | Training compute | Inference tokens and GPU hours |
| Latency | Milliseconds | Seconds; TTFT and tokens/second matter |
| Risks | Drift, bias | Hallucination, injection, data leakage, cost explosions |

### The LLMOps Lifecycle

```text
1. Design: use case, success metrics, risk assessment, model selection
2. Develop: prompts, RAG pipeline, tools, guardrails; experiment tracking
3. Evaluate: offline eval suites, red teaming, cost/latency benchmarks
4. Deploy: versioned release, canary/shadow, feature flags
5. Operate: monitoring, tracing, alerting, incident response, capacity management
6. Improve: collect feedback and failures, update eval sets, iterate on prompts/retrieval/models
```

---

## Part 2: Managing LLM Artifacts

Everything that influences behavior must be **versioned** and **traceable**.

| Artifact | Versioning Approach |
|---|---|
| **Prompts** | Git (files) or a prompt registry (Langfuse, LangSmith, MLflow prompt registry); semantic versions |
| **Model identifiers** | Pin exact versions (`gpt-4o-2024-08-06`, not floating aliases) in config |
| **Fine-tuned models / adapters** | Model registry (MLflow, Hugging Face Hub private, cloud registries) with lineage |
| **Datasets** (training, eval) | DVC, lakeFS, versioned object storage, dataset registries |
| **Embedding model + index** | Record embedding model/version per index; blue-green indexes for migrations |
| **Configuration** | Temperature, max_tokens, top-k, thresholds, tool lists in versioned config |
| **Guardrail policies** | Versioned alongside prompts |

### A Release Manifest

Bundle everything that defines a release so it can be reproduced and rolled back as a unit.

```yaml
# releases/support-assistant/2025-03-14.yaml
release: support-assistant@3.8.0
prompts:
  system: prompts/support/system.v12.md
  query_rewrite: prompts/support/rewrite.v4.md
models:
  generation: gpt-4o-mini-2024-07-18
  fallback: claude-3-5-haiku-20241022
  embedding: text-embedding-3-small
retrieval:
  index: kb-index-2025-03-10
  top_k: 30
  rerank_model: bge-reranker-v2-m3
  rerank_top_n: 6
  min_rerank_score: 0.2
generation_params:
  temperature: 0
  max_tokens: 700
guardrails: policies/support-guardrails.v5.yaml
evaluation:
  dataset: evals/support@2025-03-01
  pass_rate: 0.91
  security_pass_rate: 1.0
  p95_latency_s: 3.4
  cost_per_1k_requests_usd: 1.85
```

### Experiment Tracking

```python
# pip install mlflow
import mlflow

mlflow.set_experiment("support-assistant-prompts")

with mlflow.start_run(run_name="system-v12-temp0"):
    mlflow.log_params({"prompt_version": "v12", "model": "gpt-4o-mini-2024-07-18", "top_k": 30, "temperature": 0})
    results = run_eval(eval_cases)  # from Module 13
    mlflow.log_metrics({
        "pass_rate": sum(r["passed"] for r in results) / len(results),
        "p95_latency": sorted(r["latency"] for r in results)[int(0.95 * (len(results) - 1))],
    })
    mlflow.log_text(open("prompts/support/system.v12.md").read(), "system_prompt.md")
```

---

## Part 3: Deployment Options

### API-Based vs Self-Hosted

| | Provider APIs (OpenAI, Anthropic, Gemini, Azure OpenAI, Bedrock, Vertex) | Self-Hosted Open Models (Llama, Mistral, Qwen, Gemma, DeepSeek) |
|---|---|---|
| Time to production | Hours | Weeks |
| Quality ceiling | Frontier models | Strong and improving; below frontier for hardest tasks |
| Cost model | Per token; scales linearly with usage | GPU hours; cheaper at high sustained volume |
| Data control | Depends on agreements/region | Full control, on-prem possible |
| Customization | Limited fine-tuning | Full fine-tuning, quantization, custom decoding |
| Operations | Provider handles | You handle GPUs, scaling, upgrades |
| Rate limits | Provider quotas | Your capacity |

### Break-Even Thinking

```python
def monthly_api_cost(requests_per_day: int, in_tokens: int, out_tokens: int, in_price_per_m: float, out_price_per_m: float) -> float:
    per_request = in_tokens / 1e6 * in_price_per_m + out_tokens / 1e6 * out_price_per_m
    return per_request * requests_per_day * 30

def monthly_gpu_cost(gpus: int, hourly_rate: float, utilization_hours: float = 24 * 30) -> float:
    return gpus * hourly_rate * utilization_hours

# Illustrative numbers only - check current pricing
api = monthly_api_cost(requests_per_day=200_000, in_tokens=1500, out_tokens=300, in_price_per_m=0.15, out_price_per_m=0.60)
gpu = monthly_gpu_cost(gpus=2, hourly_rate=2.5)
print(f"API: ${api:,.0f}/month   Self-hosted (2 GPUs, 24x7): ${gpu:,.0f}/month")
```

Include engineering time, on-call, idle capacity, redundancy, and quality differences in the real comparison. Many organizations use **both**: APIs for frontier-quality tasks and self-hosted small models for high-volume narrow tasks.

### Deployment Strategies

| Strategy | Description | Use |
|---|---|---|
| **Shadow** | New version receives a copy of traffic; outputs logged, not shown | Compare quality/latency safely |
| **Canary** | Small % of users get the new version | Detect regressions with limited blast radius |
| **A/B test** | Split traffic, compare business metrics | Choose between variants |
| **Blue-green** | Two full environments, switch traffic | Fast rollback for infra changes (e.g. new index) |
| **Feature flags** | Toggle prompts/models per tenant/user at runtime | Gradual rollout, instant rollback |

```python
import hashlib

def pick_variant(user_id: str, canary_percent: int = 10) -> str:
    bucket = int(hashlib.sha256(user_id.encode()).hexdigest(), 16) % 100
    return "support-assistant@3.9.0" if bucket < canary_percent else "support-assistant@3.8.0"
```

Deterministic bucketing keeps each user on the same variant for a consistent experience.

---

## Part 4: Self-Hosted Inference Serving

### Why Naive Serving Is Slow

Running `model.generate()` in a web server handles one request at a time and wastes GPU capacity. LLM inference has two phases:

| Phase | What Happens | Bottleneck |
|---|---|---|
| **Prefill** | Process the entire prompt in parallel, build the KV cache | Compute-bound |
| **Decode** | Generate tokens one at a time, reading the KV cache | Memory-bandwidth-bound |

Decode underutilizes GPU compute, so serving engines **batch many requests together**.

### Inference Engines

| Engine | Highlights |
|---|---|
| **vLLM** | PagedAttention, continuous batching, prefix caching, multi-LoRA, OpenAI-compatible server |
| **SGLang** | Fast structured generation, RadixAttention prefix caching |
| **TensorRT-LLM / NVIDIA NIM** | Highly optimized NVIDIA kernels, enterprise containers |
| **Text Generation Inference (TGI)** | Hugging Face serving stack |
| **llama.cpp / Ollama** | CPU/edge, GGUF quantization, local dev |

### Key Serving Optimizations

| Technique | What It Does |
|---|---|
| **Continuous batching** | New requests join the running batch at each decode step instead of waiting for a whole batch to finish; massively improves throughput |
| **PagedAttention** | Manages KV cache in fixed-size blocks like OS virtual memory; minimizes fragmentation so more concurrent requests fit |
| **Prefix caching** | Reuses KV cache for shared prompt prefixes (system prompts, few-shot examples, documents) |
| **Quantization** | FP8/INT8/INT4 (AWQ, GPTQ) weights reduce memory and increase speed |
| **Speculative decoding** | A small draft model proposes several tokens; the large model verifies them in one pass |
| **Tensor parallelism** | Split a large model across multiple GPUs |
| **Chunked prefill** | Splits long prompts so they do not stall decoding for other users |

### Serving with vLLM

```bash
pip install vllm

# OpenAI-compatible server
vllm serve Qwen/Qwen2.5-7B-Instruct \
  --max-model-len 16384 \
  --gpu-memory-utilization 0.90 \
  --enable-prefix-caching \
  --max-num-seqs 128 \
  --port 8000

# Larger model split across 4 GPUs with FP8 quantization
vllm serve meta-llama/Llama-3.1-70B-Instruct --tensor-parallel-size 4 --quantization fp8
```

```python
from openai import OpenAI

local = OpenAI(base_url="http://localhost:8000/v1", api_key="not-needed")
r = local.chat.completions.create(
    model="Qwen/Qwen2.5-7B-Instruct",
    messages=[{"role": "user", "content": "Summarize the benefits of continuous batching in 2 sentences."}],
    max_tokens=120,
)
print(r.choices[0].message.content)
```

Because the API is OpenAI-compatible, your application code (Module 4) works unchanged - only `base_url` differs.

### Offline Batch Inference

```python
from vllm import LLM, SamplingParams

llm = LLM(model="Qwen/Qwen2.5-7B-Instruct", max_model_len=8192)
params = SamplingParams(temperature=0, max_tokens=64)
prompts = [f"Classify the sentiment of this review as positive/negative/neutral: {r}" for r in reviews]
outputs = llm.generate(prompts, params)   # vLLM batches internally for maximum throughput
labels = [o.outputs[0].text.strip() for o in outputs]
```

---

## Part 5: Performance Metrics and Capacity Planning

### Key Metrics

| Metric | Definition | Target Example |
|---|---|---|
| **TTFT** | Time to first token | < 500 ms for chat |
| **TPOT / ITL** | Time per output token / inter-token latency | < 50 ms (20+ tokens/s per user) |
| **End-to-end latency** | TTFT + TPOT x output tokens | Depends on output length |
| **Throughput** | Total tokens/second (or requests/second) across all users | Maximize at acceptable latency |
| **Concurrency** | Simultaneous in-flight requests | Sized to peak traffic |
| **GPU utilization / KV cache usage** | Resource saturation | 70-90% at peak |

**The core trade-off:** larger batches increase throughput (cheaper per token) but increase per-user latency. Tune `max-num-seqs` and batch settings against your latency SLO.

### GPU Memory Estimation

```python
def estimate_gpu_memory_gb(params_billion: float, bytes_per_param: float, layers: int, kv_heads: int,
                           head_dim: int, context_tokens: int, concurrent_seqs: int, kv_bytes: int = 2) -> dict:
    weights = params_billion * 1e9 * bytes_per_param / 1e9
    kv_per_token = 2 * layers * kv_heads * head_dim * kv_bytes           # K and V
    kv_total = kv_per_token * context_tokens * concurrent_seqs / 1e9
    overhead = 0.1 * (weights + kv_total)
    return {"weights_gb": round(weights, 1), "kv_cache_gb": round(kv_total, 1), "total_gb": round(weights + kv_total + overhead, 1)}

# Llama-3.1-8B-like: 32 layers, 8 KV heads (GQA), head_dim 128, BF16 weights
print(estimate_gpu_memory_gb(8, 2, 32, 8, 128, context_tokens=8192, concurrent_seqs=32))
# weights ~16 GB, KV cache ~34 GB -> needs an 80 GB GPU (or fewer concurrent seqs / shorter context / FP8 KV cache)
```

### Load Testing

Benchmark with realistic prompt/output length distributions, not a single fixed prompt.

```bash
# vLLM ships a serving benchmark; tools like locust, k6, or LLM-specific load generators also work
vllm bench serve --model Qwen/Qwen2.5-7B-Instruct --dataset-name random \
  --random-input-len 1500 --random-output-len 300 --num-prompts 500 --request-rate 8
```

Find the **maximum request rate** that meets your TTFT/TPOT SLOs per GPU; that number drives how many replicas you need at peak.

---

## Part 6: Cost Engineering

### Where LLM Cost Comes From

```text
Cost per request = input_tokens x input_price + output_tokens x output_price (+ embeddings, reranking, tools, GPUs)
```

Output tokens are typically several times more expensive than input tokens, and agents multiply calls.

### Cost Optimization Levers

| Lever | Typical Impact |
|---|---|
| **Model routing** - small model for easy tasks, large for hard | Large savings |
| **Prompt compression** - shorter system prompts, fewer few-shot examples | 10-40% input reduction |
| **RAG context budgets** - fewer, better chunks via reranking | Large input reduction |
| **Output limits** - `max_tokens`, concise instructions, structured outputs | Output reduction |
| **Provider prompt caching** - static prefix first | Discounted cached input tokens |
| **Response caching** - exact and semantic | Eliminates repeated calls |
| **Batch APIs** for offline work | Often ~50% discount |
| **Distillation / fine-tuned small models** | Very large savings at volume |
| **Self-hosting** at high sustained volume | Depends on utilization |
| **Agent budgets** - step and token caps | Prevents runaway costs |

### Cost Attribution

```python
PRICES = {  # USD per 1M tokens - illustrative, load from config
    "gpt-4o": (2.50, 10.00),
    "gpt-4o-mini": (0.15, 0.60),
}

def record_usage(tenant_id: str, feature: str, model: str, input_tokens: int, output_tokens: int, metrics_client):
    in_price, out_price = PRICES[model]
    cost = input_tokens / 1e6 * in_price + output_tokens / 1e6 * out_price
    metrics_client.increment("llm.cost_usd", cost, tags={"tenant": tenant_id, "feature": feature, "model": model})
    metrics_client.increment("llm.tokens", input_tokens + output_tokens, tags={"tenant": tenant_id, "model": model})
    return cost
```

Track **cost per feature, per tenant, per successful task** (not just per request). Set budgets and alerts; enforce per-tenant quotas.

### Model Routing Example

```python
from pydantic import BaseModel
from typing import Literal

class Complexity(BaseModel):
    level: Literal["simple", "complex"]

def route_model(question: str) -> str:
    if len(question) < 200 and not any(w in question.lower() for w in ["compare", "why", "design", "analyze"]):
        return "gpt-4o-mini"  # cheap heuristic first
    c = client.beta.chat.completions.parse(
        model="gpt-4o-mini", temperature=0,
        messages=[{"role": "user", "content": f"Is answering this simple (lookup/short) or complex (reasoning/multi-step)?\n{question}"}],
        response_format=Complexity,
    ).choices[0].message.parsed
    return "gpt-4o" if c.level == "complex" else "gpt-4o-mini"
```

Validate routing on your eval set: quality must not drop for the traffic routed to the cheaper model.

---

## Part 7: Reliability Engineering for LLM Systems

### Failure Modes

| Failure | Mitigation |
|---|---|
| Provider outage / 5xx | Retries with backoff, multi-provider fallback, circuit breakers |
| Rate limiting (429) | Client-side rate limiting, quota management, queueing, provisioned throughput |
| Latency spikes | Timeouts, streaming, fallback to faster model, hedged requests for critical paths |
| Model deprecation | Track provider deprecation schedules; test replacements early |
| Silent quality changes | Pinned versions, continuous online evaluation |
| GPU node failure (self-hosted) | Multiple replicas across zones, health checks, autoscaling |
| Vector DB outage | Replicas; degrade gracefully (answer without RAG with a disclaimer, or show search results) |

### Circuit Breaker

```python
import time

class CircuitBreaker:
    def __init__(self, failure_threshold: int = 5, reset_after_s: float = 30):
        self.failures, self.threshold, self.reset_after = 0, failure_threshold, reset_after_s
        self.opened_at: float | None = None

    def allow(self) -> bool:
        if self.opened_at is None:
            return True
        if time.time() - self.opened_at > self.reset_after:
            self.opened_at, self.failures = None, 0   # half-open: try again
            return True
        return False

    def record(self, success: bool):
        if success:
            self.failures = 0
        else:
            self.failures += 1
            if self.failures >= self.threshold:
                self.opened_at = time.time()

breakers = {"openai": CircuitBreaker(), "anthropic": CircuitBreaker()}

def resilient_complete(messages: list[dict]) -> str:
    for provider, call in [("openai", call_openai), ("anthropic", call_anthropic)]:
        if not breakers[provider].allow():
            continue
        try:
            result = call(messages)
            breakers[provider].record(True)
            return result
        except Exception:
            breakers[provider].record(False)
    return "The assistant is temporarily unavailable. Please try again shortly."
```

### SLOs for AI Features

Define SLOs across multiple dimensions:
- **Availability:** 99.9% of requests receive a non-error response
- **Latency:** p95 TTFT < 1.5 s, p95 end-to-end < 8 s
- **Quality:** online judge pass rate >= 90%, thumbs-down rate < 5%
- **Cost:** average cost per conversation < $0.02

### AI Gateways

An **AI gateway** centralizes cross-cutting concerns for all LLM traffic in an organization: unified API across providers, key management, per-team quotas, rate limiting, caching, fallbacks, logging, PII redaction, and cost reporting. Examples: LiteLLM proxy, Kong/Envoy AI gateways, cloud AI gateways (Azure API Management, Cloudflare AI Gateway).

---

## Part 8: Monitoring and Drift

| Drift Type | Example | Detection |
|---|---|---|
| **Input drift** | Users start asking about a new product | Topic clustering of queries over time, embedding distribution shifts |
| **Knowledge drift** | Docs outdated, index stale | Ingestion lag metrics, "not found" rate |
| **Model drift** | Provider updates model behavior | Online eval scores, output length/format changes |
| **Performance drift** | Latency or cost creeping up | Dashboards with baselines and alerts |

```python
# Weekly: cluster new queries to discover emerging topics not covered by the eval set
from sklearn.cluster import KMeans

def emerging_topics(query_texts: list[str], n_clusters: int = 20) -> dict[int, list[str]]:
    vecs = embedder.encode(query_texts, normalize_embeddings=True)
    labels = KMeans(n_clusters=n_clusters, n_init="auto", random_state=0).fit_predict(vecs)
    clusters: dict[int, list[str]] = {}
    for text, label in zip(query_texts, labels):
        clusters.setdefault(int(label), []).append(text)
    return clusters   # review samples from each cluster; add under-served topics to docs and eval set
```

---

## Part 9: The Continuous Improvement Loop

```text
1. Collect: traces, user feedback, escalations, judge scores, emerging query clusters
2. Triage: categorize failures (retrieval, prompt, model, tool, knowledge gap, guardrail)
3. Curate: add representative failures to the eval dataset (with expected behavior)
4. Improve: fix docs/knowledge, prompts, retrieval, routing; fine-tune if patterns justify it
5. Validate: offline eval + security suite must pass
6. Release: canary with feature flags, monitor, ramp
7. Repeat weekly
```

The organizations that succeed with GenAI treat it as a **product with an operating rhythm**, not a one-time project.

---

## Summary

| Area | Key Practices |
|---|---|
| Artifacts | Version prompts, models, datasets, indexes, config as release manifests |
| Deployment | API vs self-hosted trade-offs; shadow, canary, A/B, feature flags |
| Serving | vLLM-class engines: continuous batching, PagedAttention, prefix caching, quantization |
| Capacity | TTFT/TPOT SLOs, GPU memory math, realistic load tests |
| Cost | Routing, context budgets, caching, batch APIs, distillation, attribution |
| Reliability | Fallbacks, circuit breakers, gateways, multi-dimensional SLOs |
| Improvement | Drift detection and a weekly feedback-to-eval loop |

---

## Interview Questions

- **[L1]** What is LLMOps and how does it differ from traditional MLOps?
- **[L1]** Which artifacts must be versioned in an LLM application, and why?
- **[L2]** Explain continuous batching and PagedAttention. Why do they matter for self-hosted LLM serving?
- **[L2]** What are TTFT, TPOT, and throughput? How do batch size and concurrency affect them?
- **[L2]** Compare API-based models and self-hosted open models for a production deployment.
- **[L3]** Your LLM feature costs have tripled in two months. How do you analyze and reduce cost without hurting quality?
- **[L3]** Design a highly available LLM platform serving 20 internal teams with multiple providers and self-hosted models.
- **[L3]** How would you plan GPU capacity for serving a 70B model to 500 concurrent users with a p95 TTFT under 1 second?

## Interview Answers

1. LLMOps is the discipline of deploying, operating, and continuously improving LLM-based systems. Unlike classic MLOps, which centers on training and retraining your own models, LLMOps mostly works with foundation models accessed via API or self-hosted, where the key artifacts are prompts, model choices, retrieval configurations, tools, and optional fine-tuned adapters. It emphasizes graded evaluation and human feedback, token and GPU cost management, inference latency (TTFT, tokens/s), rapid model churn and provider updates, and security risks like prompt injection and data leakage.
2. Prompts and prompt templates, exact model identifiers and generation parameters, fine-tuned models and adapters, training and evaluation datasets, embedding models and vector indexes, retrieval settings (top-k, reranker, thresholds), tool definitions, and guardrail policies. Each of these changes system behavior, so versioning them (ideally together as a release manifest) makes quality changes traceable to their cause, enables reproducible evaluations, allows safe rollbacks, and supports audits.
3. Continuous batching (iteration-level scheduling) lets new requests join the running batch and finished requests leave at every decode step, instead of waiting for an entire static batch to finish; this keeps the GPU busy and greatly increases throughput with variable-length requests. PagedAttention stores the KV cache in fixed-size blocks mapped through a block table, like virtual memory, avoiding the fragmentation and over-reservation of contiguous per-request buffers; this allows many more concurrent sequences on the same GPU and enables KV sharing for common prefixes. Together they are why engines like vLLM serve many times more requests per GPU than naive implementations.
4. TTFT (time to first token) measures latency until the first output token and is dominated by queueing and prompt prefill. TPOT (time per output token, or inter-token latency) measures the decoding speed a user experiences. Throughput is total tokens or requests per second across all users. Increasing batch size and concurrency raises throughput and lowers cost per token, but each decode step processes more sequences, so TPOT rises, and heavy prefill from new requests plus queueing increase TTFT. Capacity tuning finds the concurrency that maximizes throughput while meeting TTFT and TPOT SLOs.
5. API models offer frontier quality, zero infrastructure, instant scaling within quotas, and fast time to market, but cost scales linearly with tokens, data leaves your environment (subject to agreements), customization is limited, and you depend on provider rate limits, availability, and model changes. Self-hosted open models give full data control, on-prem deployment, deep customization (fine-tuning, quantization, constrained decoding), and lower marginal cost at high sustained utilization, but require GPU procurement, serving expertise, scaling, on-call, and usually offer lower quality than frontier models on the hardest tasks. Many companies combine both via a gateway, choosing per use case.
6. Break down cost by feature, tenant, model, and token type using usage telemetry to find the drivers: traffic growth, longer prompts or RAG contexts, longer outputs, agent loops, a switch to a pricier model, retries, or abuse. Then apply levers while measuring quality on the eval set: route simple requests to smaller models; trim system prompts and few-shot examples; cap and rerank RAG context; limit output length; order prompts to maximize provider prompt caching; add exact and semantic response caches; move offline workloads to batch APIs; enforce agent step and token budgets and per-tenant quotas; and, for stable high-volume tasks, distill into a fine-tuned small model. Add cost dashboards, budgets, and alerts to catch future growth early.
7. Put a central AI gateway in front of all LLM traffic, providing one OpenAI-compatible API, per-team keys and quotas, routing to multiple providers and self-hosted vLLM clusters, retries, circuit breakers, and cross-provider fallback, caching, PII redaction, and centralized logging, tracing, and cost attribution. Deploy the gateway statelessly across multiple zones behind a load balancer. Self-hosted models run on Kubernetes GPU node pools with multiple replicas across zones, autoscaling on queue depth or KV cache usage, and health checks. Use provisioned throughput or reserved capacity with major providers for critical workloads. Maintain a model catalog with pinned versions and deprecation tracking, provide shared evaluation and prompt-registry tooling, and define SLOs, dashboards, on-call runbooks, and incident processes.
8. Characterize the workload: prompt and output token distributions, peak request rate, and SLOs (p95 TTFT < 1 s, target TPOT). Choose precision (e.g. FP8 weights, about 70 GB) and the parallelism layout (for example 2-4 x 80 GB GPUs per replica with tensor parallelism), then compute KV cache per token (layers x KV heads x head dim x 2 x bytes; GQA helps) and multiply by context length and concurrent sequences per replica to find how many sequences fit. Benchmark one replica with vLLM under the realistic distribution to find the maximum concurrency and request rate that meet the TTFT/TPOT SLOs, with prefix caching and chunked prefill enabled. Divide peak concurrency (500) by per-replica sustainable concurrency, add headroom (~30%) and N+1 redundancy across zones, and configure autoscaling for daily patterns. Revisit with speculative decoding, KV cache quantization, or routing easier traffic to a smaller model if GPU costs are too high.
