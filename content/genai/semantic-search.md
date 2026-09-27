---
id: genai-semantic-search
slug: semantic-search
title: Semantic and Hybrid Search
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Framework agnostic concepts"
prerequisites: [genai-embeddings-vector-db]
tags: [genai, semantic-search, keyword-search, bm25, hybrid]
relatedTopics: [genai-advanced-rag, genai-rag-fundamentals]
order: 60
status: published
---
# Semantic and Hybrid Search

## Introduction
Keyword search matches literal terms. Semantic search compares embedding meaning. Hybrid search combines both because real users ask both paraphrased questions and exact-identifier questions.

```text
Query
 ├── BM25/keyword → exact matches
 └── embedding     → meaning matches
          ↓ rank fusion
      robust result set
```

## Purpose
Semantic search handles synonyms and different phrasing. Keyword search is often better for product codes, error messages, names, versions, and exact phrases. Hybrid search is useful when both kinds occur in the same product.

## Simple example
```text
Query: “replacement filter for XR-450”

Keyword search → exact XR-450 documents
Semantic search → related replacement/filter instructions
Hybrid result → exact model + relevant meaning
```

## Professional company-level example
Use Reciprocal Rank Fusion (RRF) when raw score scales are incompatible:

```python
def rrf(rankings, k=60):
    scores = {}
    for ranking in rankings:
        for position, doc_id in enumerate(ranking, start=1):
            scores[doc_id] = scores.get(doc_id, 0) + 1 / (k + position)
    return sorted(scores, key=scores.get, reverse=True)
```

Evaluate natural-language and exact-term query sets separately. An aggregate score can hide a regression in one important query class.

## Important behavior
BM25 rewards term frequency and rarity. Embeddings place semantically related text near each other. Neither signal automatically enforces authorization; both retrieval paths must apply tenant/access filters.

## Comparison
| Search | Good at | Weak at |
|---|---|---|
| Keyword/BM25 | Exact names, IDs, codes, phrases | Synonyms/paraphrases |
| Semantic | Meaning and natural questions | Exact identifiers/model numbers |
| Hybrid | Mixed query behavior | More infrastructure/latency |
| Reranker | Candidate precision | Extra compute |

## Interview Questions
- **[L1]** What is the difference between keyword and semantic search?
- **[L1]** Why can semantic search miss product codes?
- **[L2]** Why should raw BM25 and cosine scores not be averaged directly?
- **[L2]** What is Reciprocal Rank Fusion?
- **[L3]** How would you decide whether hybrid search is worth its complexity?
- **[L3]** How do you prevent one retrieval path from bypassing authorization?

## Interview Answers
1. Keyword search compares terms; semantic search compares learned vector meaning.
2. A code may carry little semantic meaning, while exact matching is precisely what the user needs.
3. The scores have different scales/distributions. Normalize deliberately or combine rank positions instead.
4. RRF assigns contribution based on rank position across multiple result lists and sums the contributions.
5. Analyze real query logs, build labeled tests for natural/exact queries, compare quality/latency/cost, and add hybrid only when measurable benefit justifies it.
6. Apply the same tenant/access filter to keyword and vector indexes before results enter fusion; validate authorization before returning context/source data.

## Expert perspective
Search architecture should follow query behavior. A product with identifiers, error codes, natural-language support questions, and names will often need hybrid retrieval; a pure semantic or pure keyword assumption should be proven with data.
