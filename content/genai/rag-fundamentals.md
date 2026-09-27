---
id: genai-rag-fundamentals
slug: rag-fundamentals
title: "RAG: Retrieval-Augmented Generation"
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 55
version:
  minimum: "Framework agnostic"
prerequisites: [genai-embeddings-vector-db, genai-chunking-strategies]
tags: [genai, rag, retrieval, grounding, llm]
relatedTopics: [genai-advanced-rag, genai-semantic-search]
order: 30
status: published
---
# RAG: Retrieval-Augmented Generation

## Introduction
RAG retrieves relevant source chunks at query time and places them in an LLM prompt before generating an answer.

```text
Documents
   ↓ chunk + metadata + embeddings
Vector/keyword index
   ↑ query embedding/search
User question
   ↓ retrieve/rerank
Relevant context + question
   ↓
LLM answer + citations
```

RAG does not train the model. It supplies current/private information as context for one request.

## Purpose
Use RAG when answers must use a knowledge base that is private, current, large, or traceable. It is often easier to update an index than retrain a model.

## Simple example
```python
def answer(question, vector_db, llm):
    chunks = vector_db.search(embed(question), top_k=5)
    context = "\n\n".join(chunk.text for chunk in chunks)

    prompt = f"""Answer using only the context below.
If the answer is not present, say that you do not know.

Context:
{context}

Question: {question}
"""
    return llm.generate(prompt)
```

Retrieval quality matters as much as the prompt. An excellent prompt cannot recover information that was never retrieved.

## Professional company-level example
```python
def answer_with_sources(question, retriever, llm):
    chunks = retriever.search(question, top_k=8)

    if not chunks or max(chunk.score for chunk in chunks) < 0.72:
        return Answer("I could not find enough verified information.", [])

    context = "\n\n".join(
        f"[{i}] {chunk.text}" for i, chunk in enumerate(chunks, start=1)
    )
    prompt = f"""
    Answer using only the numbered sources.
    Cite every important claim as [number].
    If sources disagree, explain the disagreement.

    Sources:
    {context}

    Question: {question}
    """
    response = llm.generate(prompt)
    return Answer(response, [chunk.source for chunk in chunks])
```

A production RAG service needs:

```text
Ingestion → parsing → chunking → embedding → indexing
                                      ↓
Question → retrieval → reranking → context budget
                                      ↓
                              LLM + citations
                                      ↓
                         evaluation/feedback/metrics
```

## What can go wrong

- Relevant document was never indexed.
- Chunk split lost the surrounding context.
- Embedding model is poor for the domain.
- Top results are similar but not actually relevant.
- Too many chunks dilute the prompt.
- Context exceeds the model window.
- LLM adds facts not supported by sources.
- Retrieved documents contain prompt injection.
- User is shown a document they are not authorized to access.

## Comparison
| Approach | Knowledge source | Update cost | Traceability |
|---|---|---|---|
| Base LLM prompt | Model training/context | Prompt change | Low unless cited |
| Fine-tuning | Model weights | Retraining | Limited |
| RAG | Retrieved source data | Re-indexing | High with citations |
| RAG + reranking | Retrieved/reranked data | More latency/cost | High |

## Interview Questions
- **[L1]** What problem does RAG solve?
- **[L1]** What are the main stages of a RAG pipeline?
- **[L2]** Why does RAG not completely eliminate hallucinations?
- **[L2]** Why should a system refuse to answer when retrieval confidence is low?
- **[L3]** How would you evaluate retrieval quality and answer faithfulness separately?
- **[L3]** How should authorization work in a multi-tenant RAG system?

## Interview Answers
1. RAG gives an LLM current/private/domain-specific source content at query time without retraining the model.
2. Ingest documents, parse/chunk them, embed/index them, embed the query, retrieve/rerank chunks, construct context, generate, and evaluate/cite the answer.
3. The model can ignore or misinterpret context, add unsupported knowledge, or follow malicious text inside a retrieved document. Prompt instructions are not a hard security boundary.
4. Low-confidence retrieval means the system lacks reliable evidence; refusing is safer than producing a confident answer from irrelevant context.
5. Measure retrieval recall/precision on labeled queries separately from generation groundedness/citation correctness on retrieved context. A system can retrieve correctly but answer incorrectly.
6. Apply tenant/access filters during retrieval, carry authorization metadata with chunks, avoid mixing restricted data into a shared unrestricted index, and enforce permissions again before returning sources.

## Expert perspective
A RAG system is a search system plus a probabilistic generator. Treat retrieval quality, context construction, authorization, prompt injection, citation faithfulness, and cost as separate engineering problems; do not call a RAG answer trustworthy merely because a vector search returned something.
