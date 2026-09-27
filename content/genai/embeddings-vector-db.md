---
id: genai-embeddings-vector-db
slug: embeddings-vector-db
title: Embeddings and Vector Databases
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Framework agnostic concepts"
prerequisites: [genai-transformers]
tags: [genai, embeddings, vectors, vector-database, similarity-search]
relatedTopics: [genai-semantic-search, genai-rag-fundamentals]
order: 20
status: published
---
# Embeddings and Vector Databases

## Introduction
An embedding converts content into a numeric vector such that semantically related content tends to be close in vector space. A vector database stores vectors, metadata, and indexes to retrieve nearest candidates efficiently.

```text
Document → embedding model → vector + metadata → vector index
Query    → same model      → query vector → nearest candidates
```

## Purpose
Embeddings enable semantic search, recommendations, deduplication, classification, and RAG retrieval where exact keyword overlap is insufficient.

## Simple example
```python
from openai import OpenAI
import numpy as np

client = OpenAI()

def embed(text: str) -> list[float]:
    response = client.embeddings.create(
        model="text-embedding-3-small",
        input=text
    )
    return response.data[0].embedding

def cosine(a: list[float], b: list[float]) -> float:
    x, y = np.array(a), np.array(b)
    return float(x @ y / (np.linalg.norm(x) * np.linalg.norm(y)))
```

A query about “voiding a purchase” can retrieve a document about “cancelling an order” even when the exact words differ.

## Professional company-level example
Index each chunk with authorization/source metadata:

```python
vector_db.upsert(
    id=chunk.id,
    vector=embed(chunk.text),
    metadata={
        "tenant_id": chunk.tenant_id,
        "source": chunk.url,
        "section": chunk.section,
        "access_level": chunk.access_level,
        "embedding_model": "text-embedding-3-small"
    }
)
```

At query time, apply authorization filters before or during vector retrieval. Never retrieve all tenants and filter after the model has already seen the data.

## Search behavior

```text
Embedding similarity
    ↓ approximate nearest-neighbor index
Candidate documents
    ↓ metadata filters/reranker
Final context
```

Vector databases use approximate nearest-neighbor structures such as HNSW to avoid comparing a query against every vector. This trades a small amount of recall for speed/memory efficiency.

The same embedding model/version should be used for stored documents and queries. Changing models requires re-embedding and re-indexing; vectors from unrelated models are not comparable.

## Comparison
| Search | Strength | Limitation |
|---|---|---|
| Keyword/BM25 | Exact terms, identifiers | Misses paraphrases |
| Vector | Meaning/semantic similarity | Can miss exact codes; model-dependent |
| Hybrid | Both signals | More infrastructure/latency |
| Reranker | Better candidate precision | Extra compute/latency |

## Interview Questions
- **[L1]** What is an embedding?
- **[L1]** Why can vector search find semantic matches without shared words?
- **[L2]** Why must query/document embeddings use the same model?
- **[L2]** What problem does approximate nearest-neighbor indexing solve?
- **[L3]** How would you evaluate an embedding model for a domain?
- **[L3]** How should tenant authorization work in a vector database?

## Interview Answers
1. It is a fixed-length numeric representation learned to encode useful similarity relationships.
2. The model maps related meanings near each other even when literal wording differs.
3. Each model learns its own coordinate space/dimensions/geometry. Comparing vectors across models has no reliable meaning.
4. Brute-force comparison against millions/billions of vectors is too slow; ANN indexes find close candidates faster with some recall trade-off.
5. Create representative labeled queries/source documents and measure recall@k/precision, latency, cost, and domain-specific failure cases.
6. Store tenant/access metadata and apply filters during retrieval; validate authorization again before returning sources/context.

## Expert perspective
Embeddings are not a guarantee of relevance. They are one retrieval signal whose quality depends on model/domain fit, chunking, metadata, filters, and evaluation. Production vector search is an information-retrieval system, not just an API call that returns “similar” text.
