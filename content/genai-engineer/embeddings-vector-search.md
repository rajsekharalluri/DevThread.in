---
id: genai-eng-embeddings-vector-search
slug: embeddings-vector-search
title: "Module 5: Embeddings & Vector Search"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: intermediate
estimatedMinutes: 90
version:
  minimum: "sentence-transformers 3.x, OpenAI SDK 1.x, pgvector 0.7+"
prerequisites: [genai-eng-llm-app-development]
tags: [genai-engineer, embeddings, vector-search, similarity, ann, hnsw, pgvector, chroma, faiss]
relatedTopics: [genai-eng-rag]
order: 5
status: published
---
# Module 5: Embeddings & Vector Search

## Introduction

Keyword search finds documents that contain the same **words**. Semantic search finds documents with the same **meaning**. A search for "how do I reset my password" should match a help article titled "Recovering account access" even though they share no keywords.

Embeddings make this possible. An embedding model converts text (or images, audio, code) into a vector of numbers where similar meanings are close together in space. Vector databases store millions of these vectors and find the nearest ones in milliseconds.

This module covers what embeddings are, how to generate them, similarity metrics, approximate nearest neighbor (ANN) indexes such as HNSW and IVF, vector database options, metadata filtering, and how to evaluate search quality. It is the foundation for RAG in Module 6.

---

## Part 1: What Are Embeddings?

### From Text to Vectors

An embedding is a fixed-length list of floating-point numbers (for example 384, 768, 1536 or 3072 dimensions) that represents the meaning of an input.

```text
"The cat sat on the mat"          -> [0.12, -0.45, 0.33, ..., 0.08]   (384 numbers)
"A kitten was resting on a rug"   -> [0.11, -0.41, 0.35, ..., 0.10]   (very close)
"Quarterly revenue grew 12%"      -> [-0.52, 0.19, -0.07, ..., 0.61]  (far away)
```

Each dimension does not have a human-readable meaning. Meaning is captured by the **relative position** of vectors: distance and direction.

### How Embedding Models Are Trained

Most text embedding models are transformer **encoders** trained with **contrastive learning**:

1. Take pairs of texts that should be similar (question and its answer, title and body, paraphrases)
2. Push their vectors closer together
3. Push vectors of unrelated texts (negatives) further apart

After training on hundreds of millions of pairs, the model places semantically related texts near each other.

### Word Embeddings vs Sentence Embeddings

| Type | Example Models | Output | Use |
|---|---|---|---|
| Word embeddings | Word2Vec, GloVe | One vector per word (context-free) | Historical, NLP features |
| Contextual token embeddings | BERT hidden states | One vector per token, depends on context | Inside models |
| Sentence/document embeddings | Sentence-BERT, OpenAI text-embedding-3, BGE, E5, Cohere embed | One vector per text passage | Semantic search, RAG, clustering |

GenAI engineers almost always work with **sentence/document embeddings**.

---

## Part 2: Generating Embeddings

### Local Open-Source Model (sentence-transformers)

```python
# pip install sentence-transformers
from sentence_transformers import SentenceTransformer

model = SentenceTransformer("all-MiniLM-L6-v2")  # 384 dimensions, fast, runs on CPU

sentences = [
    "How do I reset my password?",
    "Steps to recover access to your account",
    "Our office is closed on public holidays",
]

embeddings = model.encode(sentences, normalize_embeddings=True)
print(embeddings.shape)  # (3, 384)

similarity = model.similarity(embeddings, embeddings)
print(similarity)
# Row 0 vs row 1 is high (~0.6+), row 0 vs row 2 is low (~0.0-0.1)
```

### Hosted API (OpenAI)

```python
from openai import OpenAI
import numpy as np

client = OpenAI()

response = client.embeddings.create(
    model="text-embedding-3-small",   # 1536 dims by default
    input=["How do I reset my password?", "Steps to recover access to your account"],
)

vectors = np.array([item.embedding for item in response.data])
print(vectors.shape)  # (2, 1536)

# text-embedding-3 models support shortening dimensions to save storage
small = client.embeddings.create(model="text-embedding-3-small", input="hello", dimensions=512)
print(len(small.data[0].embedding))  # 512
```

### Choosing an Embedding Model

| Factor | Question to Ask |
|---|---|
| **Quality** | How does it rank on MTEB (Massive Text Embedding Benchmark) for *retrieval* tasks in your language? |
| **Dimensions** | Higher dims = more storage and memory. 384-1024 is often enough |
| **Max input length** | 512 tokens (many open models) vs 8191 tokens (OpenAI) |
| **Language** | Multilingual models (e.g. multilingual-e5, bge-m3) for non-English content |
| **Domain** | Code, legal, medical content may benefit from domain models or fine-tuning |
| **Hosting** | API (easy, per-token cost, data leaves your network) vs self-hosted (control, GPU cost) |
| **Latency and cost** | Embedding millions of chunks can cost real money; local models are free after hardware |

**Critical rule:** queries and documents must be embedded with the **same model** (and same version). Changing the model means re-embedding the entire corpus.

### Query vs Document Prefixes

Some models (E5, BGE, Nomic) are trained with instruction prefixes. Using them improves retrieval.

```python
model = SentenceTransformer("intfloat/e5-base-v2")
query_vec = model.encode("query: how to rotate api keys", normalize_embeddings=True)
doc_vecs = model.encode(["passage: API keys can be rotated from the security settings page..."], normalize_embeddings=True)
```

Always check the model card for the expected format.

### Batching Embedding Calls

```python
def embed_in_batches(texts: list[str], batch_size: int = 256) -> list[list[float]]:
    all_vectors = []
    for i in range(0, len(texts), batch_size):
        batch = texts[i:i + batch_size]
        resp = client.embeddings.create(model="text-embedding-3-small", input=batch)
        all_vectors.extend(item.embedding for item in resp.data)
    return all_vectors
```

---

## Part 3: Similarity Metrics

| Metric | Formula | Range | Notes |
|---|---|---|---|
| **Cosine similarity** | `a·b / (‖a‖‖b‖)` | -1 to 1 | Ignores magnitude; the default for text |
| **Dot product** | `a·b` | unbounded | Equals cosine when vectors are normalized; fastest |
| **Euclidean (L2) distance** | `‖a - b‖` | 0 to ∞ | Smaller = more similar; ranking equals cosine for normalized vectors |

```python
import numpy as np

a = np.array([0.2, 0.8, 0.1])
b = np.array([0.25, 0.75, 0.05])

cosine = a @ b / (np.linalg.norm(a) * np.linalg.norm(b))
dot = a @ b
l2 = np.linalg.norm(a - b)
print(f"cosine={cosine:.4f} dot={dot:.4f} l2={l2:.4f}")

# Normalize once, then dot product == cosine similarity
a_n, b_n = a / np.linalg.norm(a), b / np.linalg.norm(b)
print(np.isclose(a_n @ b_n, cosine))  # True
```

**Practical rule:** normalize embeddings at write time and use dot product (inner product) - it is the fastest and gives cosine ranking. Use whichever metric the embedding model was trained with (documented on the model card).

---

## Part 4: Exact vs Approximate Nearest Neighbor Search

### Brute-Force (Exact) Search

Compare the query to every vector. Perfect recall, but cost grows linearly: O(N x d).

```python
import numpy as np

def top_k_exact(query: np.ndarray, matrix: np.ndarray, k: int = 5) -> list[tuple[int, float]]:
    scores = matrix @ query  # vectors pre-normalized
    idx = np.argpartition(-scores, k)[:k]
    idx = idx[np.argsort(-scores[idx])]
    return [(int(i), float(scores[i])) for i in idx]

docs = np.random.randn(100_000, 384).astype("float32")
docs /= np.linalg.norm(docs, axis=1, keepdims=True)
q = docs[42] + 0.05 * np.random.randn(384).astype("float32")
q /= np.linalg.norm(q)
print(top_k_exact(q, docs, k=3))  # index 42 should be first
```

Brute force is fine up to roughly 100K-1M vectors on modern hardware. Beyond that, or with strict latency budgets, use **ANN indexes**.

### Approximate Nearest Neighbor (ANN) Indexes

ANN trades a small amount of recall (for example 95-99%) for massive speedups.

#### HNSW (Hierarchical Navigable Small World)

A multi-layer graph. Top layers have few nodes with long-range links for coarse navigation; lower layers are dense for fine search. Search starts at the top and greedily descends.

| Parameter | Meaning | Effect of Increasing |
|---|---|---|
| `M` | Links per node | Better recall, more memory |
| `ef_construction` | Candidate list size while building | Better index quality, slower build |
| `ef_search` | Candidate list size while querying | Better recall, slower queries |

HNSW is the default in most vector databases: excellent recall and latency, but memory-hungry and slower to build.

#### IVF (Inverted File Index)

Cluster vectors into `nlist` centroids with k-means. At query time, only search the `nprobe` closest clusters.

- Faster build, less memory than HNSW
- Recall depends on `nprobe`
- Needs training data to build centroids

#### Product Quantization (PQ) and Scalar/Binary Quantization

Compress vectors to reduce memory (for example float32 to int8 = 4x smaller, binary = 32x smaller). Often combined as IVF-PQ. Some recall loss, usually recovered by **re-scoring** top candidates with full-precision vectors.

### FAISS Example

```python
# pip install faiss-cpu
import faiss
import numpy as np

d = 384
xb = np.random.randn(200_000, d).astype("float32")
faiss.normalize_L2(xb)

# Exact inner-product index
flat = faiss.IndexFlatIP(d)
flat.add(xb)

# HNSW index
hnsw = faiss.IndexHNSWFlat(d, 32, faiss.METRIC_INNER_PRODUCT)
hnsw.hnsw.efConstruction = 200
hnsw.add(xb)
hnsw.hnsw.efSearch = 64

xq = xb[:5].copy()
D_exact, I_exact = flat.search(xq, 10)
D_hnsw, I_hnsw = hnsw.search(xq, 10)

recall = np.mean([len(set(I_exact[i]) & set(I_hnsw[i])) / 10 for i in range(5)])
print(f"HNSW recall@10 vs exact: {recall:.2f}")
```

---

## Part 5: Vector Databases

### What a Vector Database Adds Beyond an Index

- Persistence, replication, backups
- CRUD operations on vectors (updates and deletes)
- **Metadata storage and filtering** (tenant, date, document type, permissions)
- Hybrid search (vector + keyword)
- Horizontal scaling, sharding, access control

### Options Landscape

| Option | Type | Good For |
|---|---|---|
| **pgvector** (PostgreSQL) | Extension | Teams already on Postgres; transactions + joins + vectors in one DB |
| **Chroma** | Embedded/server | Prototyping, local development |
| **Qdrant** | Dedicated, open source | Strong filtering, Rust performance, self-host or cloud |
| **Weaviate** | Dedicated, open source | Built-in hybrid search and modules |
| **Milvus / Zilliz** | Dedicated, open source | Very large scale (billions of vectors) |
| **Pinecone** | Managed SaaS | Zero-ops serverless vector search |
| **Elasticsearch / OpenSearch** | Search engine with kNN | Existing search stacks, strong keyword + vector hybrid |
| **Azure AI Search / Vertex AI Vector Search / Amazon OpenSearch** | Cloud managed | Cloud-native enterprise deployments |
| **Redis** | In-memory with vector module | Low-latency, semantic caching |

**Default recommendation:** start with pgvector if you already use Postgres, or Chroma for prototypes. Move to a dedicated engine when scale, filtering performance, or operational needs demand it.

### Chroma Example

```python
# pip install chromadb
import chromadb
from chromadb.utils import embedding_functions

client = chromadb.PersistentClient(path="./chroma_db")
embed_fn = embedding_functions.SentenceTransformerEmbeddingFunction(model_name="all-MiniLM-L6-v2")

collection = client.get_or_create_collection(
    name="help_articles",
    embedding_function=embed_fn,
    metadata={"hnsw:space": "cosine"},
)

collection.add(
    ids=["a1", "a2", "a3"],
    documents=[
        "To reset your password, open Settings > Security and click Reset.",
        "Invoices are generated on the 1st of every month and emailed to the billing contact.",
        "Two-factor authentication can be enabled from the Security page.",
    ],
    metadatas=[{"category": "account"}, {"category": "billing"}, {"category": "account"}],
)

results = collection.query(
    query_texts=["I forgot my login credentials"],
    n_results=2,
    where={"category": "account"},  # metadata filter
)
for doc, dist in zip(results["documents"][0], results["distances"][0]):
    print(f"{dist:.3f}  {doc}")
```

### pgvector Example

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE document_chunks (
    id          BIGSERIAL PRIMARY KEY,
    tenant_id   UUID NOT NULL,
    doc_id      TEXT NOT NULL,
    content     TEXT NOT NULL,
    metadata    JSONB DEFAULT '{}',
    embedding   VECTOR(1536) NOT NULL
);

-- HNSW index using cosine distance
CREATE INDEX ON document_chunks USING hnsw (embedding vector_cosine_ops) WITH (m = 16, ef_construction = 64);
CREATE INDEX ON document_chunks (tenant_id);

-- Query: top 5 most similar chunks for one tenant
-- <=> is cosine distance, <-> is L2 distance, <#> is negative inner product
SET hnsw.ef_search = 100;
SELECT id, content, 1 - (embedding <=> $1) AS similarity
FROM document_chunks
WHERE tenant_id = $2
ORDER BY embedding <=> $1
LIMIT 5;
```

```python
# pip install psycopg[binary] pgvector
import psycopg
from pgvector.psycopg import register_vector
import numpy as np

conn = psycopg.connect("postgresql://app:app@localhost:5432/ai")
register_vector(conn)

query_vec = np.array(client_embed("reset my password"), dtype=np.float32)  # your embedding function
rows = conn.execute(
    "SELECT content, 1 - (embedding <=> %s) AS sim FROM document_chunks WHERE tenant_id = %s ORDER BY embedding <=> %s LIMIT 5",
    (query_vec, tenant_id, query_vec),
).fetchall()
```

### Metadata Filtering: Pre-filter vs Post-filter

| Approach | How | Risk |
|---|---|---|
| **Post-filter** | ANN search top-k, then remove non-matching | May return fewer than k results (or zero) when filters are selective |
| **Pre-filter** | Restrict candidates first, then search | Accurate, but can be slow if implemented as brute force |
| **Filtered ANN (in-graph filtering)** | Engine applies filters during graph traversal | Best of both; supported by Qdrant, Weaviate, Milvus, pgvector iterative scans |

For multi-tenant systems, **tenant isolation** is a security requirement, not a relevance tweak. Enforce tenant filters in server-side code or use separate collections/partitions per tenant.

---

## Part 6: Semantic Search Applications Beyond RAG

| Application | How Embeddings Are Used |
|---|---|
| Semantic search | Nearest documents to a query |
| Recommendations | Items near items a user liked |
| Deduplication | Pairs with similarity above threshold (e.g. 0.95) |
| Clustering / topic discovery | K-means or HDBSCAN on embeddings |
| Classification | Nearest labeled examples, or train a small classifier on embeddings |
| Anomaly detection | Items far from all clusters |
| Semantic caching | Reuse LLM answers for near-duplicate questions |

### Zero-Training Classifier with Embeddings

```python
from sentence_transformers import SentenceTransformer
import numpy as np

model = SentenceTransformer("all-MiniLM-L6-v2")

labels = {
    "billing": ["I was charged twice", "refund my payment", "invoice is wrong"],
    "technical": ["app crashes on start", "error 500 when saving", "page does not load"],
    "account": ["change my email", "reset password", "delete my account"],
}

centroids = {
    name: model.encode(examples, normalize_embeddings=True).mean(axis=0)
    for name, examples in labels.items()
}

def classify(text: str) -> str:
    v = model.encode(text, normalize_embeddings=True)
    return max(centroids, key=lambda name: float(v @ centroids[name]))

print(classify("my card got billed two times"))  # billing
```

### Semantic Cache

```python
SIM_THRESHOLD = 0.92
cache_vectors: list[np.ndarray] = []
cache_answers: list[str] = []

def semantic_cache_lookup(question: str) -> str | None:
    if not cache_vectors:
        return None
    q = model.encode(question, normalize_embeddings=True)
    scores = np.array(cache_vectors) @ q
    best = int(np.argmax(scores))
    return cache_answers[best] if scores[best] >= SIM_THRESHOLD else None
```

Tune the threshold carefully: too low returns wrong answers for different questions ("cancel my order" vs "track my order").

---

## Part 7: Evaluating Retrieval Quality

You cannot improve what you do not measure. Build a labeled set of queries with their relevant document IDs.

| Metric | Meaning |
|---|---|
| **Recall@k** | Fraction of relevant docs found in the top k |
| **Precision@k** | Fraction of top k that are relevant |
| **MRR (Mean Reciprocal Rank)** | Average of 1/rank of the first relevant result |
| **nDCG@k** | Rewards relevant results appearing higher, supports graded relevance |
| **Hit rate@k** | Fraction of queries with at least one relevant result in top k |

```python
def recall_at_k(retrieved: list[str], relevant: set[str], k: int) -> float:
    return len(set(retrieved[:k]) & relevant) / len(relevant)

def reciprocal_rank(retrieved: list[str], relevant: set[str]) -> float:
    for rank, doc_id in enumerate(retrieved, start=1):
        if doc_id in relevant:
            return 1 / rank
    return 0.0

eval_set = [
    {"query": "reset password", "relevant": {"a1"}},
    {"query": "when are invoices sent", "relevant": {"a2"}},
]

def evaluate(search_fn, k: int = 5):
    recalls, rrs = [], []
    for item in eval_set:
        ids = search_fn(item["query"], k)
        recalls.append(recall_at_k(ids, item["relevant"], k))
        rrs.append(reciprocal_rank(ids, item["relevant"]))
    print(f"Recall@{k}: {np.mean(recalls):.3f}  MRR: {np.mean(rrs):.3f}")
```

Use this harness to compare embedding models, chunk sizes, index parameters, and hybrid search settings with data instead of intuition.

---

## Part 8: Production Considerations

| Concern | Practice |
|---|---|
| **Model versioning** | Store `embedding_model` and version with each vector; re-embed on change using a new index + backfill + switch |
| **Storage sizing** | `vectors x dims x 4 bytes` (float32). 10M x 1536 x 4 = ~61 GB before index overhead |
| **Index memory** | HNSW adds graph overhead; plan RAM accordingly or use quantization / disk-based indexes |
| **Freshness** | Incremental upserts on document change; delete vectors when documents are deleted |
| **Security** | Enforce tenant/ACL filters server-side; embeddings can leak information and should be treated as sensitive data |
| **Latency** | Cache query embeddings, tune `ef_search`/`nprobe`, co-locate DB and app |
| **Observability** | Log queries, top scores, empty-result rates, latency percentiles |

---

## Summary

| Concept | Key Takeaway |
|---|---|
| Embeddings | Vectors where semantic similarity equals geometric closeness |
| Same model rule | Queries and documents must use the same embedding model |
| Similarity | Normalize + dot product = cosine; use the model's intended metric |
| ANN | HNSW (fast, memory-heavy), IVF (cheaper), quantization (compression) |
| Vector DBs | pgvector, Chroma, Qdrant, Weaviate, Milvus, Pinecone, cloud services |
| Filtering | Filtered ANN; tenant isolation is a security control |
| Evaluation | Recall@k, MRR, nDCG on a labeled query set |

---

## Interview Questions

- **[L1]** What is an embedding, and why are embeddings useful for search?
- **[L1]** What is cosine similarity, and how does it relate to dot product for normalized vectors?
- **[L2]** Why must queries and documents use the same embedding model? What happens operationally when you change models?
- **[L2]** Explain how HNSW works and what the parameters M, ef_construction, and ef_search control.
- **[L2]** Compare pgvector with a dedicated vector database such as Qdrant or Pinecone. When would you choose each?
- **[L3]** Design a multi-tenant semantic search system for 500 enterprise customers with 50 million document chunks. How do you handle isolation, filtering, and scale?
- **[L3]** Your semantic search returns plausible but wrong results for product codes and exact names. Why, and how do you fix it?
- **[L3]** How would you evaluate and choose between three embedding models for a new retrieval system?

## Interview Answers

1. An embedding is a fixed-length numeric vector produced by a model that represents the meaning of text, images, or other data, such that semantically similar inputs map to nearby vectors. This enables semantic search: a query and a document can match by meaning even without shared keywords (e.g. "forgot login" matching "recover account access"). Embeddings also power recommendations, clustering, deduplication, and classification.
2. Cosine similarity measures the cosine of the angle between two vectors: `a·b / (‖a‖‖b‖)`, ranging from -1 to 1, ignoring magnitude. If vectors are normalized to unit length, the denominators equal 1, so cosine similarity equals the dot product. That is why systems normalize embeddings at write time and use inner-product search, which is computationally cheaper and gives identical ranking.
3. Each embedding model defines its own vector space; vectors from different models (or even different versions) are not comparable, so similarity scores between them are meaningless. Changing models requires re-embedding the entire corpus. Operationally: create a new index/collection, backfill embeddings with the new model in a background job, dual-write new documents to both, evaluate retrieval quality on a labeled set, switch reads (possibly via a feature flag), then decommission the old index. Store the model name and version with every vector.
4. HNSW builds a multi-layer proximity graph. Higher layers contain few nodes with long links for coarse navigation; the bottom layer contains all nodes with short links. Search enters at the top layer, greedily moves to the neighbor closest to the query, descends a layer, and repeats until the bottom layer returns the nearest candidates. `M` is the number of links per node (higher gives better recall and more memory). `ef_construction` is the candidate list size during index build (higher gives a better quality graph and slower build). `ef_search` is the candidate list size at query time (higher gives better recall and higher latency); it can be tuned per query.
5. **pgvector** keeps vectors in PostgreSQL alongside relational data, giving transactions, joins, SQL filtering, existing backups, security, and operational familiarity; it is ideal up to tens of millions of vectors for teams already on Postgres. **Dedicated vector databases** (Qdrant, Milvus, Weaviate, Pinecone) offer better performance at very large scale, advanced filtered ANN, built-in hybrid search, quantization, horizontal sharding, and (for Pinecone) fully managed serverless operation. Choose pgvector for simplicity and data consistency; choose a dedicated engine when scale, latency, filtering performance, or specialized features exceed what Postgres handles comfortably.
6. Use a vector database that supports filtered ANN and partitioning. For isolation, either use a partition/namespace per tenant (strong isolation, simple deletes, but many small indexes) or a shared collection with a mandatory tenant_id payload index enforced by server-side code (efficient for many small tenants); large tenants can get dedicated shards. Never rely on client-provided tenant IDs. Add document-level ACL metadata for permission filtering. For 50M chunks at 1024 dims, use int8 or binary quantization with re-scoring to control memory, shard across nodes, and replicate for availability. Pipeline ingestion through a queue with idempotent upserts, track embedding model version, and monitor per-tenant latency and recall. Support tenant deletion (GDPR) by partition drop or filtered delete.
7. Dense embeddings capture general meaning but are weak at exact tokens such as SKUs, error codes, names, and rare terms; similar-looking codes map to nearby vectors, and out-of-vocabulary strings are poorly represented. Fixes: add **hybrid search** combining BM25/keyword search with vector search and fuse results (e.g. Reciprocal Rank Fusion); route queries that look like identifiers to exact-match lookup; store identifiers as metadata for filtering; add a cross-encoder reranker; and consider fine-tuning the embedding model on domain data. Evaluate on a query set that includes exact-match queries.
8. Build a representative labeled evaluation set from real or realistic queries with relevant document IDs (hundreds of queries, including hard cases like acronyms and multilingual text). Embed the same chunked corpus with each model and measure Recall@k, MRR, and nDCG@k, segmented by query type. Also compare operational factors: dimensions and storage cost, max input length, embedding latency and throughput, per-token API cost or GPU cost, language support, licensing, and data residency. Check MTEB retrieval scores only as a starting shortlist since public benchmarks may not reflect your domain. Choose the model with the best quality/cost trade-off and document the decision.
