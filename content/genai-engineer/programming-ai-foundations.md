---
id: genai-eng-programming-ai-foundations
slug: programming-ai-foundations
title: "Module 1: Programming & AI Foundations"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: beginner
estimatedMinutes: 90
version:
  minimum: "Python 3.11+"
prerequisites: []
tags: [genai-engineer, python, machine-learning, apis, numpy, pandas, fastapi, foundations]
relatedTopics: [genai-eng-llm-fundamentals]
order: 1
status: published
---
# Module 1: Programming & AI Foundations

## Introduction

Every GenAI engineer needs a solid foundation in three areas: **Python programming**, **core machine learning concepts**, and **building APIs**. This module covers each area with enough depth that you can confidently move into LLM-specific work.

You do not need a PhD in machine learning. You need to understand how data flows through models, how to manipulate data programmatically, and how to expose AI capabilities through web services. This module builds exactly that foundation.

---

## Part 1: Python for AI Engineering

### Why Python Dominates AI

Python is the default language for AI/ML work because of its ecosystem: NumPy, Pandas, scikit-learn, PyTorch, TensorFlow, Hugging Face, LangChain, and nearly every major AI framework is Python-first. The language's readability and rapid prototyping speed make it ideal for experimentation-heavy AI work.

### Core Python Patterns Used in AI

#### Type Hints and Dataclasses

Modern AI code uses type hints extensively. They make complex data pipelines readable and catch errors early.

```python
from dataclasses import dataclass, field
from typing import Optional

@dataclass
class Document:
    content: str
    metadata: dict[str, str] = field(default_factory=dict)
    embedding: Optional[list[float]] = None
    token_count: int = 0

    def chunk(self, max_tokens: int = 512) -> list["Document"]:
        words = self.content.split()
        chunks = []
        for i in range(0, len(words), max_tokens):
            chunk_text = " ".join(words[i:i + max_tokens])
            chunks.append(Document(
                content=chunk_text,
                metadata={**self.metadata, "chunk_index": str(len(chunks))},
            ))
        return chunks

# Usage
doc = Document(content="A long document about transformers...", metadata={"source": "arxiv"})
chunks = doc.chunk(max_tokens=100)
print(f"Split into {len(chunks)} chunks")
```

**What this does:** Defines a structured document type with optional embedding storage and a chunking method — a pattern you will use constantly in RAG pipelines.

#### Generators and Iterators for Streaming

LLM responses are often streamed token-by-token. Python generators model this naturally.

```python
from typing import Generator

def stream_tokens(text: str, delay: float = 0.0) -> Generator[str, None, None]:
    """Simulate streaming LLM output token by token."""
    tokens = text.split()
    for token in tokens:
        yield token + " "

def process_stream(stream: Generator[str, None, None]) -> str:
    """Consume a token stream and build the full response."""
    full_response = ""
    for token in stream:
        full_response += token
        print(token, end="", flush=True)  # Real-time display
    print()  # Newline at end
    return full_response

# Usage
response = process_stream(stream_tokens("The transformer architecture uses self-attention"))
print(f"\nFull response: {response}")
```

**What this does:** Demonstrates the generator pattern that mirrors how real LLM APIs stream responses. OpenAI, Anthropic, and local models all use this pattern.

#### Async/Await for Concurrent AI Calls

AI applications often call multiple models or services concurrently. Python's asyncio is essential.

```python
import asyncio
from typing import Any

async def call_llm(prompt: str, model: str = "gpt-4") -> dict[str, Any]:
    """Simulate an async LLM API call."""
    await asyncio.sleep(0.5)  # Simulates network latency
    return {"model": model, "response": f"Answer to: {prompt[:50]}..."}

async def parallel_inference(prompts: list[str]) -> list[dict[str, Any]]:
    """Call the LLM for multiple prompts concurrently."""
    tasks = [call_llm(prompt) for prompt in prompts]
    results = await asyncio.gather(*tasks)
    return list(results)

async def main():
    prompts = [
        "Explain transformers in simple terms",
        "What is attention mechanism?",
        "How does tokenization work?",
    ]
    results = await parallel_inference(prompts)
    for r in results:
        print(f"  {r['model']}: {r['response']}")

# Run it
asyncio.run(main())
```

**What this does:** Shows how to make concurrent LLM calls. In production, you might call an embedding model and a generation model simultaneously, or fan out the same prompt to multiple models for comparison.

#### Context Managers for Resource Management

AI pipelines often hold expensive resources (GPU memory, database connections, API sessions).

```python
from contextlib import contextmanager
from typing import Generator

class ModelSession:
    """Represents an expensive model loaded into memory."""
    def __init__(self, model_name: str):
        self.model_name = model_name
        self.loaded = False

    def load(self):
        print(f"Loading {self.model_name} into memory...")
        self.loaded = True

    def predict(self, text: str) -> str:
        if not self.loaded:
            raise RuntimeError("Model not loaded")
        return f"Prediction for: {text[:30]}..."

    def unload(self):
        print(f"Unloading {self.model_name} from memory")
        self.loaded = False

@contextmanager
def model_session(model_name: str) -> Generator[ModelSession, None, None]:
    session = ModelSession(model_name)
    session.load()
    try:
        yield session
    finally:
        session.unload()

# Usage — model is always properly cleaned up
with model_session("sentence-transformers/all-MiniLM-L6-v2") as model:
    result = model.predict("What is machine learning?")
    print(result)
# Model is automatically unloaded here, even if an exception occurred
```

**What this does:** Ensures expensive AI resources are properly released. This pattern is critical when working with GPU-loaded models, vector database connections, or rate-limited API clients.

---

## Part 2: NumPy and Data Manipulation

### Why NumPy Matters for AI

Every embedding is a NumPy array. Every similarity calculation uses NumPy operations. Every model input and output passes through NumPy at some level.

### Essential NumPy Operations for AI

```python
import numpy as np

# --- Embeddings are vectors ---
embedding_1 = np.array([0.1, 0.3, 0.5, 0.7, 0.9])
embedding_2 = np.array([0.2, 0.4, 0.5, 0.6, 0.8])

# --- Cosine similarity (the most common similarity metric in AI) ---
def cosine_similarity(a: np.ndarray, b: np.ndarray) -> float:
    dot_product = np.dot(a, b)
    magnitude_a = np.linalg.norm(a)
    magnitude_b = np.linalg.norm(b)
    if magnitude_a == 0 or magnitude_b == 0:
        return 0.0
    return float(dot_product / (magnitude_a * magnitude_b))

similarity = cosine_similarity(embedding_1, embedding_2)
print(f"Cosine similarity: {similarity:.4f}")  # ~0.9949

# --- Batch similarity: compare one query against many documents ---
document_embeddings = np.random.rand(1000, 384)  # 1000 documents, 384 dimensions
query_embedding = np.random.rand(384)

# Vectorized cosine similarity for all documents at once
norms = np.linalg.norm(document_embeddings, axis=1)
query_norm = np.linalg.norm(query_embedding)
similarities = document_embeddings @ query_embedding / (norms * query_norm)

# Get top 5 most similar documents
top_5_indices = np.argsort(similarities)[-5:][::-1]
print(f"Top 5 document indices: {top_5_indices}")
print(f"Top 5 similarities: {similarities[top_5_indices]}")

# --- Matrix operations (used in attention mechanisms) ---
# Simplified attention: Q @ K^T / sqrt(d_k)
d_k = 64
Q = np.random.rand(10, d_k)  # 10 query tokens
K = np.random.rand(15, d_k)  # 15 key tokens
V = np.random.rand(15, d_k)  # 15 value tokens

attention_scores = Q @ K.T / np.sqrt(d_k)

# Softmax to normalize scores
def softmax(x: np.ndarray) -> np.ndarray:
    exp_x = np.exp(x - np.max(x, axis=-1, keepdims=True))
    return exp_x / np.sum(exp_x, axis=-1, keepdims=True)

attention_weights = softmax(attention_scores)
output = attention_weights @ V
print(f"Attention output shape: {output.shape}")  # (10, 64)
```

**What this does:** Covers the three most important NumPy patterns in AI: computing cosine similarity between embeddings, batch similarity search, and the matrix math behind attention mechanisms.

### Pandas for AI Data Pipelines

```python
import pandas as pd
import numpy as np

# --- Loading and preparing training data ---
data = pd.DataFrame({
    "question": [
        "What is Python?",
        "Explain machine learning",
        "What is a neural network?",
        "How does backpropagation work?",
        "What is gradient descent?",
    ],
    "answer": [
        "Python is a high-level programming language",
        "ML is a subset of AI that learns from data",
        "A neural network is layers of interconnected nodes",
        "Backprop computes gradients by chain rule",
        "Gradient descent minimizes loss by following gradients",
    ],
    "category": ["programming", "ml", "dl", "dl", "ml"],
    "difficulty": [1, 2, 3, 3, 2],
})

# Filter and transform for fine-tuning dataset
training_pairs = data[data["difficulty"] >= 2].apply(
    lambda row: {
        "prompt": f"Question: {row['question']}\nAnswer:",
        "completion": f" {row['answer']}",
    },
    axis=1,
).tolist()

print(f"Training pairs: {len(training_pairs)}")
for pair in training_pairs[:2]:
    print(f"  Prompt: {pair['prompt'][:50]}...")

# --- Computing evaluation metrics ---
results = pd.DataFrame({
    "model": ["gpt-4", "gpt-4", "gpt-3.5", "gpt-3.5", "claude", "claude"],
    "latency_ms": [450, 520, 120, 95, 380, 410],
    "score": [0.92, 0.88, 0.75, 0.71, 0.90, 0.85],
    "tokens_used": [150, 200, 100, 80, 160, 190],
})

summary = results.groupby("model").agg(
    avg_latency=("latency_ms", "mean"),
    avg_score=("score", "mean"),
    total_tokens=("tokens_used", "sum"),
    count=("score", "count"),
).round(3)

print("\nModel comparison:")
print(summary.to_string())
```

**What this does:** Shows how Pandas is used to prepare fine-tuning datasets and aggregate evaluation metrics across model experiments — two tasks you will do repeatedly.

---

## Part 3: Machine Learning Fundamentals for GenAI

You do not need to build ML models from scratch to be a GenAI engineer. But you need to understand these core concepts because they underpin everything LLMs do.

### Supervised vs Unsupervised vs Self-Supervised Learning

| Type | How It Learns | AI/GenAI Example |
|---|---|---|
| **Supervised** | Labeled input-output pairs | Fine-tuning a classifier on labeled reviews |
| **Unsupervised** | Finds patterns in unlabeled data | Clustering documents by topic |
| **Self-Supervised** | Creates its own labels from data | GPT predicting the next token (no human labels needed) |

**Key insight:** LLMs are trained with self-supervised learning. GPT models predict the next token. BERT models predict masked tokens. This is why they can train on internet-scale data without human labeling.

### The Training Pipeline

```text
Raw Data
Preprocessing (cleaning, tokenization)
Model Architecture (transformer)
Loss Function (cross-entropy for next-token prediction)
Optimizer (Adam, AdamW)
Training Loop (forward pass, loss, backward pass, update)
Evaluation (perplexity, accuracy, human evaluation)
```

### Key Concepts You Must Know

#### 1. Loss Functions

The loss function measures how wrong the model's predictions are. For language models, **cross-entropy loss** measures the difference between the predicted probability distribution over tokens and the actual next token.

```python
import numpy as np

def cross_entropy_loss(predicted_probs: np.ndarray, actual_index: int) -> float:
    """
    predicted_probs: probability distribution over vocabulary (sums to 1)
    actual_index: index of the correct next token
    """
    # Add small epsilon to prevent log(0)
    return -np.log(predicted_probs[actual_index] + 1e-10)

# Example: vocabulary of 5 tokens
# Model predicts: [0.1, 0.6, 0.1, 0.15, 0.05]
# Actual next token is index 1
predicted = np.array([0.1, 0.6, 0.1, 0.15, 0.05])
loss = cross_entropy_loss(predicted, actual_index=1)
print(f"Loss: {loss:.4f}")  # ~0.5108 (lower is better)

# If model was more confident about the right answer:
confident = np.array([0.02, 0.92, 0.02, 0.02, 0.02])
loss_confident = cross_entropy_loss(confident, actual_index=1)
print(f"Confident loss: {loss_confident:.4f}")  # ~0.0834 (much lower)
```

#### 2. Gradient Descent and Backpropagation

Gradient descent adjusts model weights to reduce loss. Backpropagation computes which direction to adjust each weight.

```python
import numpy as np

# Simplified gradient descent on a single parameter
learning_rate = 0.1
weight = 5.0  # Start far from optimal
target = 0.0  # Optimal value

print("Gradient descent steps:")
for step in range(10):
    # Loss = (weight - target)^2
    loss = (weight - target) ** 2
    # Gradient of loss with respect to weight = 2 * (weight - target)
    gradient = 2 * (weight - target)
    # Update: move weight in the opposite direction of gradient
    weight = weight - learning_rate * gradient
    print(f"  Step {step}: weight={weight:.4f}, loss={loss:.4f}")
```

**What this does:** Shows the core optimization loop. In real LLMs, the same principle operates across billions of parameters simultaneously.

#### 3. Overfitting vs Underfitting

| Problem | Symptom | GenAI Relevance |
|---|---|---|
| **Overfitting** | Model memorizes training data, fails on new data | Fine-tuned model repeats training examples verbatim |
| **Underfitting** | Model is too simple to capture patterns | Base model gives generic, unhelpful responses |
| **Good fit** | Model generalizes well to unseen data | Fine-tuned model handles novel queries correctly |

#### 4. Train/Validation/Test Split

```python
import numpy as np

data = list(range(1000))  # Your dataset

# Shuffle
np.random.seed(42)
np.random.shuffle(data)

# Split: 70% train, 15% validation, 15% test
train = data[:700]
validation = data[700:850]
test = data[850:]

print(f"Train: {len(train)}, Validation: {len(validation)}, Test: {len(test)}")
# Train: 700, Validation: 150, Test: 150
```

**Why this matters for GenAI:** When fine-tuning models or evaluating RAG pipelines, you need separate data for training, tuning hyperparameters, and final evaluation. Using the same data for all three gives misleading results.

#### 5. Tokenization

Tokenization converts text to numbers that models can process. Modern LLMs use subword tokenization (BPE, WordPiece, SentencePiece).

```python
# Using tiktoken (OpenAI's tokenizer)
# pip install tiktoken
import tiktoken

encoder = tiktoken.encoding_for_model("gpt-4")

text = "Machine learning is transforming software engineering"
tokens = encoder.encode(text)
print(f"Text: {text}")
print(f"Tokens: {tokens}")
print(f"Token count: {len(tokens)}")

# Decode back
decoded = encoder.decode(tokens)
print(f"Decoded: {decoded}")

# See individual token strings
token_strings = [encoder.decode([t]) for t in tokens]
print(f"Token strings: {token_strings}")

# Why this matters: pricing and context limits
long_text = "This is a sample document. " * 100
token_count = len(encoder.encode(long_text))
print(f"\nLong text token count: {token_count}")
print(f"Cost at $0.01/1K tokens: ${token_count * 0.01 / 1000:.4f}")
```

**What this does:** Shows how text becomes tokens. Understanding tokenization is critical because LLM context windows, pricing, and behavior all depend on token counts, not word counts.

---

## Part 4: Building APIs for AI Applications

### Why FastAPI for AI

FastAPI is the standard framework for AI APIs because it supports async natively (essential for concurrent LLM calls), has automatic OpenAPI documentation, and handles streaming responses.

### Complete AI API Example

```python
from fastapi import FastAPI, HTTPException
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field
from typing import AsyncGenerator
import asyncio
import json

app = FastAPI(title="AI Foundation API", version="1.0.0")

# --- Request/Response Models ---

class CompletionRequest(BaseModel):
    prompt: str = Field(..., min_length=1, max_length=4000, description="The input prompt")
    max_tokens: int = Field(default=256, ge=1, le=2048)
    temperature: float = Field(default=0.7, ge=0.0, le=2.0)
    stream: bool = Field(default=False)

class CompletionResponse(BaseModel):
    text: str
    model: str
    tokens_used: int
    finish_reason: str

class SimilarityRequest(BaseModel):
    text_a: str = Field(..., min_length=1)
    text_b: str = Field(..., min_length=1)

class SimilarityResponse(BaseModel):
    similarity: float
    method: str

class HealthResponse(BaseModel):
    status: str
    model_loaded: bool
    version: str

# --- Simulated AI functions ---

async def generate_text(prompt: str, max_tokens: int, temperature: float) -> str:
    """Simulate LLM text generation."""
    await asyncio.sleep(0.3)  # Simulate inference time
    return f"Generated response for: {prompt[:50]}... (temp={temperature})"

async def generate_stream(prompt: str, max_tokens: int) -> AsyncGenerator[str, None]:
    """Simulate streaming LLM output."""
    words = f"This is a streamed response to the prompt about {prompt[:30]}".split()
    for word in words:
        await asyncio.sleep(0.05)  # Simulate token generation time
        yield json.dumps({"token": word + " ", "done": False}) + "\n"
    yield json.dumps({"token": "", "done": True}) + "\n"

async def compute_embedding(text: str) -> list[float]:
    """Simulate embedding computation."""
    await asyncio.sleep(0.1)
    import hashlib
    # Deterministic fake embedding based on text hash
    hash_bytes = hashlib.sha256(text.encode()).digest()
    return [b / 255.0 for b in hash_bytes[:16]]

# --- API Endpoints ---

@app.get("/health", response_model=HealthResponse)
async def health_check():
    return HealthResponse(status="healthy", model_loaded=True, version="1.0.0")

@app.post("/v1/completions", response_model=CompletionResponse)
async def create_completion(request: CompletionRequest):
    if request.stream:
        return StreamingResponse(
            generate_stream(request.prompt, request.max_tokens),
            media_type="application/x-ndjson",
        )
    text = await generate_text(request.prompt, request.max_tokens, request.temperature)
    return CompletionResponse(
        text=text,
        model="foundation-v1",
        tokens_used=len(text.split()),
        finish_reason="stop",
    )

@app.post("/v1/similarity", response_model=SimilarityResponse)
async def compute_similarity(request: SimilarityRequest):
    emb_a = await compute_embedding(request.text_a)
    emb_b = await compute_embedding(request.text_b)
    # Cosine similarity
    import numpy as np
    a, b = np.array(emb_a), np.array(emb_b)
    similarity = float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b)))
    return SimilarityResponse(similarity=round(similarity, 4), method="cosine")

@app.post("/v1/embeddings")
async def create_embeddings(texts: list[str]):
    if not texts:
        raise HTTPException(status_code=400, detail="texts list cannot be empty")
    embeddings = []
    for text in texts:
        emb = await compute_embedding(text)
        embeddings.append(emb)
    return {"embeddings": embeddings, "model": "embedding-v1", "dimensions": len(embeddings[0])}
```

**Run with:** `uvicorn main:app --reload`

**What this covers:**
- Pydantic models for request validation (critical for production AI APIs)
- Streaming responses (how ChatGPT-style UIs work)
- Async endpoints for concurrent model calls
- Health checks (essential for Kubernetes deployments)
- Embedding and similarity endpoints (building blocks for RAG)

### REST API Design Patterns for AI

| Pattern | Use Case | Example |
|---|---|---|
| **Synchronous** | Fast inference (<2s) | Embedding generation, classification |
| **Streaming** | Long generation | Chat completions, document summarization |
| **Async with polling** | Very long tasks (>30s) | Fine-tuning jobs, batch processing |
| **Webhook callback** | Background processing | Document indexing, evaluation runs |

```python
# Async job pattern for long-running AI tasks
from fastapi import BackgroundTasks
from uuid import uuid4

jobs: dict[str, dict] = {}

class JobRequest(BaseModel):
    documents: list[str]
    model: str = "embedding-v1"

@app.post("/v1/jobs")
async def create_job(request: JobRequest, background_tasks: BackgroundTasks):
    job_id = str(uuid4())
    jobs[job_id] = {"status": "processing", "progress": 0, "result": None}
    background_tasks.add_task(process_job, job_id, request.documents)
    return {"job_id": job_id, "status": "processing"}

@app.get("/v1/jobs/{job_id}")
async def get_job(job_id: str):
    if job_id not in jobs:
        raise HTTPException(status_code=404, detail="Job not found")
    return jobs[job_id]

async def process_job(job_id: str, documents: list[str]):
    for i, doc in enumerate(documents):
        await asyncio.sleep(0.5)  # Simulate processing
        jobs[job_id]["progress"] = (i + 1) / len(documents) * 100
    jobs[job_id]["status"] = "completed"
    jobs[job_id]["result"] = f"Processed {len(documents)} documents"
```

---

## Part 5: Environment Setup and Tooling

### Essential Tools for GenAI Development

| Tool | Purpose |
|---|---|
| **Python 3.11+** | Language runtime (3.11+ for performance improvements) |
| **pip / uv** | Package management (uv is significantly faster) |
| **Jupyter / VS Code** | Interactive development and experimentation |
| **Git** | Version control for code and configs |
| **Docker** | Containerize AI services for deployment |
| **tiktoken** | Token counting for OpenAI models |
| **numpy** | Numerical computing, embeddings |
| **pandas** | Data manipulation and analysis |
| **fastapi + uvicorn** | API framework for serving AI |
| **pydantic** | Data validation and serialization |
| **httpx / aiohttp** | Async HTTP clients for API calls |

### Project Structure for AI Applications

```text
my-genai-app/
  src/
    api/              # FastAPI routes
    models/           # Pydantic models
    services/         # Business logic (LLM calls, RAG pipeline)
    embeddings/       # Embedding generation and storage
    prompts/          # Prompt templates
    evaluation/       # Evaluation scripts and metrics
  tests/
  config/
    settings.py       # Environment-based configuration
  data/
    raw/              # Raw documents
    processed/        # Chunked/embedded documents
  scripts/            # One-off scripts (data loading, evaluation)
  Dockerfile
  docker-compose.yml
  pyproject.toml
  .env.example
```

---

## Summary: What You Need Before Moving to LLMs

| Skill | Why It Matters | Minimum Level |
|---|---|---|
| Python type hints | Read and write typed AI code | Use in all function signatures |
| Generators | Stream LLM responses | Write custom generators |
| Async/await | Concurrent API calls | Await multiple LLM calls |
| NumPy arrays | Embeddings, similarity | Cosine similarity, matrix ops |
| Pandas DataFrames | Data prep, evaluation | Filter, group, aggregate |
| Loss functions | Understand training | Explain cross-entropy |
| Tokenization | Context windows, pricing | Count tokens, estimate cost |
| FastAPI | Serve AI capabilities | Build async API with streaming |
| Pydantic | Validate AI inputs/outputs | Define request/response models |

---

## Interview Questions

- **[L1]** What is the difference between supervised, unsupervised, and self-supervised learning? Which one do LLMs use?
- **[L1]** Why is Python the dominant language for AI engineering, and what are its key libraries?
- **[L2]** Explain cosine similarity and why it is used to compare embeddings rather than Euclidean distance.
- **[L2]** How does tokenization affect LLM context windows, pricing, and application design?
- **[L2]** Why do AI APIs need streaming responses, and how do Python generators enable this?
- **[L3]** Design the API structure for a production AI service that handles both fast inference (embeddings) and long-running tasks (document processing). What patterns would you use?
- **[L3]** You are building a RAG pipeline that processes 10,000 documents. How would you structure the data pipeline using Python async, and what would your evaluation strategy look like?
- **[L3]** Explain gradient descent and backpropagation at a high level. Why does a GenAI engineer need to understand these even if they are not training models from scratch?

## Interview Answers

1. **Supervised learning** uses labeled data (input-output pairs), **unsupervised learning** finds patterns in unlabeled data, and **self-supervised learning** generates its own labels from the data structure. LLMs use self-supervised learning — GPT predicts the next token, BERT predicts masked tokens. This allows training on massive text corpora without human annotation.
2. Python dominates because of its ecosystem: NumPy and Pandas for data, PyTorch and TensorFlow for model training, Hugging Face for pretrained models, LangChain and LlamaIndex for LLM applications, FastAPI for serving. The language's readability accelerates experimentation, and its C-extensions (NumPy, PyTorch) handle performance-critical math.
3. Cosine similarity measures the angle between two vectors, ignoring magnitude. Two documents of different lengths can have very different Euclidean distances but similar meanings — cosine similarity captures this because it normalizes by vector length. It ranges from -1 (opposite) to 1 (identical direction), making it intuitive for "how similar are these meanings?" comparisons.
4. LLMs operate on tokens, not words. A word like "tokenization" might become multiple tokens. Context windows (e.g., 128K tokens for GPT-4) limit how much text the model can see at once. Pricing is per-token. Application design must account for token budgets: prompt + context + response must fit within the window, and longer prompts cost more. Counting tokens with tiktoken before sending requests prevents errors and controls cost.
5. LLMs generate tokens one at a time, and full responses can take seconds. Without streaming, users see nothing until the complete response arrives. With streaming, each token appears immediately, creating a responsive experience. Python generators (`yield`) naturally model this — they produce values lazily one at a time, matching the token-by-token output of LLM inference. FastAPI's `StreamingResponse` wraps async generators for HTTP streaming.
6. I would separate endpoints by latency profile. **Fast inference** (embeddings, classification): synchronous POST endpoints with response in <500ms. **Medium tasks** (single document summarization): streaming SSE endpoints. **Long tasks** (batch document processing, evaluation): async job pattern with POST to create a job (returns job_id immediately), GET to poll status, and optional webhook callback on completion. All endpoints use Pydantic validation, and a health endpoint reports model readiness for load balancer integration.
7. I would use an async pipeline with three stages: (1) **Ingestion** — async file reading with `aiofiles`, parsing with appropriate libraries, splitting into chunks. Use `asyncio.Semaphore` to limit concurrency and avoid memory spikes. (2) **Embedding** — batch documents (e.g., 100 at a time) for the embedding API, using `asyncio.gather` with rate limiting. Store embeddings in a vector database. (3) **Evaluation** — hold out 10-15% of documents with known questions/answers. Measure retrieval recall (does the correct chunk appear in top-k?), generation accuracy (does the LLM answer correctly?), and latency. Use Pandas to aggregate metrics across categories and difficulty levels.
8. **Gradient descent** iteratively adjusts model weights to minimize a loss function — it computes the gradient (direction of steepest increase in loss) and steps in the opposite direction. **Backpropagation** efficiently computes these gradients through the chain rule, propagating error backward from output to input layers. A GenAI engineer needs this understanding to: diagnose fine-tuning issues (loss not decreasing = learning rate too low or data problems), choose hyperparameters (learning rate, batch size, warmup steps), understand why model behavior changes during fine-tuning, and communicate effectively with ML engineers about training decisions.
