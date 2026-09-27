---
id: genai-advanced-rag
slug: advanced-rag
title: "Advanced RAG: Hybrid Retrieval, Reranking, and Query Rewriting"
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Framework agnostic concepts"
prerequisites: [genai-rag-fundamentals, genai-semantic-search]
tags: [genai, advanced-rag, reranking, hybrid-search, query-rewriting]
relatedTopics: [genai-chunking-strategies, genai-embeddings-vector-db]
order: 40
status: published
---
# Advanced RAG: Hybrid Retrieval, Reranking, and Query Rewriting

## Introduction
Basic RAG retrieves top-k embedding matches and sends them to an LLM. Advanced RAG improves specific failure modes using multiple retrieval signals, a more accurate second-stage ranker, query reformulation, or iterative retrieval.

```text
Question
  ├── semantic search
  ├── keyword/BM25 search
  └── rewritten/sub-queries
          ↓ merge candidates
     reranker/cross-encoder
          ↓
      final context
          ↓
          LLM
```

## Purpose
Use advanced retrieval only when evaluation shows basic retrieval misses relevant information, returns close-but-wrong results, or struggles with exact identifiers/complex questions.

## Techniques

### Hybrid retrieval
Semantic search handles paraphrases; keyword search handles exact codes/names/phrases. Combine rank positions rather than raw scores because cosine similarity and BM25 scales are different.

```python
def reciprocal_rank_fusion(rankings, smoothing=60):
    scores = {}
    for ranking in rankings:
        for position, document_id in enumerate(ranking, start=1):
            scores[document_id] = scores.get(document_id, 0) + 1 / (smoothing + position)
    return sorted(scores, key=scores.get, reverse=True)
```

### Reranking
A bi-encoder embeds query/documents independently and is fast enough for millions of candidates. A cross-encoder reads the query and candidate together, giving better relevance judgment but much higher per-candidate cost. Retrieve 30–100 candidates cheaply, then rerank only that set.

### Query rewriting
A model can expand an unclear user question into alternative searches or split a multi-part question into sub-questions. Rewriting can add noise when the original query is already precise, so measure it rather than enabling it universally.

## Professional company-level example
```text
User: “What happens if a payment is accepted but shipping fails?”

Rewrite/sub-questions:
  1. payment accepted order state
  2. shipping failure compensation
  3. order saga compensation policy

Retrieve each → deduplicate → rerank against original question
→ answer with citations and disagreement handling
```

Every stage needs tenant/access filters, timeout/cost limits, tracing, and evaluation metrics.

## Common failure scenarios
- Reranker applied to millions of documents, creating unacceptable latency.
- Keyword and vector scores averaged without normalization.
- Query rewriting changes the user's intent.
- More retrieved chunks dilute the prompt.
- Advanced retrieval is added without proving it improves recall/precision.

## Comparison
| Technique | Solves | Cost |
|---|---|---|
| Semantic search | Paraphrase/meaning mismatch | Embedding/index |
| BM25 | Exact terms/identifiers | Keyword index |
| Hybrid | Both failure types | Two indexes/fusion |
| Reranking | Initial candidate imprecision | Extra model latency |
| Rewriting | Poor phrasing/multi-part query | Extra LLM calls |

## Interview Questions
- **[L1]** What is reranking?
- **[L1]** Why combine keyword and semantic retrieval?
- **[L2]** What is the difference between a bi-encoder and cross-encoder?
- **[L2]** Why should raw BM25 and cosine scores not be averaged directly?
- **[L3]** How would you decide which advanced RAG technique to introduce first?
- **[L3]** How do you prevent query rewriting from changing user intent?

## Interview Answers
1. Reranking reorders a small initial candidate set with a more accurate relevance model.
2. Keyword search finds exact terms; semantic search finds meaning/paraphrases. Hybrid catches both.
3. A bi-encoder precomputes independent embeddings for fast broad retrieval; a cross-encoder jointly reads query/document for accurate but expensive reranking.
4. They have different scales/distributions. Normalize carefully or use rank fusion such as RRF.
5. Establish baseline evaluation, identify the failure mode, add one technique, measure retrieval/answer improvement versus latency/cost, and keep only useful complexity.
6. Preserve the original question, generate bounded alternatives, compare rewritten intent, retrieve with metadata/security filters, and evaluate on adversarial/ambiguous queries.

## Expert perspective
Advanced RAG is not a checklist of fashionable components. Each technique should address a measured retrieval failure mode; otherwise it adds latency, cost, and operational debugging without improving the answer.
