---
id: genai-eng-llm-app-development
slug: llm-app-development
title: "Module 4: LLM Application Development"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: intermediate
estimatedMinutes: 100
version:
  minimum: "Python 3.11+, OpenAI SDK 1.x, FastAPI"
prerequisites: [genai-eng-prompt-engineering]
tags: [genai-engineer, llm-api, streaming, conversation-memory, retries, rate-limits, fastapi, workflows]
relatedTopics: [genai-eng-embeddings-vector-search]
order: 4
status: published
---
# Module 4: LLM Application Development

## Introduction

Calling an LLM once in a notebook is easy. Building an application around LLMs that is fast, reliable, affordable, and pleasant to use is real engineering work.

This module covers the building blocks every LLM application needs: working with provider APIs, streaming responses to a UI, managing conversation history and context windows, handling errors, retries and rate limits, abstracting multiple providers, caching, and composing LLM calls into workflows. We finish by building a complete streaming chat backend with FastAPI.

---

## Part 1: Working with LLM Provider APIs

### The Common Request Shape

Almost every chat API follows the same shape: a model name, a list of messages, and generation parameters.

```python
from openai import OpenAI

client = OpenAI()  # uses OPENAI_API_KEY env variable

response = client.chat.completions.create(
    model="gpt-4o-mini",
    messages=[
        {"role": "system", "content": "You are a concise assistant."},
        {"role": "user", "content": "Explain idempotency in one sentence."},
    ],
    temperature=0.2,
    max_tokens=100,
)

print(response.choices[0].message.content)
print(response.usage)  # prompt_tokens, completion_tokens, total_tokens
print(response.choices[0].finish_reason)  # "stop", "length", "tool_calls", "content_filter"
```

### The Same Call with Anthropic

```python
import anthropic

client = anthropic.Anthropic()  # uses ANTHROPIC_API_KEY

message = client.messages.create(
    model="claude-3-5-sonnet-latest",
    max_tokens=100,
    system="You are a concise assistant.",
    messages=[{"role": "user", "content": "Explain idempotency in one sentence."}],
)

print(message.content[0].text)
print(message.usage.input_tokens, message.usage.output_tokens)
```

**Differences to notice:** Anthropic puts the system prompt in a separate `system` parameter, requires `max_tokens`, and returns content as a list of blocks. These small differences are why teams build a thin abstraction layer (Part 5).

### Important Response Fields

| Field | Why It Matters |
|---|---|
| `finish_reason = "length"` | Output was cut off by `max_tokens` - the answer is incomplete |
| `finish_reason = "content_filter"` | Provider safety system blocked output |
| `finish_reason = "tool_calls"` | Model wants you to execute tools |
| `usage` | Token counts for cost tracking and budgets |
| Request ID headers | Needed when opening support tickets with the provider |

### Secrets and Configuration

Never hardcode keys. Load from environment variables or a secrets manager and centralize settings.

```python
from pydantic_settings import BaseSettings

class Settings(BaseSettings):
    openai_api_key: str
    default_model: str = "gpt-4o-mini"
    request_timeout_seconds: float = 30.0
    max_output_tokens: int = 800

    class Config:
        env_file = ".env"

settings = Settings()
```

---

## Part 2: Streaming Responses

### Why Streaming

A 500-token answer can take 5-10 seconds to generate. Without streaming, users stare at a spinner. With streaming, the first words appear in a few hundred milliseconds. The key metric is **Time To First Token (TTFT)**.

### Streaming with the OpenAI SDK

```python
stream = client.chat.completions.create(
    model="gpt-4o-mini",
    messages=[{"role": "user", "content": "Write a haiku about Kubernetes."}],
    stream=True,
)

full_text = []
for chunk in stream:
    delta = chunk.choices[0].delta.content if chunk.choices else None
    if delta:
        print(delta, end="", flush=True)
        full_text.append(delta)

answer = "".join(full_text)
```

### Async Streaming

```python
import asyncio
from openai import AsyncOpenAI

aclient = AsyncOpenAI()

async def stream_answer(prompt: str):
    stream = await aclient.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": prompt}],
        stream=True,
    )
    async for chunk in stream:
        if chunk.choices and chunk.choices[0].delta.content:
            yield chunk.choices[0].delta.content

async def main():
    async for token in stream_answer("Explain CAP theorem briefly."):
        print(token, end="", flush=True)

asyncio.run(main())
```

### Delivering Streams to the Browser: Server-Sent Events (SSE)

SSE is a simple, one-directional HTTP streaming protocol, ideal for LLM output. Each event is a line starting with `data:` followed by a blank line.

```python
from fastapi import FastAPI
from fastapi.responses import StreamingResponse
from pydantic import BaseModel
import json

app = FastAPI()

class ChatRequest(BaseModel):
    message: str

@app.post("/chat/stream")
async def chat_stream(req: ChatRequest):
    async def event_generator():
        async for token in stream_answer(req.message):
            yield f"data: {json.dumps({'token': token})}\n\n"
        yield "data: [DONE]\n\n"

    return StreamingResponse(event_generator(), media_type="text/event-stream")
```

### Consuming the Stream in the Frontend

```typescript
async function streamChat(message: string, onToken: (t: string) => void) {
  const response = await fetch('/chat/stream', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message }),
  });

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = '';

  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });

    const events = buffer.split('\n\n');
    buffer = events.pop() ?? '';
    for (const event of events) {
      const data = event.replace(/^data: /, '');
      if (data === '[DONE]') return;
      onToken(JSON.parse(data).token);
    }
  }
}
```

### Streaming Gotchas

- **Proxies buffer responses** - disable buffering in Nginx (`proxy_buffering off;`) or send `X-Accel-Buffering: no`
- **Cancellation** - when the user closes the tab, stop the upstream LLM call to save cost
- **Partial structured output** - you cannot `json.loads` a half-streamed JSON; stream prose, or use partial JSON parsers
- **Usage stats** - with streaming, token usage is reported only in the final chunk (OpenAI: `stream_options={"include_usage": True}`)

---

## Part 3: Conversation Memory and Context Management

### LLMs Are Stateless

The model remembers nothing between requests. "Memory" is the history you resend each time. Your application owns the conversation state.

```python
class Conversation:
    def __init__(self, system_prompt: str):
        self.messages = [{"role": "system", "content": system_prompt}]

    def ask(self, user_text: str) -> str:
        self.messages.append({"role": "user", "content": user_text})
        response = client.chat.completions.create(model="gpt-4o-mini", messages=self.messages)
        answer = response.choices[0].message.content
        self.messages.append({"role": "assistant", "content": answer})
        return answer

chat = Conversation("You are a helpful travel assistant.")
chat.ask("I want to visit Japan in April.")
print(chat.ask("What should I pack?"))  # model knows the context is Japan in April
```

### The Context Window Problem

History grows every turn. Eventually you exceed the context window, costs rise linearly with history length, and very long contexts can reduce answer quality ("lost in the middle").

### Memory Strategies

| Strategy | How It Works | Pros | Cons |
|---|---|---|---|
| **Full history** | Send everything | Simple, perfect recall | Expensive, hits limits |
| **Sliding window** | Keep last N turns | Predictable cost | Forgets early facts |
| **Token-budget trimming** | Drop oldest turns until under budget | Uses space efficiently | Still forgets |
| **Summary memory** | Summarize old turns into a running summary | Retains key facts cheaply | Summary may lose detail |
| **Retrieval memory** | Store turns in a vector DB, retrieve relevant ones | Scales to long histories | More infrastructure |

### Token-Budget Trimming with Summarization

```python
import tiktoken

enc = tiktoken.get_encoding("o200k_base")

def count_tokens(messages: list[dict]) -> int:
    return sum(len(enc.encode(m["content"] or "")) + 4 for m in messages)

def summarize_turns(turns: list[dict]) -> str:
    transcript = "\n".join(f"{m['role']}: {m['content']}" for m in turns)
    r = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": f"Summarize the key facts, decisions and user preferences:\n{transcript}"}],
        max_tokens=200,
    )
    return r.choices[0].message.content

def fit_to_budget(messages: list[dict], budget: int = 3000, keep_recent: int = 6) -> list[dict]:
    if count_tokens(messages) <= budget:
        return messages
    system, history = messages[0], messages[1:]
    old, recent = history[:-keep_recent], history[-keep_recent:]
    summary = summarize_turns(old)
    return [system, {"role": "system", "content": f"Summary of earlier conversation: {summary}"}, *recent]
```

### Persisting Conversations

In a real backend, store conversations in a database keyed by user and session:

```text
conversations(id, user_id, title, created_at)
messages(id, conversation_id, role, content, token_count, model, created_at)
```

Store `model` and `token_count` per message for analytics and cost reporting.

---

## Part 4: Reliability - Errors, Retries, Timeouts, Rate Limits

### Failure Types

| Error | HTTP | Retry? | Action |
|---|---|---|---|
| Rate limit | 429 | Yes, with backoff | Respect `retry-after` header |
| Server error / overloaded | 500, 502, 503, 529 | Yes, with backoff | Consider fallback provider |
| Timeout | - | Yes (once or twice) | Lower max_tokens, stream |
| Bad request | 400 | No | Fix the request (context too long, invalid schema) |
| Authentication | 401/403 | No | Alert, check keys |
| Content filtered | 400 / finish_reason | No | Return safe message to user |

### Exponential Backoff with Jitter

```python
import random
import time
from openai import OpenAI, RateLimitError, APITimeoutError, APIConnectionError, InternalServerError

client = OpenAI(timeout=30.0, max_retries=0)  # we handle retries ourselves

RETRYABLE = (RateLimitError, APITimeoutError, APIConnectionError, InternalServerError)

def call_with_retry(messages: list[dict], model: str = "gpt-4o-mini", max_attempts: int = 5) -> str:
    for attempt in range(1, max_attempts + 1):
        try:
            r = client.chat.completions.create(model=model, messages=messages)
            return r.choices[0].message.content
        except RETRYABLE as err:
            if attempt == max_attempts:
                raise
            delay = min(2 ** attempt, 30) + random.uniform(0, 1)
            print(f"Attempt {attempt} failed ({type(err).__name__}); retrying in {delay:.1f}s")
            time.sleep(delay)
```

**Note:** the official SDKs already retry some errors automatically (`max_retries` defaults to 2). Libraries like `tenacity` give declarative retry policies.

### Fallback Across Models or Providers

```python
def call_with_fallback(messages: list[dict]) -> str:
    for model in ["gpt-4o", "gpt-4o-mini"]:
        try:
            return call_with_retry(messages, model=model, max_attempts=2)
        except Exception as err:
            print(f"{model} failed: {err}")
    return "Sorry, the assistant is temporarily unavailable. Please try again."
```

### Client-Side Rate Limiting and Concurrency Control

Providers limit **requests per minute (RPM)** and **tokens per minute (TPM)**. When processing batches, cap concurrency.

```python
import asyncio
from openai import AsyncOpenAI

aclient = AsyncOpenAI()
semaphore = asyncio.Semaphore(8)  # max 8 in-flight requests

async def classify(text: str) -> str:
    async with semaphore:
        r = await aclient.chat.completions.create(
            model="gpt-4o-mini",
            temperature=0,
            messages=[{"role": "user", "content": f"Classify as bug/feature/question: {text}"}],
        )
        return r.choices[0].message.content

async def classify_all(texts: list[str]) -> list[str]:
    return await asyncio.gather(*(classify(t) for t in texts))
```

For large offline jobs, use provider **Batch APIs** - they are asynchronous, cheaper (often ~50% off), and have separate rate limits.

---

## Part 5: Provider Abstraction and Model Routing

### Why Abstract

- Swap models without rewriting business logic
- Fall back when a provider is down
- Route cheap tasks to small models and hard tasks to large models
- Test with a fake client

### A Minimal Provider-Agnostic Interface

```python
from typing import Protocol
from dataclasses import dataclass

@dataclass
class LLMResult:
    text: str
    input_tokens: int
    output_tokens: int
    model: str

class LLMClient(Protocol):
    def complete(self, system: str, messages: list[dict], max_tokens: int = 800) -> LLMResult: ...

class OpenAIClient:
    def __init__(self, model: str = "gpt-4o-mini"):
        from openai import OpenAI
        self.client, self.model = OpenAI(), model

    def complete(self, system, messages, max_tokens=800):
        r = self.client.chat.completions.create(
            model=self.model, max_tokens=max_tokens,
            messages=[{"role": "system", "content": system}, *messages],
        )
        return LLMResult(r.choices[0].message.content, r.usage.prompt_tokens, r.usage.completion_tokens, self.model)

class AnthropicClient:
    def __init__(self, model: str = "claude-3-5-haiku-latest"):
        import anthropic
        self.client, self.model = anthropic.Anthropic(), model

    def complete(self, system, messages, max_tokens=800):
        r = self.client.messages.create(model=self.model, system=system, messages=messages, max_tokens=max_tokens)
        return LLMResult(r.content[0].text, r.usage.input_tokens, r.usage.output_tokens, self.model)

class FakeClient:
    def complete(self, system, messages, max_tokens=800):
        return LLMResult("fake answer", 10, 2, "fake")
```

Libraries such as **LiteLLM** provide this abstraction for 100+ providers with one OpenAI-compatible interface. Many self-hosted servers (vLLM, Ollama, LM Studio) also expose OpenAI-compatible endpoints, so you can point the OpenAI SDK at them with `base_url`.

```python
local = OpenAI(base_url="http://localhost:11434/v1", api_key="ollama")  # Ollama
r = local.chat.completions.create(model="llama3.1", messages=[{"role": "user", "content": "Hi"}])
```

### Simple Router

```python
def choose_model(task: str, input_tokens: int) -> LLMClient:
    if task in {"classify", "extract", "route"}:
        return OpenAIClient("gpt-4o-mini")
    if input_tokens > 100_000:
        return AnthropicClient("claude-3-5-sonnet-latest")  # long context
    return OpenAIClient("gpt-4o")
```

---

## Part 6: Caching

| Cache Type | What It Caches | When It Helps |
|---|---|---|
| **Exact-match cache** | Hash of (model, messages, params) to response | Repeated identical requests (FAQ, tests) |
| **Semantic cache** | Embedding of the query to response for similar queries | Paraphrased repeated questions |
| **Provider prompt caching** | Provider caches the static prompt prefix | Long system prompts / documents reused across calls |

```python
import hashlib
import json

cache: dict[str, str] = {}  # use Redis in production

def cached_completion(model: str, messages: list[dict], temperature: float = 0) -> str:
    key = hashlib.sha256(json.dumps({"m": model, "msgs": messages, "t": temperature}, sort_keys=True).encode()).hexdigest()
    if key in cache:
        return cache[key]
    r = client.chat.completions.create(model=model, messages=messages, temperature=temperature)
    cache[key] = r.choices[0].message.content
    return cache[key]
```

Only cache deterministic (temperature 0) and non-personalized responses, and set a TTL.

---

## Part 7: Workflows - Composing LLM Calls

Most production LLM features are **workflows** (predefined code paths) rather than autonomous agents. Common patterns:

| Pattern | Description | Example |
|---|---|---|
| **Chain** | Output of step 1 feeds step 2 | Extract then summarize then translate |
| **Router** | Classify input, send to specialized prompt | Support bot routes billing vs technical |
| **Parallel / fan-out** | Run independent calls concurrently, merge | Review code for security, style and performance simultaneously |
| **Evaluator-optimizer** | One call generates, another critiques, loop | Draft email, critique tone, revise |
| **Map-reduce** | Process chunks independently, then combine | Summarize a 300-page report |

### Router + Parallel Example

```python
import asyncio

ROUTES = {
    "billing": "You are a billing specialist. Be precise about amounts and dates.",
    "technical": "You are a senior support engineer. Provide step-by-step troubleshooting.",
    "general": "You are a friendly support agent.",
}

async def route(query: str) -> str:
    r = await aclient.chat.completions.create(
        model="gpt-4o-mini", temperature=0,
        messages=[{"role": "user", "content": f"Classify as billing, technical, or general. One word only.\n\n{query}"}],
    )
    label = r.choices[0].message.content.strip().lower()
    return label if label in ROUTES else "general"

async def answer(query: str) -> str:
    label = await route(query)
    r = await aclient.chat.completions.create(
        model="gpt-4o",
        messages=[{"role": "system", "content": ROUTES[label]}, {"role": "user", "content": query}],
    )
    return r.choices[0].message.content

async def parallel_review(code: str) -> dict[str, str]:
    aspects = ["security", "performance", "readability"]
    async def review(aspect: str) -> str:
        r = await aclient.chat.completions.create(
            model="gpt-4o-mini",
            messages=[{"role": "user", "content": f"Review this code only for {aspect}. Max 3 bullets.\n\n{code}"}],
        )
        return r.choices[0].message.content
    results = await asyncio.gather(*(review(a) for a in aspects))
    return dict(zip(aspects, results))
```

### Map-Reduce Summarization

```python
def chunk_text(text: str, size: int = 3000) -> list[str]:
    return [text[i:i + size] for i in range(0, len(text), size)]

async def map_reduce_summary(document: str) -> str:
    async def summarize_chunk(chunk: str) -> str:
        r = await aclient.chat.completions.create(
            model="gpt-4o-mini",
            messages=[{"role": "user", "content": f"Summarize in 3 bullets:\n{chunk}"}],
        )
        return r.choices[0].message.content

    partials = await asyncio.gather(*(summarize_chunk(c) for c in chunk_text(document)))
    r = await aclient.chat.completions.create(
        model="gpt-4o",
        messages=[{"role": "user", "content": "Combine these partial summaries into one executive summary:\n\n" + "\n\n".join(partials)}],
    )
    return r.choices[0].message.content
```

### Frameworks

| Framework | Strength |
|---|---|
| **LangChain** | Large ecosystem of integrations, prompt templates, output parsers |
| **LangGraph** | Stateful graphs, loops, human-in-the-loop (Module 9) |
| **LlamaIndex** | Data ingestion and RAG pipelines |
| **Semantic Kernel** | .NET/Java/Python enterprise orchestration |
| **Plain SDK + your code** | Maximum control, fewer abstractions - often the right starting point |

---

## Part 8: Complete Example - Production-Style Chat Backend

```python
# app.py - run with: uvicorn app:app --reload
import json
import time
import uuid
from fastapi import FastAPI, HTTPException, Request
from fastapi.responses import StreamingResponse
from pydantic import BaseModel, Field
from openai import AsyncOpenAI

app = FastAPI(title="Chat Backend")
aclient = AsyncOpenAI(timeout=60.0)

SYSTEM_PROMPT = "You are a helpful engineering assistant. Answer concisely and use code blocks for code."
MAX_HISTORY_MESSAGES = 20
sessions: dict[str, list[dict]] = {}  # replace with Redis/Postgres in production

class ChatIn(BaseModel):
    session_id: str | None = None
    message: str = Field(min_length=1, max_length=8000)

@app.post("/v1/chat")
async def chat(body: ChatIn, request: Request):
    session_id = body.session_id or str(uuid.uuid4())
    history = sessions.setdefault(session_id, [])
    history.append({"role": "user", "content": body.message})
    messages = [{"role": "system", "content": SYSTEM_PROMPT}, *history[-MAX_HISTORY_MESSAGES:]]

    async def events():
        started = time.perf_counter()
        first_token_at = None
        parts: list[str] = []
        try:
            stream = await aclient.chat.completions.create(
                model="gpt-4o-mini",
                messages=messages,
                stream=True,
                stream_options={"include_usage": True},
                max_tokens=1000,
            )
            yield f"data: {json.dumps({'type': 'session', 'session_id': session_id})}\n\n"
            async for chunk in stream:
                if await request.is_disconnected():
                    break  # user left - stop spending tokens
                if chunk.choices and chunk.choices[0].delta.content:
                    if first_token_at is None:
                        first_token_at = time.perf_counter()
                    token = chunk.choices[0].delta.content
                    parts.append(token)
                    yield f"data: {json.dumps({'type': 'token', 'value': token})}\n\n"
                if chunk.usage:
                    yield f"data: {json.dumps({'type': 'usage', 'input': chunk.usage.prompt_tokens, 'output': chunk.usage.completion_tokens})}\n\n"
        except Exception as err:
            yield f"data: {json.dumps({'type': 'error', 'message': 'The model is unavailable. Please retry.'})}\n\n"
            print(f"LLM error: {err}")
        finally:
            if parts:
                history.append({"role": "assistant", "content": "".join(parts)})
            ttft = (first_token_at - started) if first_token_at else None
            print(json.dumps({"session": session_id, "ttft_s": ttft, "total_s": time.perf_counter() - started}))
            yield "data: [DONE]\n\n"

    return StreamingResponse(events(), media_type="text/event-stream", headers={"X-Accel-Buffering": "no"})

@app.delete("/v1/chat/{session_id}")
async def reset(session_id: str):
    if sessions.pop(session_id, None) is None:
        raise HTTPException(404, "Session not found")
    return {"reset": True}
```

**What this example demonstrates:** input validation, session memory with a history cap, SSE streaming with typed events, client disconnect handling, usage reporting, graceful error events, and latency logging (TTFT and total time).

---

## Summary

| Area | Production Practice |
|---|---|
| API calls | Check `finish_reason`, track `usage`, centralize config |
| Streaming | SSE, measure TTFT, disable proxy buffering, handle cancel |
| Memory | App owns state; trim, summarize, or retrieve history |
| Reliability | Timeouts, exponential backoff with jitter, fallbacks |
| Rate limits | Semaphores, batch APIs, respect retry-after |
| Abstraction | Provider-agnostic interface, OpenAI-compatible endpoints |
| Caching | Exact, semantic, provider prompt caching |
| Workflows | Chain, router, parallel, evaluator-optimizer, map-reduce |

---

## Interview Questions

- **[L1]** Why do LLM applications stream responses, and what is Time To First Token?
- **[L1]** LLMs are stateless. How does a chatbot "remember" previous messages?
- **[L2]** Which LLM API errors should be retried and which should not? Describe a good retry strategy.
- **[L2]** Compare sliding-window, summary, and retrieval-based conversation memory. When would you use each?
- **[L2]** How do you implement streaming from an LLM through a FastAPI backend to a browser, and what production issues can break it?
- **[L3]** Design a provider-agnostic LLM layer for a company that uses OpenAI, Anthropic, and a self-hosted model. What features does it need?
- **[L3]** You must summarize 50,000 support tickets overnight within provider rate limits and a fixed budget. How do you design the job?
- **[L3]** When should you build a deterministic workflow instead of an autonomous agent? Give examples of workflow patterns.

## Interview Answers

1. Generating a long answer can take several seconds; streaming sends tokens to the user as they are produced so the interface feels responsive and users can start reading immediately or cancel early. Time To First Token (TTFT) is the latency from sending the request to receiving the first generated token. It is the main perceived-latency metric for chat applications and is affected by prompt length, model size, provider load, and network.
2. The model keeps no state between API calls. The application stores the conversation (in memory, Redis, or a database) and resends the relevant history with each request. Because context windows and costs are limited, applications trim history with sliding windows, summarize older turns into a running summary, or store turns in a vector store and retrieve only relevant past messages.
3. Retry transient errors: 429 rate limits (respecting the `retry-after` header), 5xx server or overload errors, connection errors, and timeouts. Do not retry 400 bad requests (context too long, invalid parameters), 401/403 authentication errors, or content-policy rejections, because the same request will fail again. A good strategy uses exponential backoff with jitter, a maximum number of attempts, a total time budget, idempotent requests, and a fallback model or provider after retries are exhausted, plus metrics on retry rates.
4. **Sliding window** keeps the last N turns; it is simple and cheap but forgets early facts, good for short task-focused chats. **Summary memory** compresses older turns into a running summary; it keeps key facts and preferences at low token cost but can lose detail, good for long assistants. **Retrieval memory** embeds past turns or facts and retrieves only relevant ones per request; it scales to very long histories and cross-session memory but needs a vector store and relevance tuning. Many systems combine recent-window + summary + retrieval.
5. The backend calls the LLM with `stream=True` and iterates chunks with an async generator, formatting each token as an SSE event (`data: ...\n\n`) returned through `StreamingResponse` with `text/event-stream`. The browser reads the body via `fetch` and a `ReadableStream` reader (or `EventSource` for GET), parses events, and appends tokens to the UI. Common issues: reverse proxies or load balancers buffering the response (fix with `proxy_buffering off` / `X-Accel-Buffering: no`), idle timeouts on long streams, not stopping the upstream call when the client disconnects, compression middleware buffering output, and handling errors mid-stream with a typed error event.
6. It needs a common request/response model (messages, tools, structured output, streaming events, usage), adapters per provider translating differences (system prompt placement, tool call formats, content blocks), configuration-driven model routing by task, cost, latency, and data sensitivity, retries with backoff, circuit breakers, and cross-provider fallback, centralized secrets and per-team API keys/quotas, token and cost accounting per tenant, logging and tracing of prompts, versions, and latency, PII redaction policies, caching, and a fake/mock provider for tests. Self-hosted models are integrated via an OpenAI-compatible endpoint (vLLM). Alternatively adopt a gateway such as LiteLLM and extend it.
7. Estimate tokens per ticket to compute total tokens and cost, and choose the cheapest model that meets quality on a sample evaluated against human summaries. Prefer the provider Batch API for 50,000 offline requests since it is cheaper and uses separate limits. If using the live API, run an async worker pool with a semaphore and a token-bucket limiter tuned to RPM/TPM, with backoff on 429s. Make the job idempotent and resumable by storing each result keyed by ticket ID, so failures only reprocess missing items. Truncate or pre-clean long tickets, cache duplicates, track spend in real time with a hard budget stop, and sample outputs for quality.
8. Use workflows when the steps are known in advance, when predictability, testability, latency, and cost control matter, and when actions are risky. Agents fit open-ended problems where the number and order of steps cannot be predetermined. Workflow patterns include prompt chaining (extract then summarize), routing (classify then send to a specialized prompt or model), parallelization (independent reviews merged), map-reduce (summarize chunks then combine), and evaluator-optimizer loops (generate, critique, revise with a max iteration count). Most production features start as workflows and add agentic behavior only where it clearly adds value.
