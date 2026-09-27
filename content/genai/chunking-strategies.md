---
id: genai-chunking-strategies
slug: chunking-strategies
title: Document Chunking for RAG
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 45
version:
  minimum: "Framework agnostic concepts"
prerequisites: [genai-embeddings-vector-db, genai-rag-fundamentals]
tags: [genai, chunking, rag, document-processing]
relatedTopics: [genai-advanced-rag, genai-semantic-search]
order: 50
status: published
---
# Document Chunking for RAG

## Introduction
Chunking splits documents into retrieval units before embedding. Chunk size and boundaries strongly influence whether retrieval returns complete, relevant context or fragmented/noisy text.

```text
Large document
      ↓ parse structure
Headings/paragraphs/tables/code blocks
      ↓ chunk with token budget + overlap
Retrieval units + source metadata
      ↓ embeddings/index
Searchable knowledge base
```

## Purpose
A chunk must be small enough for precise retrieval and large enough to retain the context needed to understand its claims. Chunking is not a universal constant; it depends on document structure, query style, and model context budget.

## Strategies

```text
Fixed size       → predictable, can split meaning
Fixed + overlap  → boundary context, more duplicate storage
Structure-aware  → headings/paragraphs, better semantics
Semantic          → topic shifts, higher processing cost
Parent-child     → small retrieval, larger context returned
```

## Simple example
```python
def chunks_by_tokens(text, max_tokens=400, overlap=40):
    tokens = tokenizer.encode(text)
    start = 0
    while start < len(tokens):
        end = min(start + max_tokens, len(tokens))
        yield tokenizer.decode(tokens[start:end])
        start = end - overlap
```

Overlap helps when a fact is split at a boundary, but too much overlap increases indexing/storage cost and produces redundant retrieval results.

## Professional company-level example
For technical documentation, preserve heading context:

```python
for section in document.sections:
    text = f"{section.heading}\n{section.body}"
    for part in split_with_overlap(text, max_tokens=400, overlap=40):
        index.add(
            text=part,
            metadata={
                "source": document.url,
                "title": document.title,
                "section": section.heading,
                "tenant_id": document.tenant_id,
                "access_level": document.access_level
            }
        )
```

A retrieved paragraph without its heading may not tell the LLM whether it describes deployment, configuration, limitations, or an example. Keep enough metadata to cite the source and enforce access control.

## Failure scenarios
- Chunks split a procedure between steps.
- A chunk contains a pronoun with no antecedent.
- Large chunks dilute a precise fact.
- Tiny chunks lose necessary context.
- Tables/code are split into unusable fragments.
- Metadata is lost, preventing citations/security filtering.
- A new chunking strategy is deployed without re-indexing old vectors.

## Comparison
| Strategy | Benefit | Risk |
|---|---|---|
| Fixed-size | Simple/predictable | Arbitrary semantic breaks |
| Structure-aware | Coherent units | Requires parser/format handling |
| Overlap | Preserves boundaries | More storage/duplicate results |
| Parent-child | Precise retrieval + rich context | More index/context logic |
| Semantic | Topic-aware | More compute/complexity |

## Interview Questions
- **[L1]** Why does chunk size matter in RAG?
- **[L1]** What is chunk overlap?
- **[L2]** Why is structure-aware chunking usually better than arbitrary characters?
- **[L2]** Why must chunk metadata be preserved?
- **[L3]** How would you choose chunk size and overlap for a real corpus?
- **[L3]** When would you use a parent-child retrieval strategy?

## Interview Answers
1. Large chunks dilute embeddings and consume context; tiny chunks lose the context needed to interpret a fact.
2. It repeats a boundary region between chunks so an idea split at a boundary is more likely to remain understandable in at least one chunk.
3. Document headings/paragraphs usually preserve coherent meaning better than arbitrary character boundaries.
4. Metadata enables citations, filtering, tenant authorization, freshness filtering, and rebuild/debugging.
5. Create a labeled query/source evaluation set, test several sizes/overlaps, measure recall/precision and answer completeness, and choose based on latency/cost too.
6. When small passages are best for finding a match but a larger parent section is needed to answer correctly; retrieve child vectors and return the authorized parent context.

## Expert perspective
Chunking is often more important than changing the embedding model. If the knowledge unit is poorly formed, even an excellent vector model cannot retrieve complete, understandable evidence consistently.
