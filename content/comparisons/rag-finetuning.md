---
id: comparisons-rag-finetuning
slug: rag-finetuning
title: RAG vs Fine-Tuning vs Prompting
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 25
version:
  minimum: "Generative AI concepts, OpenAI SDK 1.x examples"
prerequisites: [genai-rag-fundamentals, genai-transformers]
tags: [genai, rag, fine-tuning, prompting, comparison]
relatedTopics: [genai-production-evaluation, genai-security-guardrails]
order: 70
status: published
---
# RAG vs Fine-Tuning vs Prompting

## Introduction

There are three main ways to make a general-purpose LLM useful for your specific problem:

- **Prompting** changes the **instructions and examples** sent with each request.
- **RAG (Retrieval-Augmented Generation)** changes the **knowledge** available per request by retrieving relevant documents and inserting them into the prompt.
- **Fine-tuning** changes the **model's weights** by training it on examples, so behavior becomes built in.

A useful rule: **prompting and fine-tuning shape behavior; RAG supplies knowledge.** Always start with prompting, add RAG when the model needs facts it does not have, and consider fine-tuning only when evaluation shows prompting cannot reach the required quality, cost, or latency.

## Quick Decision Table

| Concern | Prompting | RAG | Fine-Tuning |
|---|---|---|---|
| What changes | Instructions and examples | Context retrieved per request | Model weights |
| Adds new knowledge | Only what fits in the prompt | Yes, from your documents | Poorly and not reliably updatable |
| Freshness | Immediate (edit prompt) | Immediate (re-index documents) | Requires retraining |
| Citations / traceability | No | Yes | No |
| Per-user permissions | No | Yes (filter at retrieval) | No |
| Format / style / tone consistency | Good | Not its purpose | Excellent |
| Setup effort | Hours | Days-weeks (ingestion, index, eval) | Weeks (data, training, eval, serving) |
| Cost profile | Token cost of long prompts | Retrieval infra + context tokens | Training + possibly cheaper inference with a smaller model |
| Typical failure | Ignores instructions, inconsistent | Retrieves wrong/missing context | Overfitting, forgetting, stale knowledge |

## Prompting

### When to Use

- Always first: it is the cheapest, fastest experiment
- Tasks the model already knows how to do (summarize, classify, extract, rewrite)
- Controlling format with structured outputs, role, constraints, and few-shot examples

### When Not to Rely on Prompting Alone

- The model needs private or recent facts
- The prompt becomes thousands of tokens of instructions on every call
- Quality plateaus even with good examples

### Example

```python
from openai import OpenAI
from pydantic import BaseModel
from typing import Literal

client = OpenAI()

class Ticket(BaseModel):
    category: Literal["billing", "technical", "account", "other"]
    priority: Literal["low", "medium", "high"]
    summary: str

completion = client.beta.chat.completions.parse(
    model="gpt-4o-mini",
    temperature=0,
    messages=[
        {"role": "system", "content": "Classify support tickets. High priority = outage, data loss, or payment failure."},
        {"role": "user", "content": "Since this morning none of our users can log in to the dashboard."},
    ],
    response_format=Ticket,
)
print(completion.choices[0].message.parsed)  # category='technical' priority='high' ...
```

## RAG

### When to Use

- Answers must come from your documents: policies, manuals, contracts, knowledge bases
- Knowledge changes frequently
- Users need citations to verify answers
- Different users may see different documents

### When Not to Use

- The problem is behavior or format, not knowledge
- The whole relevant context is small and fixed (just put it in the prompt)

### Example

```python
def answer_with_rag(question: str, user) -> str:
    chunks = vector_store.search(question, k=5, filter={"tenant_id": user.tenant_id, "group": {"$in": user.groups}})
    context = "\n\n".join(f"[{i}] {c.text} (source: {c.source})" for i, c in enumerate(chunks, 1))
    response = client.chat.completions.create(
        model="gpt-4o-mini",
        temperature=0,
        messages=[
            {"role": "system", "content": "Answer only from the sources and cite them like [1]. "
                                          "If the sources do not contain the answer, say you don't know."},
            {"role": "user", "content": f"Sources:\n{context}\n\nQuestion: {question}"},
        ],
    )
    return response.choices[0].message.content
```

**Why each part exists:** permission filters at retrieval stop the model from ever seeing unauthorized documents; numbered sources enable citations; the "don't know" instruction reduces hallucination when retrieval misses.

## Fine-Tuning

### When to Use

- Consistent output format, tone, or domain style that prompting cannot achieve reliably
- High-volume narrow tasks where a small fine-tuned model can replace a large model (cost and latency)
- Long instructions you want baked into the model to shorten prompts
- Specialized classification or extraction with thousands of labeled examples

### When Not to Use

- To teach facts that change (the model cannot be updated per document, and it cannot cite)
- With fewer than roughly 100 high-quality examples
- Before a strong prompting baseline and evaluation set exist

### Example (Hosted Fine-Tuning)

```python
# train.jsonl: {"messages": [{"role": "system", ...}, {"role": "user", ...}, {"role": "assistant", "content": "{...json...}"}]}
training_file = client.files.create(file=open("train.jsonl", "rb"), purpose="fine-tune")
job = client.fine_tuning.jobs.create(model="gpt-4o-mini-2024-07-18", training_file=training_file.id, suffix="ticket-classifier")

# After the job succeeds, call the resulting model name exactly like any other model
# client.chat.completions.create(model=job.fine_tuned_model, messages=[...])
```

## Combining Them

The approaches are complementary:

| Combination | Example |
|---|---|
| Prompting + RAG | Most knowledge assistants: instructions + retrieved documents |
| Fine-tuning + RAG | Fine-tune for a strict answer format and domain tone; RAG supplies current facts |
| Fine-tuning for retrieval | Fine-tune the **embedding model** or reranker on your domain to improve RAG recall |
| Distillation | Use a large model with RAG to generate reviewed training data; fine-tune a small model for cost |

## Same Scenario: Insurance Company Assistant

| Requirement | Approach |
|---|---|
| Answer questions about current policy terms with citations | RAG over policy documents |
| Always respond in a compliant, empathetic tone with a fixed disclaimer | Prompting first; fine-tune if it drifts at scale |
| Classify 500,000 claims per month into 60 categories cheaply | Fine-tune a small model (after prompting baseline) |
| Show adjusters only claims in their region | RAG with permission filters |
| Policy terms change monthly | RAG (re-index), not fine-tuning |

## Decision Matrix

| Factor | Prompting | RAG | Fine-Tuning |
|---|---|---|---|
| Speed to first result | Strong | Medium | Weak |
| Private / changing knowledge | Weak | Strong | Weak |
| Behavior consistency | Medium | Weak | Strong |
| Cost at high volume | Medium | Medium | Strong (small model) |
| Explainability (citations) | Weak | Strong | Weak |
| Maintenance burden | Low | Medium (ingestion, index) | High (data, retraining, evaluation) |

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Fine-tuning to "teach the model our documents" | Use RAG |
| Skipping evaluation and judging by a few demos | Build an evaluation set before choosing |
| Stuffing every document into the prompt | Retrieve only relevant chunks |
| RAG without permission filtering | Filter at retrieval by user and tenant |
| Fine-tuning on noisy or inconsistent data | Curate, dedupe, and review examples |
| Treating the approaches as mutually exclusive | Combine them where each fits |

## Interview Questions
- **[L1]** What problem does RAG solve?
- **[L1]** What does fine-tuning change?
- **[L2]** Why is prompting not a knowledge-storage strategy?
- **[L2]** When does fine-tuning make sense?
- **[L3]** How do you choose between RAG and fine-tuning for private company knowledge?
- **[L3]** Can RAG and fine-tuning be combined?

## Interview Answers
1. RAG solves the problem that an LLM does not know private, domain-specific, or recent information and may hallucinate when asked about it. It retrieves relevant passages from your own documents at query time and places them in the prompt, so the model answers grounded in that content, can cite sources, respects per-user permissions through retrieval filters, and stays current by re-indexing documents instead of retraining.
2. Fine-tuning continues training the model on your examples and updates its weights (fully or through adapters such as LoRA), so the desired behavior becomes built in: output format, tone, domain vocabulary, or skill at a narrow task. It changes how the model responds by default; it is not a reliable way to store and update specific facts, and the model cannot cite where learned information came from.
3. A prompt only exists for one request and is limited by the context window and token cost, so you cannot put a large, changing knowledge base in it. Manually maintained prompt knowledge quickly becomes stale and inconsistent, cannot be permission-filtered per user, and long prompts increase latency and cost on every call. Knowledge should live in a searchable store and be retrieved selectively, which is what RAG does.
4. Fine-tuning makes sense after a strong prompting baseline and evaluation set exist and show a gap that prompting cannot close: when outputs must follow a strict format or style consistently, when a high-volume narrow task could run on a smaller, cheaper, faster fine-tuned model, when long instructions should be baked in to shorten prompts, or when you have hundreds to thousands of high-quality representative examples. It does not make sense for adding changing facts.
5. For private company knowledge, start with RAG: ingest documents with metadata and permissions, retrieve with filters, generate with citations, and evaluate retrieval and answer quality. RAG supports freshness, access control, and traceability, which fine-tuning cannot provide. Consider fine-tuning only for behavior aspects that remain weak after good prompting, such as answer format, tone, or domain terminology, or fine-tune the embedding model or reranker to improve retrieval, and measure each change against the same evaluation set.
6. Yes, and it is common. Fine-tuning can teach the model a consistent answer structure, domain style, or better use of retrieved context, while RAG supplies current, permission-filtered facts at query time. Fine-tuning can also be applied to the retrieval components themselves, such as embedding models and rerankers. Each part should be evaluated separately: retrieval metrics for RAG and task or style metrics for the fine-tuned behavior, plus end-to-end answer quality.
