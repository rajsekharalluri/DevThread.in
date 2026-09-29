---
id: genai-eng-advanced-rag
slug: advanced-rag
title: "Module 7: Advanced RAG"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 110
version:
  minimum: "Python 3.11+, sentence-transformers 3.x, rank-bm25, networkx"
prerequisites: [genai-eng-rag]
tags: [genai-engineer, advanced-rag, hybrid-search, reranking, graph-rag, query-transformation, agentic-rag, bm25]
relatedTopics: [genai-eng-ai-agents]
order: 7
status: published
---
# Module 7: Advanced RAG

## Introduction

A basic RAG pipeline (chunk, embed, top-k, generate) typically gets you to 60-75% answer quality. Getting to 90%+ for production knowledge systems requires attacking each weak point: queries that do not match document wording, dense retrieval missing exact terms, noisy top-k results, chunks without enough context, questions that need multi-hop reasoning across documents, and questions that ask for global summaries rather than specific facts.

This module covers the techniques used in production-grade knowledge systems: **hybrid search**, **reranking**, **query transformation** (rewriting, multi-query, HyDE, decomposition), **advanced chunking and indexing** (parent-child, contextual retrieval, summaries), **Graph RAG**, **self-correcting and agentic RAG**, and **context optimization**.

---

## Part 1: The Advanced RAG Pipeline

```text
User question
1. Query understanding: classify intent, rewrite, expand, decompose, extract filters
2. Retrieval: hybrid (dense vectors + BM25 keyword) with metadata filters, multiple queries
3. Fusion: merge result lists (Reciprocal Rank Fusion)
4. Reranking: cross-encoder or LLM reranker scores top 50 down to top 5
5. Context construction: expand to parent chunks, deduplicate, order, compress
6. Generation: grounded answer with citations
7. Verification: faithfulness check, retry or fall back if unsupported
```

Each step is optional. Add them based on **measured** failures, not by default - each adds latency and cost.

| Technique | Fixes | Typical Cost |
|---|---|---|
| Hybrid search | Missed exact terms, codes, names | Low |
| Reranking | Relevant chunk ranked 8th instead of 1st | Medium (50-300 ms) |
| Query rewriting / multi-query | Vocabulary mismatch, vague queries | 1 extra LLM call |
| HyDE | Short queries vs long documents | 1 extra LLM call |
| Decomposition | Multi-part and comparison questions | Several retrievals |
| Parent-child chunks | Precise match but missing context | Low |
| Contextual retrieval | Chunks meaningless alone | Indexing-time LLM cost |
| Graph RAG | Multi-hop relations, global questions | High indexing cost |
| Self-correction / agentic | Complex, open-ended questions | Multiple LLM calls |

---

## Part 2: Hybrid Search

### Why Dense Retrieval Alone Is Not Enough

Dense embeddings excel at meaning but struggle with:
- Exact identifiers: `ERR-4032`, `SKU-88213`, `CVE-2024-3094`
- Rare proper nouns and acronyms
- Negations and precise numbers

**BM25** (the classic keyword ranking used by Elasticsearch/Lucene) excels at exactly these. Hybrid search combines both.

### BM25 in Python

```python
# pip install rank-bm25
from rank_bm25 import BM25Okapi
import re

corpus = [
    {"id": "d1", "text": "Error ERR-4032 occurs when the payment token has expired. Refresh the token."},
    {"id": "d2", "text": "To update billing details, go to Settings > Billing and edit the card."},
    {"id": "d3", "text": "Payment failures can happen due to insufficient funds or expired cards."},
]

def tokenize(text: str) -> list[str]:
    return re.findall(r"[a-z0-9\-]+", text.lower())

bm25 = BM25Okapi([tokenize(d["text"]) for d in corpus])

def bm25_search(query: str, k: int = 10) -> list[str]:
    scores = bm25.get_scores(tokenize(query))
    ranked = sorted(range(len(corpus)), key=lambda i: scores[i], reverse=True)
    return [corpus[i]["id"] for i in ranked[:k] if scores[i] > 0]

print(bm25_search("ERR-4032"))  # ['d1']
```

### Reciprocal Rank Fusion (RRF)

BM25 scores and cosine similarities are on different scales, so you cannot simply add them. RRF combines **ranks** instead of scores:

`RRF_score(doc) = sum over result lists of 1 / (k + rank_in_list)` with k typically 60.

```python
from collections import defaultdict

def reciprocal_rank_fusion(result_lists: list[list[str]], k: int = 60) -> list[tuple[str, float]]:
    scores: dict[str, float] = defaultdict(float)
    for results in result_lists:
        for rank, doc_id in enumerate(results, start=1):
            scores[doc_id] += 1.0 / (k + rank)
    return sorted(scores.items(), key=lambda x: x[1], reverse=True)

dense_results = ["d3", "d2", "d1"]   # semantic: "payment failures" is close in meaning
sparse_results = ["d1", "d3"]        # keyword: exact match on ERR-4032
print(reciprocal_rank_fusion([dense_results, sparse_results]))
# d3 and d1 rise to the top because both retrievers agree on them
```

### Hybrid Retriever

```python
from sentence_transformers import SentenceTransformer
import numpy as np

encoder = SentenceTransformer("all-MiniLM-L6-v2")
doc_vectors = encoder.encode([d["text"] for d in corpus], normalize_embeddings=True)

def dense_search(query: str, k: int = 10) -> list[str]:
    q = encoder.encode(query, normalize_embeddings=True)
    order = np.argsort(-(doc_vectors @ q))[:k]
    return [corpus[i]["id"] for i in order]

def hybrid_search(query: str, k: int = 5) -> list[str]:
    fused = reciprocal_rank_fusion([dense_search(query, 20), bm25_search(query, 20)])
    return [doc_id for doc_id, _ in fused[:k]]
```

Production engines provide hybrid search natively: Elasticsearch/OpenSearch, Weaviate, Qdrant (sparse + dense vectors), Azure AI Search, Vespa, and Postgres (pgvector + full-text `tsvector`).

### Learned Sparse Retrieval

Models like **SPLADE** and **BGE-M3** produce sparse vectors that behave like keywords but with learned term expansion (a query for "car" also activates "vehicle"). They offer a middle ground between BM25 and dense embeddings.

---

## Part 3: Reranking

### Bi-Encoder vs Cross-Encoder

| | Bi-encoder (embedding retrieval) | Cross-encoder (reranker) |
|---|---|---|
| How | Embed query and doc **separately**, compare vectors | Feed query **and** doc together through a transformer |
| Speed | Very fast (precomputed doc vectors) | Slow (one forward pass per pair) |
| Accuracy | Good | Significantly better - sees word-level interactions |
| Use | First-stage retrieval over millions | Second-stage over top 20-100 candidates |

The standard pattern: **retrieve 50 cheaply, rerank to the best 5 precisely.**

```python
from sentence_transformers import CrossEncoder

reranker = CrossEncoder("cross-encoder/ms-marco-MiniLM-L-6-v2")

def rerank(query: str, candidates: list[dict], top_n: int = 5) -> list[dict]:
    pairs = [(query, c["text"]) for c in candidates]
    scores = reranker.predict(pairs)
    for c, s in zip(candidates, scores):
        c["rerank_score"] = float(s)
    return sorted(candidates, key=lambda c: c["rerank_score"], reverse=True)[:top_n]

candidates = [{"id": d["id"], "text": d["text"]} for d in corpus]
for c in rerank("why did my payment fail with ERR-4032", candidates, top_n=2):
    print(f"{c['rerank_score']:.2f}  {c['text'][:70]}")
```

### Reranker Options

| Option | Notes |
|---|---|
| `cross-encoder/ms-marco-MiniLM-L-6-v2` | Small, fast, English |
| `BAAI/bge-reranker-v2-m3` | Strong multilingual open model |
| Cohere Rerank, Voyage rerank, Jina reranker | Hosted APIs |
| LLM as reranker | Ask an LLM to score/order candidates - flexible but expensive and slower |

Rerank scores also make a better **relevance threshold** than raw cosine similarity for deciding "not found".

---

## Part 4: Query Transformation

Users write short, vague, or multi-part questions. Documents are written differently. Query transformation bridges the gap.

### Query Rewriting

Covered in Module 6 for conversation history; also useful to fix typos and expand acronyms.

### Multi-Query Retrieval

Generate several paraphrases, retrieve for each, and fuse the results. Improves recall when vocabulary varies.

```python
from openai import OpenAI
from pydantic import BaseModel

oai = OpenAI()

class QueryVariants(BaseModel):
    queries: list[str]

def generate_queries(question: str, n: int = 4) -> list[str]:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini",
        temperature=0.4,
        messages=[{"role": "user", "content": f"Write {n} different search queries that would find documents answering:\n{question}"}],
        response_format=QueryVariants,
    )
    return [question, *r.choices[0].message.parsed.queries]

def multi_query_search(question: str, k: int = 5) -> list[str]:
    result_lists = [hybrid_search(q, 20) for q in generate_queries(question)]
    return [doc_id for doc_id, _ in reciprocal_rank_fusion(result_lists)[:k]]
```

### HyDE (Hypothetical Document Embeddings)

Ask the LLM to write a **hypothetical answer**, then embed that answer and search. The fake answer looks like a real document, so it lands closer to real documents in embedding space than a short question does.

```python
def hyde_search(question: str, k: int = 5) -> list[str]:
    hypothetical = oai.chat.completions.create(
        model="gpt-4o-mini",
        temperature=0,
        messages=[{"role": "user", "content": f"Write a short passage from technical documentation that answers: {question}"}],
    ).choices[0].message.content
    q = encoder.encode(hypothetical, normalize_embeddings=True)
    order = np.argsort(-(doc_vectors @ q))[:k]
    return [corpus[i]["id"] for i in order]
```

**Caution:** HyDE can mislead retrieval when the model's hypothetical answer is confidently wrong about domain-specific facts. Evaluate before adopting.

### Query Decomposition

Break complex questions into sub-questions, answer each with retrieval, then synthesize.

```python
class SubQuestions(BaseModel):
    sub_questions: list[str]

def decompose(question: str) -> list[str]:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini",
        temperature=0,
        messages=[{"role": "user", "content": f"Break this question into the minimal set of independent sub-questions needed to answer it:\n{question}"}],
        response_format=SubQuestions,
    )
    return r.choices[0].message.parsed.sub_questions

# "Compare the refund policy and SLA of the Pro and Enterprise plans"
# -> ["What is the refund policy of the Pro plan?", "What is the refund policy of the Enterprise plan?",
#     "What is the SLA of the Pro plan?", "What is the SLA of the Enterprise plan?"]
```

### Self-Querying (Metadata Extraction)

Extract structured filters from natural language so retrieval can filter precisely.

```python
from typing import Literal, Optional

class SearchFilters(BaseModel):
    semantic_query: str
    product: Optional[Literal["payments", "billing", "identity"]] = None
    year: Optional[int] = None
    doc_type: Optional[Literal["policy", "runbook", "release-notes"]] = None

def extract_filters(question: str) -> SearchFilters:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini", temperature=0,
        messages=[{"role": "user", "content": f"Extract search filters from: {question}"}],
        response_format=SearchFilters,
    )
    return r.choices[0].message.parsed

# "What changed in the payments release notes in 2024?"
# -> semantic_query="changes in release", product="payments", year=2024, doc_type="release-notes"
```

---

## Part 5: Advanced Chunking and Indexing

### Parent-Child (Small-to-Big) Retrieval

Embed **small** chunks for precise matching, but return the **larger parent** section to the LLM for context.

```python
def build_parent_child(doc_id: str, text: str, parent_tokens: int = 1500, child_tokens: int = 250):
    parents, children = {}, []
    for p_idx, parent in enumerate(recursive_split(text, parent_tokens)):
        parent_id = f"{doc_id}::p{p_idx}"
        parents[parent_id] = parent
        for c_idx, child in enumerate(recursive_split(parent, child_tokens)):
            children.append({"id": f"{parent_id}::c{c_idx}", "text": child, "parent_id": parent_id})
    return parents, children

def retrieve_parents(query: str, child_index, parents: dict, k: int = 8, max_parents: int = 3) -> list[str]:
    child_hits = child_index.search(query, k)            # search over small chunks
    seen, results = set(), []
    for hit in child_hits:
        pid = hit["parent_id"]
        if pid not in seen:
            seen.add(pid)
            results.append(parents[pid])                 # return the big parent section
        if len(results) == max_parents:
            break
    return results
```

A variant is **sentence-window retrieval**: embed single sentences, return the sentence plus N neighbors.

### Contextual Retrieval

Before embedding, use an LLM to write a 1-2 sentence context for each chunk describing where it sits in the document, and prepend it. This dramatically reduces failures where a chunk is ambiguous on its own ("The limit was raised to 500").

```python
CONTEXT_PROMPT = """<document>
{document}
</document>
Here is a chunk from the document:
<chunk>
{chunk}
</chunk>
Write one or two sentences situating this chunk within the overall document to improve search retrieval.
Answer only with the context."""

def contextualize_chunk(document: str, chunk: str) -> str:
    context = oai.chat.completions.create(
        model="gpt-4o-mini",
        temperature=0,
        messages=[{"role": "user", "content": CONTEXT_PROMPT.format(document=document, chunk=chunk)}],
        max_tokens=100,
    ).choices[0].message.content
    return f"{context}\n\n{chunk}"
```

Index the contextualized text in **both** the vector index and the BM25 index. Provider prompt caching makes sending the same document for every chunk affordable.

### Multi-Representation Indexing

Index different representations that all point to the same source:
- **Summaries** of each document or section (good for broad questions)
- **Hypothetical questions** each chunk answers (good match for user questions)
- **Raw chunks** (good for detail)

```python
class ChunkQuestions(BaseModel):
    questions: list[str]

def questions_for_chunk(chunk: str) -> list[str]:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini", temperature=0,
        messages=[{"role": "user", "content": f"Write 3 questions a user might ask that this text answers:\n{chunk}"}],
        response_format=ChunkQuestions,
    )
    return r.choices[0].message.parsed.questions
# Embed each question with metadata {"points_to": chunk_id}; at query time return the original chunk.
```

### Hierarchical / RAPTOR-Style Indexing

Recursively cluster chunks and summarize each cluster, building a tree of summaries. Queries can match detailed leaves or high-level summaries, which helps questions like "What are the main themes across these reports?"

---

## Part 6: Graph RAG

### When Vector RAG Fails

- **Multi-hop questions:** "Which customers are affected by vulnerabilities in libraries used by the payments service?" requires following relationships across documents
- **Global questions:** "What are the top recurring issues across all incident reports this year?" - no single chunk contains the answer
- **Entity-centric questions:** "Everything we know about Vendor X"

### Knowledge Graph Basics

A knowledge graph stores **entities** (nodes) and **relationships** (edges): `(payments-service) -[USES]-> (log4j 2.14)`, `(log4j 2.14) -[HAS_VULNERABILITY]-> (CVE-2021-44228)`.

### Building a Graph with an LLM

```python
# pip install networkx
import networkx as nx

class Triple(BaseModel):
    subject: str
    relation: str
    object: str

class Triples(BaseModel):
    triples: list[Triple]

def extract_triples(text: str) -> list[Triple]:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini", temperature=0,
        messages=[{
            "role": "system",
            "content": "Extract knowledge graph triples. Use canonical entity names. "
                       "Relations in UPPER_SNAKE_CASE such as USES, OWNS, DEPENDS_ON, HAS_VULNERABILITY.",
        }, {"role": "user", "content": text}],
        response_format=Triples,
    )
    return r.choices[0].message.parsed.triples

graph = nx.MultiDiGraph()
documents = {
    "arch-1": "The payments-service uses log4j 2.14 and depends on the fraud-service.",
    "sec-7": "log4j 2.14 has vulnerability CVE-2021-44228, rated critical.",
    "cust-3": "Acme Corp and Globex use the payments-service for checkout.",
}
for doc_id, text in documents.items():
    for t in extract_triples(text):
        graph.add_edge(t.subject.lower(), t.object.lower(), relation=t.relation, source=doc_id)

def neighborhood(entity: str, hops: int = 2) -> list[str]:
    entity = entity.lower()
    if entity not in graph:
        return []
    nodes = nx.single_source_shortest_path_length(graph.to_undirected(), entity, cutoff=hops).keys()
    facts = []
    for u, v, data in graph.subgraph(nodes).edges(data=True):
        facts.append(f"{u} -[{data['relation']}]-> {v}  (source: {data['source']})")
    return facts

print("\n".join(neighborhood("CVE-2021-44228", hops=3)))
# Links the CVE -> log4j -> payments-service -> Acme Corp / Globex
```

The facts from the graph neighborhood are passed to the LLM as context alongside (or instead of) vector-retrieved chunks.

### Graph RAG Approaches

| Approach | Description |
|---|---|
| **Entity-linked retrieval** | Extract entities from the query, fetch their graph neighborhood, add to context |
| **Text-to-Cypher** | LLM translates the question into a graph query (Neo4j Cypher) and executes it |
| **Community summaries (Microsoft GraphRAG)** | Detect communities of related entities, pre-summarize each; answer global questions by map-reducing over community summaries |
| **Hybrid graph + vector** | Vector search finds entry chunks, graph expands to related entities |

### Trade-offs

| Pro | Con |
|---|---|
| Multi-hop and relationship reasoning | Expensive LLM-based extraction at indexing |
| Global/thematic questions | Entity resolution is hard ("IBM" vs "International Business Machines") |
| Explainable paths | Graph maintenance as documents change |
| Structured querying | More infrastructure (Neo4j, graph pipelines) |

Use Graph RAG when your questions are genuinely relational or global; vector + hybrid + reranking is sufficient for most FAQ/document Q&A.

---

## Part 7: Self-Correcting and Agentic RAG

### Corrective RAG (CRAG) Pattern

Grade retrieved documents; if they are not relevant, rewrite the query or use another source (web search, a different index) before generating.

```python
class RelevanceGrade(BaseModel):
    relevant: bool

def grade(question: str, chunk: str) -> bool:
    r = oai.beta.chat.completions.parse(
        model="gpt-4o-mini", temperature=0,
        messages=[{"role": "user", "content": f"Does this text help answer the question?\nQuestion: {question}\nText: {chunk}"}],
        response_format=RelevanceGrade,
    )
    return r.choices[0].message.parsed.relevant

def corrective_rag(question: str, search, max_rounds: int = 2) -> list[str]:
    query = question
    for _ in range(max_rounds):
        chunks = search(query)
        relevant = [c for c in chunks if grade(question, c)]
        if len(relevant) >= 2:
            return relevant
        query = oai.chat.completions.create(
            model="gpt-4o-mini", temperature=0,
            messages=[{"role": "user", "content": f"The search '{query}' found nothing useful. Write a better search query for: {question}"}],
        ).choices[0].message.content
    return relevant  # possibly empty -> answer "not found"
```

### Self-RAG and Answer Verification

After generating, check whether the answer is grounded (faithfulness judge from Module 6). If not, regenerate with stricter instructions or return "not found".

### Agentic RAG

Give an agent **retrieval tools** and let it decide what to search, which index to use, and when it has enough information.

```python
tools = [
    {"type": "function", "function": {
        "name": "search_docs",
        "description": "Search product documentation. Use for how-to and feature questions.",
        "parameters": {"type": "object", "properties": {"query": {"type": "string"}}, "required": ["query"]},
    }},
    {"type": "function", "function": {
        "name": "search_tickets",
        "description": "Search resolved support tickets. Use for error messages and known issues.",
        "parameters": {"type": "object", "properties": {"query": {"type": "string"}}, "required": ["query"]},
    }},
]
# The agent loop from Module 3/8 lets the model call these repeatedly, refine queries,
# combine evidence from both indexes, and stop when it can answer with citations.
```

**Trade-off:** agentic RAG handles complex questions better but is slower, costlier, and less predictable. Use routing: simple questions go to the fast pipeline; complex ones go to the agent.

---

## Part 8: Context Optimization

### Lost in the Middle

LLMs attend best to information at the **beginning and end** of the context. Place the most relevant chunks first (or first and last), and avoid sending 30 marginal chunks.

### Context Compression

- **Deduplicate** near-identical chunks (similarity > 0.95)
- **Extractive compression**: keep only sentences relevant to the query
- **LLM compression**: summarize each chunk relative to the question (costly)
- **Token budget**: fill context by rerank score until a budget is reached

```python
def build_budgeted_context(chunks: list[dict], budget_tokens: int = 3000) -> list[dict]:
    selected, used, seen_texts = [], 0, set()
    for c in sorted(chunks, key=lambda c: c["rerank_score"], reverse=True):
        key = c["text"][:200]
        if key in seen_texts:
            continue
        cost = token_len(c["text"])
        if used + cost > budget_tokens:
            continue
        selected.append(c)
        used += cost
        seen_texts.add(key)
    return selected
```

---

## Part 9: Putting It Together

```python
def advanced_rag_answer(question: str, user: dict) -> dict:
    filters = extract_filters(question)                                   # self-query
    queries = generate_queries(filters.semantic_query, n=3)               # multi-query
    where = {"tenant_id": user["tenant_id"]}                              # security filter
    if filters.product:
        where = {"$and": [where, {"product": filters.product}]}

    result_lists = [hybrid_search_with_filter(q, where, k=30) for q in queries]   # hybrid retrieval
    fused_ids = [doc_id for doc_id, _ in reciprocal_rank_fusion(result_lists)[:40]]
    candidates = fetch_chunks(fused_ids)
    top = rerank(question, candidates, top_n=8)                           # cross-encoder rerank
    top = [c for c in top if c["rerank_score"] > 0.0]                     # relevance threshold
    if not top:
        return {"answer": "I couldn't find this in the documentation.", "sources": []}

    context_chunks = build_budgeted_context(expand_to_parents(top), 3500) # parent expansion + budget
    answer = generate_with_citations(question, context_chunks)            # grounded generation
    if not judge_faithfulness(answer["answer"], build_context(context_chunks)).faithful:
        answer = generate_with_citations(question, context_chunks, strict=True)  # self-correction
    return answer
```

Measure the impact of each stage with your evaluation set. A typical result is that hybrid search + reranking + good chunking deliver most of the gains; the more expensive techniques address specific residual failure categories.

---

## Summary

| Technique | Key Idea |
|---|---|
| Hybrid search | Dense + BM25 fused with RRF |
| Reranking | Cross-encoder rescoring of top candidates |
| Multi-query / HyDE / decomposition | Transform queries to match how documents are written |
| Self-querying | Extract structured filters from natural language |
| Parent-child | Match small, return big |
| Contextual retrieval | Prepend LLM-written context before indexing |
| Graph RAG | Entities and relationships for multi-hop and global questions |
| Corrective / agentic RAG | Grade, retry, and let agents drive retrieval |
| Context optimization | Order, dedupe, compress, budget |

---

## Interview Questions

- **[L1]** What is hybrid search and why does it outperform pure vector search for many enterprise queries?
- **[L1]** What does a reranker do, and why is it used after initial retrieval rather than instead of it?
- **[L2]** Explain Reciprocal Rank Fusion and why it is preferred over adding raw scores from different retrievers.
- **[L2]** Compare multi-query retrieval, HyDE, and query decomposition. Which problems does each solve?
- **[L2]** What is parent-child (small-to-big) retrieval and what trade-off does it address?
- **[L3]** When is Graph RAG worth its cost compared with vector RAG? Describe how you would build one.
- **[L3]** Design a RAG pipeline that must answer with over 90% accuracy under a 2-second latency budget. Which advanced techniques do you choose and why?
- **[L3]** What is agentic RAG, what are its risks, and how would you decide which queries should use it?

## Interview Answers

1. Hybrid search runs dense vector retrieval and sparse keyword retrieval (BM25 or learned sparse models) in parallel and merges the results. Dense retrieval captures meaning and paraphrases but is weak at exact identifiers, error codes, product names, acronyms, and rare terms; keyword search excels at those but misses synonyms. Enterprise queries often mix both ("ERR-4032 payment failure"), so combining them improves recall and robustness across query types.
2. A reranker, usually a cross-encoder, takes the query and a candidate passage together and outputs a precise relevance score, capturing word-level interactions that bi-encoder embeddings miss. It is too slow to run over millions of documents because every pair requires a full transformer pass, so the standard pattern is to retrieve 20-100 candidates cheaply with vector/hybrid search and rerank them to the top 3-10. This improves precision of what reaches the LLM and provides a better relevance threshold.
3. RRF scores each document as the sum of `1/(k + rank)` over each result list, with k usually 60. Different retrievers produce scores on incomparable scales (BM25 is unbounded, cosine is -1 to 1) and distributions vary per query, so adding raw or naively normalized scores is unreliable. RRF only uses ranks, needs no calibration or tuning, rewards documents that rank highly in multiple lists, and is robust, which is why it is the default fusion method in many search engines.
4. **Multi-query** generates several paraphrases and fuses results, improving recall when users' vocabulary differs from documents. **HyDE** generates a hypothetical answer and embeds it, helping when short questions poorly match long, document-style passages, though it can mislead if the hypothetical answer is wrong. **Decomposition** splits complex, multi-part, or comparison questions into sub-questions, retrieves for each, and synthesizes, solving questions that no single chunk answers. All add latency and cost, so they should be applied based on measured failure types or query classification.
5. Parent-child retrieval indexes small child chunks (sentences or ~200 tokens) for precise embedding matches, but returns their larger parent section (~1000-2000 tokens) to the LLM. It addresses the trade-off between retrieval precision (small chunks embed a single topic cleanly) and generation context (large chunks contain surrounding details, definitions, and conditions needed to answer correctly). Deduplicating parents and applying a token budget prevents context bloat.
6. Graph RAG is worth it when questions are relational or multi-hop (dependencies, ownership, impact analysis), entity-centric, or global/thematic across a large corpus, and when explainable reasoning paths matter. It is not worth it for typical FAQ or document lookups where hybrid search and reranking suffice. To build one: extract entities and relationships from documents with an LLM using a controlled schema, perform entity resolution and deduplication, store in a graph database like Neo4j with source references, optionally detect communities and pre-summarize them for global questions, and at query time extract query entities, traverse neighborhoods or generate Cypher queries, combine graph facts with vector-retrieved text, and generate a cited answer. Plan for incremental updates and evaluation of extraction quality.
7. Start with strong fundamentals: good parsing, structure-aware chunks with contextual headers (contextual retrieval done at indexing time, so no query latency), and metadata filters. At query time use hybrid search (dense + BM25 in parallel, ~50-100 ms) fused with RRF, then a small fast cross-encoder reranker on the top ~30 candidates (~100-200 ms on GPU), parent expansion, and a token-budgeted context. Avoid per-query LLM-heavy steps like HyDE or multi-query on the hot path; use a small model for conversational query rewriting only when there is history. Generate with a fast model and streaming so perceived latency is low. Use a routing classifier to send only complex queries to slower paths. Validate the 90% target with an evaluation set and track p95 latency per stage.
8. Agentic RAG gives an LLM agent retrieval tools (multiple indexes, web search, SQL) and lets it plan searches, refine queries, and decide when it has enough evidence. It handles complex, multi-step, and ambiguous questions better than a fixed pipeline. Risks include higher and unpredictable latency and cost, infinite or redundant loops, harder testing and debugging, tool misuse, and prompt injection from retrieved content steering the agent. Mitigate with step and token limits, tracing, restricted read-only tools, and evaluation. Decide routing with a lightweight classifier or heuristics: simple factual lookups go to the fast deterministic pipeline, while multi-part, comparative, or low-confidence queries (low rerank scores, failed faithfulness checks) escalate to the agent.
