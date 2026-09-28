---
id: genai-eng-llm-fundamentals
slug: llm-fundamentals
title: "Module 2: GenAI & LLM Fundamentals"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: intermediate
estimatedMinutes: 90
version:
  minimum: "Framework agnostic concepts"
prerequisites: [genai-eng-programming-ai-foundations]
tags: [genai-engineer, llm, transformers, attention, tokenization, gpt, bert, training, inference]
relatedTopics: [genai-eng-prompt-engineering]
order: 2
status: published
---
# Module 2: GenAI & LLM Fundamentals

## Introduction

This module explains how large language models actually work — from the transformer architecture that powers them, through the training process that gives them knowledge, to the inference mechanics that generate text. You will understand what happens when you send a prompt to GPT-4 or Claude, why models hallucinate, what temperature and top-p actually control, and how different model families (GPT, BERT, T5, LLaMA) differ architecturally.

This is not a research-level deep dive into linear algebra. This is the practical understanding every GenAI engineer needs to make informed decisions about model selection, prompt design, fine-tuning, and debugging.

---

## Part 1: The Transformer Architecture

### Why Transformers Changed Everything

Before transformers (2017), language models used recurrent neural networks (RNNs/LSTMs) that processed text sequentially — one word at a time, left to right. This had two critical problems:

1. **Sequential bottleneck**: Could not parallelize training across GPUs efficiently
2. **Long-range forgetting**: Information from early tokens degraded by the time the model processed later tokens

The transformer solved both problems with the **self-attention mechanism**, which lets every token directly attend to every other token in a single parallel operation.

### The Transformer Block

Every modern LLM is built from stacked transformer blocks. Each block contains:

```text
Input Embeddings (token vectors)
        |
  Multi-Head Self-Attention
        |
  Add & Layer Normalize (residual connection)
        |
  Feed-Forward Network (2-layer MLP)
        |
  Add & Layer Normalize (residual connection)
        |
  Output (passed to next block or final prediction)
```

GPT-4 has ~120 of these blocks stacked. LLaMA 3 70B has 80. Each block refines the representation of every token by letting it gather information from other tokens (attention) and then transform that information (feed-forward).

### Self-Attention: The Core Mechanism

Self-attention answers the question: "For each token, which other tokens in the sequence are most relevant?"

#### How It Works Step by Step

1. Each token's embedding is projected into three vectors: **Query (Q)**, **Key (K)**, and **Value (V)**
2. The attention score between token i and token j is computed as: `score = Q_i · K_j / sqrt(d_k)`
3. Scores are normalized with softmax to create attention weights (sum to 1)
4. The output for each token is a weighted sum of all Value vectors

```python
import numpy as np

def self_attention(embeddings: np.ndarray) -> np.ndarray:
    """
    Simplified self-attention for understanding.
    embeddings: shape (seq_len, d_model)
    Returns: shape (seq_len, d_model)
    """
    seq_len, d_model = embeddings.shape
    d_k = d_model  # In practice, d_k = d_model / num_heads

    # In real models, these are learned linear projections (weight matrices)
    # Here we use identity for simplicity
    Q = embeddings  # (seq_len, d_k)
    K = embeddings  # (seq_len, d_k)
    V = embeddings  # (seq_len, d_k)

    # Step 1: Compute attention scores
    # Each query attends to every key
    scores = Q @ K.T / np.sqrt(d_k)  # (seq_len, seq_len)

    # Step 2: Softmax to get attention weights
    exp_scores = np.exp(scores - np.max(scores, axis=-1, keepdims=True))
    attention_weights = exp_scores / np.sum(exp_scores, axis=-1, keepdims=True)

    # Step 3: Weighted sum of values
    output = attention_weights @ V  # (seq_len, d_model)

    return output, attention_weights

# Example: 4 tokens, 8-dimensional embeddings
np.random.seed(42)
embeddings = np.random.rand(4, 8)
output, weights = self_attention(embeddings)

print("Attention weights (which tokens attend to which):")
tokens = ["The", "cat", "sat", "down"]
for i, token in enumerate(tokens):
    top_attention = tokens[np.argmax(weights[i])]
    print(f"  '{token}' attends most to '{top_attention}' ({weights[i].max():.3f})")
```

**What this shows:** Each token computes a relevance score with every other token, then uses those scores to aggregate information. In the sentence "The bank by the river was steep," the word "bank" would attend strongly to "river" to disambiguate its meaning.

### Multi-Head Attention

Instead of one attention computation, transformers use multiple "heads" — each head learns to attend to different types of relationships.

```python
def multi_head_attention(embeddings: np.ndarray, num_heads: int = 4) -> np.ndarray:
    """
    Multi-head attention splits the embedding dimension across heads.
    Each head independently computes attention, then results are concatenated.
    """
    seq_len, d_model = embeddings.shape
    d_k = d_model // num_heads
    outputs = []

    for head in range(num_heads):
        # Each head gets a slice of the embedding dimension
        start = head * d_k
        end = start + d_k
        head_embeddings = embeddings[:, start:end]

        # Compute attention for this head
        Q = head_embeddings
        K = head_embeddings
        V = head_embeddings

        scores = Q @ K.T / np.sqrt(d_k)
        exp_scores = np.exp(scores - np.max(scores, axis=-1, keepdims=True))
        weights = exp_scores / np.sum(exp_scores, axis=-1, keepdims=True)
        head_output = weights @ V
        outputs.append(head_output)

    # Concatenate all heads back together
    return np.concatenate(outputs, axis=-1)  # (seq_len, d_model)

result = multi_head_attention(embeddings, num_heads=4)
print(f"Multi-head output shape: {result.shape}")  # (4, 8)
```

**Why multiple heads matter:**
- Head 1 might learn syntactic relationships (subject-verb agreement)
- Head 2 might learn positional proximity (nearby words)
- Head 3 might learn semantic relationships (synonyms, antonyms)
- Head 4 might learn coreference (pronouns to their referents)

GPT-4 uses 96 attention heads. Each head independently discovers useful patterns during training.

### Causal (Masked) Attention

GPT-style models use **causal masking** — each token can only attend to tokens that came before it (and itself). This prevents the model from "cheating" by looking at future tokens during training.

```python
def causal_attention(embeddings: np.ndarray) -> np.ndarray:
    seq_len, d_model = embeddings.shape
    Q, K, V = embeddings, embeddings, embeddings

    scores = Q @ K.T / np.sqrt(d_model)

    # Causal mask: set future positions to -infinity
    mask = np.triu(np.ones((seq_len, seq_len)), k=1) * (-1e9)
    scores = scores + mask

    exp_scores = np.exp(scores - np.max(scores, axis=-1, keepdims=True))
    weights = exp_scores / np.sum(exp_scores, axis=-1, keepdims=True)
    output = weights @ V

    return output, weights

output, weights = causal_attention(embeddings)
print("Causal attention weights:")
tokens = ["The", "cat", "sat", "down"]
for i, token in enumerate(tokens):
    visible = [f"{tokens[j]}:{weights[i][j]:.2f}" for j in range(i + 1)]
    print(f"  '{token}' sees: {', '.join(visible)}")
```

**Key insight:** This is why GPT models are called "autoregressive" — they generate text left-to-right, one token at a time, where each new token can only see what came before it.

---

## Part 2: Tokenization Deep Dive

### How Text Becomes Numbers

Models cannot process raw text. Tokenization converts text into integer token IDs from a fixed vocabulary.

### Tokenization Algorithms

| Algorithm | Used By | How It Works |
|---|---|---|
| **BPE (Byte Pair Encoding)** | GPT-2, GPT-3, GPT-4, LLaMA | Iteratively merges the most frequent character pairs |
| **WordPiece** | BERT, DistilBERT | Similar to BPE but uses likelihood-based merging |
| **SentencePiece** | T5, LLaMA, Gemma | Language-agnostic, works on raw text including spaces |
| **Tiktoken** | OpenAI models | Optimized BPE implementation by OpenAI |

### BPE: How It Actually Works

```python
def demonstrate_bpe():
    """
    Simplified BPE demonstration.
    BPE starts with individual characters and merges frequent pairs.
    """
    # Start with character-level tokens
    corpus = ["l o w </w>", "l o w e r </w>", "n e w </w>", "n e w e r </w>"]

    # Count all adjacent pairs
    from collections import Counter

    def get_pairs(corpus):
        pairs = Counter()
        for word in corpus:
            symbols = word.split()
            for i in range(len(symbols) - 1):
                pairs[(symbols[i], symbols[i + 1])] += 1
        return pairs

    def merge_pair(corpus, pair):
        merged = []
        for word in corpus:
            word = word.replace(f"{pair[0]} {pair[1]}", f"{pair[0]}{pair[1]}")
            merged.append(word)
        return merged

    print("BPE Merge Steps:")
    for step in range(5):
        pairs = get_pairs(corpus)
        if not pairs:
            break
        best_pair = pairs.most_common(1)[0]
        print(f"  Step {step + 1}: Merge '{best_pair[0][0]}' + '{best_pair[0][1]}' (count: {best_pair[1]})")
        corpus = merge_pair(corpus, best_pair[0])
        print(f"    Corpus: {corpus}")

demonstrate_bpe()
```

### Token Count vs Word Count

```python
import tiktoken

encoder = tiktoken.encoding_for_model("gpt-4")

examples = [
    "Hello world",
    "The quick brown fox jumps over the lazy dog",
    "def fibonacci(n): return n if n <= 1 else fibonacci(n-1) + fibonacci(n-2)",
    "antidisestablishmentarianism",
    "こんにちは世界",  # Japanese: "Hello world"
    "https://api.example.com/v1/embeddings?model=text-embedding-3-large",
]

print(f"{'Text':<70} {'Words':>6} {'Tokens':>7} {'Ratio':>6}")
print("-" * 95)
for text in examples:
    words = len(text.split())
    tokens = len(encoder.encode(text))
    ratio = tokens / max(words, 1)
    display = text[:67] + "..." if len(text) > 70 else text
    print(f"{display:<70} {words:>6} {tokens:>7} {ratio:>6.1f}x")
```

**Key takeaways:**
- English text averages ~1.3 tokens per word
- Code is more token-dense (~2-3 tokens per word)
- Non-Latin scripts use significantly more tokens per character
- URLs and technical strings tokenize inefficiently

### Why Token Limits Matter

```python
import tiktoken

encoder = tiktoken.encoding_for_model("gpt-4")

# Context window budget
MAX_CONTEXT = 128_000  # GPT-4 Turbo
RESERVED_FOR_RESPONSE = 4_096

system_prompt = "You are a helpful assistant that answers questions about software architecture."
system_tokens = len(encoder.encode(system_prompt))

user_context = "Here is the relevant documentation: " + ("word " * 50000)
context_tokens = len(encoder.encode(user_context))

available = MAX_CONTEXT - system_tokens - RESERVED_FOR_RESPONSE
fits = context_tokens <= available

print(f"Context window:      {MAX_CONTEXT:>10,} tokens")
print(f"System prompt:       {system_tokens:>10,} tokens")
print(f"Reserved response:   {RESERVED_FOR_RESPONSE:>10,} tokens")
print(f"Available for input: {available:>10,} tokens")
print(f"User context:        {context_tokens:>10,} tokens")
print(f"Fits in context:     {'Yes' if fits else 'NO — must chunk or summarize'}")
```

---

## Part 3: How LLMs Are Trained

### The Three-Stage Training Pipeline

| Stage | What Happens | Data Size | Goal |
|---|---|---|---|
| **1. Pre-training** | Next-token prediction on internet text | Trillions of tokens | Learn language, facts, reasoning patterns |
| **2. Supervised Fine-Tuning (SFT)** | Train on human-written instruction/response pairs | Thousands to millions of examples | Learn to follow instructions and be helpful |
| **3. RLHF / DPO** | Optimize for human preferences | Thousands of preference comparisons | Learn to be safe, aligned, and high-quality |

### Stage 1: Pre-training (Next Token Prediction)

The core training objective is deceptively simple: predict the next token.

```python
import numpy as np

def demonstrate_pretraining():
    """
    Shows the core pre-training objective: next-token prediction.
    """
    text = "The transformer architecture uses self-attention mechanisms"
    tokens = text.split()  # Simplified tokenization

    print("Pre-training examples from one sentence:")
    print(f"{'Input (context)':<50} {'Target (next token)'}")
    print("-" * 70)
    for i in range(1, len(tokens)):
        context = " ".join(tokens[:i])
        target = tokens[i]
        print(f"{context:<50} {target}")

demonstrate_pretraining()
```

**Output:**
```text
Input (context)                                    Target (next token)
----------------------------------------------------------------------
The                                                transformer
The transformer                                    architecture
The transformer architecture                       uses
The transformer architecture uses                  self-attention
The transformer architecture uses self-attention   mechanisms
```

The model learns from trillions of these examples. By predicting the next token across all of the internet, the model implicitly learns grammar, facts, reasoning, code patterns, and much more.

### Stage 2: Supervised Fine-Tuning (SFT)

After pre-training, the model can predict text but does not know how to follow instructions. SFT teaches it with curated instruction-response pairs.

```python
sft_examples = [
    {
        "instruction": "Explain what a REST API is in simple terms.",
        "response": "A REST API is a way for two computer programs to communicate over the internet. Think of it like a waiter in a restaurant: your program (the customer) sends a request to the API (the waiter), which gets the data from the server (the kitchen) and brings it back to you. REST APIs use standard HTTP methods like GET (read data), POST (create data), PUT (update data), and DELETE (remove data)."
    },
    {
        "instruction": "Write a Python function to check if a string is a palindrome.",
        "response": """def is_palindrome(s: str) -> bool:
    cleaned = ''.join(c.lower() for c in s if c.isalnum())
    return cleaned == cleaned[::-1]

# Examples:
# is_palindrome("racecar")  -> True
# is_palindrome("A man, a plan, a canal: Panama")  -> True
# is_palindrome("hello")  -> False"""
    },
]

# SFT training format (simplified)
for example in sft_examples:
    training_text = f"### Instruction:\n{example['instruction']}\n\n### Response:\n{example['response']}"
    print(training_text[:150] + "...\n")
```

### Stage 3: RLHF and DPO (Alignment)

After SFT, the model is helpful but might produce unsafe, biased, or low-quality responses. RLHF and DPO refine behavior using human preferences.

**RLHF (Reinforcement Learning from Human Feedback):**
1. Generate multiple responses to the same prompt
2. Human raters rank responses from best to worst
3. Train a reward model on these preferences
4. Use reinforcement learning (PPO) to optimize the LLM to produce responses the reward model scores highly

**DPO (Direct Preference Optimization):**
A simpler alternative that skips the reward model and directly optimizes the LLM on preference pairs (chosen response vs rejected response).

```python
# Preference pair example used in RLHF/DPO
preference_example = {
    "prompt": "How do I pick a lock?",
    "chosen": "I can not provide instructions on lock picking as it could be used for illegal purposes. If you are locked out, I recommend contacting a licensed locksmith.",
    "rejected": "To pick a lock, you will need a tension wrench and a pick. Insert the tension wrench into the bottom of the keyhole..."
}

print(f"Prompt: {preference_example['prompt']}")
print(f"\nChosen (preferred): {preference_example['chosen'][:100]}...")
print(f"\nRejected: {preference_example['rejected'][:100]}...")
print("\nThe model learns to produce responses similar to 'chosen' and avoid responses similar to 'rejected'")
```

---

## Part 4: LLM Inference — How Text Is Generated

### The Autoregressive Generation Loop

When you send a prompt, the model generates one token at a time:

```python
import numpy as np

def simulate_generation(prompt_tokens: list[str], max_new_tokens: int = 10) -> list[str]:
    """
    Simulates the autoregressive generation loop.
    In reality, the model computes probability distributions over the vocabulary.
    """
    generated = list(prompt_tokens)
    vocabulary = ["the", "a", "is", "are", "model", "generates", "tokens",
                  "one", "at", "time", "using", "attention", ".", "AI"]

    for step in range(max_new_tokens):
        # In reality: model processes ALL tokens in `generated` and outputs
        # a probability distribution over the vocabulary for the NEXT token
        probs = np.random.dirichlet(np.ones(len(vocabulary)))

        # Sampling strategy determines which token is selected
        next_token_idx = np.random.choice(len(vocabulary), p=probs)
        next_token = vocabulary[next_token_idx]
        generated.append(next_token)

        print(f"  Step {step + 1}: context=[{' '.join(generated[:-1])}] -> '{next_token}'")

        if next_token == ".":
            break

    return generated

print("Autoregressive generation:")
result = simulate_generation(["The", "transformer"])
print(f"\nFull output: {' '.join(result)}")
```

### Temperature, Top-p, and Top-k

These parameters control the randomness and quality of generation:

```python
import numpy as np

def apply_temperature(logits: np.ndarray, temperature: float) -> np.ndarray:
    """
    Temperature controls randomness.
    - temperature < 1.0: more focused, deterministic (picks likely tokens)
    - temperature = 1.0: unchanged probabilities
    - temperature > 1.0: more random, creative (flattens distribution)
    """
    scaled = logits / temperature
    exp_scaled = np.exp(scaled - np.max(scaled))
    return exp_scaled / np.sum(exp_scaled)

def apply_top_p(probs: np.ndarray, top_p: float) -> np.ndarray:
    """
    Top-p (nucleus sampling): only consider tokens whose cumulative
    probability mass is within top_p.
    """
    sorted_indices = np.argsort(probs)[::-1]
    sorted_probs = probs[sorted_indices]
    cumulative = np.cumsum(sorted_probs)

    # Zero out tokens beyond the top_p threshold
    mask = cumulative - sorted_probs <= top_p
    filtered = np.zeros_like(probs)
    filtered[sorted_indices[mask]] = probs[sorted_indices[mask]]
    return filtered / filtered.sum()

def apply_top_k(probs: np.ndarray, top_k: int) -> np.ndarray:
    """
    Top-k: only consider the top_k most likely tokens.
    """
    top_indices = np.argsort(probs)[-top_k:]
    filtered = np.zeros_like(probs)
    filtered[top_indices] = probs[top_indices]
    return filtered / filtered.sum()

# Example: vocabulary of 8 tokens with raw logits from the model
vocab = ["the", "a", "cat", "dog", "transformer", "hello", "world", "AI"]
logits = np.array([2.5, 1.0, 0.5, 0.3, 3.0, -1.0, 0.1, 1.8])

print(f"{'Token':<15} {'Logits':>7} {'T=0.3':>7} {'T=1.0':>7} {'T=1.5':>7}")
print("-" * 50)

for temp in [0.3, 1.0, 1.5]:
    probs = apply_temperature(logits, temp)
    if temp == 0.3:
        for i, token in enumerate(vocab):
            t03 = apply_temperature(logits, 0.3)[i]
            t10 = apply_temperature(logits, 1.0)[i]
            t15 = apply_temperature(logits, 1.5)[i]
            print(f"{token:<15} {logits[i]:>7.1f} {t03:>7.3f} {t10:>7.3f} {t15:>7.3f}")

print("\nTop-p=0.9 filtering:")
base_probs = apply_temperature(logits, 1.0)
top_p_probs = apply_top_p(base_probs, 0.9)
for i, token in enumerate(vocab):
    if top_p_probs[i] > 0:
        print(f"  {token}: {top_p_probs[i]:.3f}")
```

### When to Use Which Settings

| Setting | Use Case | Typical Values |
|---|---|---|
| **Temperature 0** | Factual Q&A, code generation, classification | Exactly 0 (greedy) |
| **Temperature 0.3-0.5** | Summarization, structured output | Balance accuracy and fluency |
| **Temperature 0.7-1.0** | Creative writing, brainstorming | Higher variety |
| **Top-p 0.9** | General-purpose generation | Most common default |
| **Top-p 0.1** | Very focused, deterministic | Factual tasks |
| **Top-k 50** | Prevent very unlikely tokens | Safety net |

---

## Part 5: Model Families and Architectures

### The Three Transformer Variants

| Architecture | Direction | Training Objective | Models | Best For |
|---|---|---|---|---|
| **Decoder-only** | Left-to-right (causal) | Next token prediction | GPT-4, Claude, LLaMA, Mistral | Text generation, chat, code |
| **Encoder-only** | Bidirectional | Masked token prediction | BERT, RoBERTa, DeBERTa | Classification, NER, search |
| **Encoder-Decoder** | Both | Sequence-to-sequence | T5, BART, Flan-T5 | Translation, summarization |

### Key Model Families

```python
model_families = {
    "GPT-4 / GPT-4o": {
        "company": "OpenAI",
        "architecture": "Decoder-only (MoE suspected)",
        "context": "128K tokens",
        "strengths": "Best general reasoning, code, instruction following",
        "access": "API only",
    },
    "Claude 3.5 Sonnet / Claude 4": {
        "company": "Anthropic",
        "architecture": "Decoder-only",
        "context": "200K tokens",
        "strengths": "Long context, safety, nuanced reasoning",
        "access": "API only",
    },
    "LLaMA 3.1 (8B/70B/405B)": {
        "company": "Meta",
        "architecture": "Decoder-only",
        "context": "128K tokens",
        "strengths": "Open weights, fine-tunable, strong performance per size",
        "access": "Open weights (download and run locally)",
    },
    "Mistral / Mixtral": {
        "company": "Mistral AI",
        "architecture": "Decoder-only (Mixtral uses MoE)",
        "context": "32K-128K tokens",
        "strengths": "Efficient, fast, good for fine-tuning",
        "access": "Open weights + API",
    },
    "Gemini 1.5 Pro": {
        "company": "Google",
        "architecture": "Decoder-only (MoE)",
        "context": "1M-2M tokens",
        "strengths": "Massive context window, multimodal",
        "access": "API only",
    },
}

for model, info in model_families.items():
    print(f"\n{model} ({info['company']})")
    print(f"  Architecture: {info['architecture']}")
    print(f"  Context: {info['context']}")
    print(f"  Strengths: {info['strengths']}")
    print(f"  Access: {info['access']}")
```

### Mixture of Experts (MoE)

Modern models like Mixtral and (reportedly) GPT-4 use MoE architecture:

```text
Input Token
    |
  Router (learned gating network)
    |
  Selects 2 out of 8 experts
    |
  Expert 1 output + Expert 5 output (weighted sum)
    |
  Output Token
```

**Why MoE matters:** A model with 8 experts of 7B parameters each has 56B total parameters but only activates ~14B per token (2 experts). This gives near-56B quality at 14B inference cost.

### Scaling Laws

Research has shown predictable relationships between model performance and:

| Factor | Effect |
|---|---|
| **Parameters** | More parameters = better performance (with diminishing returns) |
| **Training data** | More data = better performance (must scale with parameters) |
| **Compute** | More FLOPs = better performance (optimal allocation matters) |
| **Chinchilla scaling** | Optimal: ~20 tokens per parameter. A 7B model needs ~140B tokens |

---

## Part 6: Why Models Hallucinate

### Understanding Hallucination

Hallucination occurs when a model generates text that is fluent and confident but factually incorrect. This happens because:

1. **Training objective is next-token prediction, not truth**: The model learns to produce statistically plausible text, not verified facts
2. **Compressed knowledge**: Billions of facts compressed into model weights lead to lossy recall
3. **No grounding**: The model has no access to external databases or the internet during inference
4. **Sycophancy**: RLHF training can make models agree with the user even when the user is wrong

### Types of Hallucination

| Type | Example | Cause |
|---|---|---|
| **Factual** | "Python was created by James Gosling in 1991" | Wrong fact stored in weights |
| **Fabrication** | Inventing a research paper that does not exist | Pattern completion without grounding |
| **Logical** | "Since all dogs are mammals and all cats are mammals, all dogs are cats" | Superficial pattern matching |
| **Temporal** | Giving outdated information as current | Knowledge cutoff |

### Mitigation Strategies

```python
mitigation_strategies = {
    "RAG (Retrieval-Augmented Generation)": {
        "how": "Retrieve relevant documents and include them in the prompt",
        "effectiveness": "High — grounds responses in real data",
        "cost": "Additional infrastructure for retrieval",
    },
    "Temperature = 0": {
        "how": "Use greedy decoding for factual queries",
        "effectiveness": "Moderate — reduces random errors",
        "cost": "Free — just a parameter change",
    },
    "Chain of Thought": {
        "how": "Ask the model to reason step by step",
        "effectiveness": "Moderate — catches some logical errors",
        "cost": "More output tokens = higher cost",
    },
    "Self-consistency": {
        "how": "Generate multiple responses and take the majority answer",
        "effectiveness": "High for reasoning tasks",
        "cost": "N times the inference cost",
    },
    "Fine-tuning with verified data": {
        "how": "Train on domain-specific, verified Q&A pairs",
        "effectiveness": "High for specific domains",
        "cost": "Data curation + training compute",
    },
}

for strategy, info in mitigation_strategies.items():
    print(f"\n{strategy}")
    print(f"  How: {info['how']}")
    print(f"  Effectiveness: {info['effectiveness']}")
    print(f"  Cost: {info['cost']}")
```

---

## Part 7: KV Cache and Inference Optimization

### The KV Cache Problem

During generation, each new token requires attending to all previous tokens. Without optimization, generating token N requires recomputing attention for all N-1 previous tokens — quadratic cost.

The **KV cache** stores the Key and Value projections from previous tokens so they are not recomputed:

```python
def demonstrate_kv_cache():
    """
    Shows why KV cache is critical for efficient inference.
    """
    seq_lengths = [100, 1000, 10000, 100000]
    d_model = 4096
    num_layers = 80
    bytes_per_float = 2  # float16

    print(f"{'Seq Length':>10} {'KV Cache Size':>15} {'Without Cache (recompute)':>30}")
    print("-" * 60)

    for seq_len in seq_lengths:
        # KV cache stores K and V for each layer
        # Shape per layer: 2 * (seq_len, d_model) for K and V
        cache_size = 2 * seq_len * d_model * num_layers * bytes_per_float
        cache_gb = cache_size / (1024 ** 3)

        # Without cache: each new token recomputes all previous K,V
        # Total FLOPs scales quadratically
        recompute_factor = seq_len * (seq_len + 1) / 2  # triangular sum

        print(f"{seq_len:>10,} {cache_gb:>14.2f}GB {recompute_factor:>29,.0f} ops")

demonstrate_kv_cache()
```

### Key Inference Optimizations

| Technique | What It Does | Speedup |
|---|---|---|
| **KV Cache** | Stores previous Key/Value pairs | ~N times faster (linear vs quadratic) |
| **Flash Attention** | Fuses attention computation, reduces memory I/O | 2-4x faster |
| **Quantization** | Reduces precision (FP16 to INT8 or INT4) | 2-4x less memory, faster |
| **Speculative decoding** | Small model drafts tokens, large model verifies in batch | 2-3x faster |
| **Continuous batching** | Dynamically batches requests of different lengths | Higher throughput |

---

## Summary

| Concept | What You Should Know |
|---|---|
| Transformer blocks | Attention + feed-forward, stacked N times |
| Self-attention | Q, K, V projections, softmax scoring, weighted aggregation |
| Causal masking | Decoder-only models only see past tokens |
| Tokenization | BPE/WordPiece, tokens != words, affects cost and context |
| Pre-training | Next-token prediction on trillions of tokens |
| SFT | Instruction-following from curated examples |
| RLHF/DPO | Alignment via human preferences |
| Temperature/top-p | Control generation randomness and quality |
| Model families | GPT, Claude, LLaMA, Mistral, Gemini — know the trade-offs |
| Hallucination | Statistical plausibility != truth; RAG is the primary mitigation |
| KV cache | Essential for efficient inference at scale |

---

## Interview Questions

- **[L1]** What is the transformer architecture, and why did it replace RNNs for language modeling?
- **[L1]** Explain the difference between pre-training, supervised fine-tuning, and RLHF in LLM development.
- **[L2]** How does self-attention work? Walk through the Query, Key, Value computation and explain what attention weights represent.
- **[L2]** What is the difference between decoder-only (GPT), encoder-only (BERT), and encoder-decoder (T5) architectures? When would you choose each?
- **[L2]** Explain temperature and top-p sampling. How do they affect LLM output, and what values would you use for a factual Q&A system vs a creative writing assistant?
- **[L3]** Why do LLMs hallucinate, and what are the most effective strategies to mitigate hallucination in production systems?
- **[L3]** Explain the KV cache, why it is necessary, and how it affects memory requirements for long-context inference. How would you estimate the memory needed for a 70B parameter model serving 128K context?
- **[L3]** Compare GPT-4, Claude, LLaMA 3, and Mistral for a production enterprise application. What factors would drive your model selection, and when would you choose open-weight models over API-based models?

## Interview Answers

1. The transformer architecture (introduced in "Attention Is All You Need," 2017) uses self-attention to let every token directly attend to every other token in parallel, replacing the sequential processing of RNNs. This solved two critical problems: training could be parallelized across GPUs (enabling scaling to billions of parameters), and long-range dependencies were captured directly rather than degrading through sequential steps. Every major LLM today (GPT-4, Claude, LLaMA) is built on transformers.
2. **Pre-training** teaches the model language by predicting the next token on trillions of tokens of internet text — the model learns grammar, facts, reasoning patterns, and code. **SFT (Supervised Fine-Tuning)** takes the pre-trained model and trains it on curated instruction/response pairs so it learns to follow instructions and be helpful. **RLHF (Reinforcement Learning from Human Feedback)** further refines the model by training on human preference rankings — humans compare multiple responses and the model learns to produce responses that align with human values (safety, helpfulness, honesty).
3. Each token's embedding is projected through three learned weight matrices to produce Query (Q), Key (K), and Value (V) vectors. Attention scores are computed as `Q · K^T / sqrt(d_k)` — this measures how relevant each key is to each query. Softmax normalizes scores to weights that sum to 1. The output is a weighted sum of Value vectors: `softmax(QK^T/sqrt(d_k)) · V`. The attention weights represent how much information each token gathers from every other token. Multi-head attention runs this computation multiple times with different projections, allowing the model to capture different types of relationships simultaneously.
4. **Decoder-only** (GPT, Claude, LLaMA) uses causal masking — each token only sees previous tokens. Best for text generation, chat, and code. **Encoder-only** (BERT) uses bidirectional attention — each token sees all other tokens. Best for classification, named entity recognition, and semantic search (understanding meaning, not generating text). **Encoder-decoder** (T5, BART) uses the encoder to understand input and the decoder to generate output. Best for translation, summarization, and tasks with clear input-output structure. For most GenAI applications today, decoder-only models dominate because they handle generation tasks natively.
5. **Temperature** scales the logits before softmax: lower values make the distribution sharper (more deterministic), higher values flatten it (more random). At temperature 0, the model always picks the highest-probability token (greedy decoding). **Top-p** (nucleus sampling) truncates the distribution to only the smallest set of tokens whose cumulative probability exceeds p. For a factual Q&A system, use temperature 0-0.2 with top-p 0.1 to minimize randomness and hallucination. For creative writing, use temperature 0.8-1.0 with top-p 0.9 to encourage variety while avoiding very unlikely tokens.
6. LLMs hallucinate because their training objective is next-token prediction (statistical plausibility), not factual accuracy. Knowledge is compressed into weights with lossy recall. RLHF can cause sycophancy (agreeing with the user). Most effective mitigations: **(1) RAG** — retrieve verified documents and include in the prompt, grounding responses in real data. **(2) Temperature 0** for factual queries. **(3) Structured output with citations** — force the model to cite sources, making hallucinations detectable. **(4) Evaluation pipelines** — automated checks comparing responses against known facts. **(5) Fine-tuning on domain-specific verified data** for specialized applications.
7. The KV cache stores the Key and Value projections from all previous tokens at each layer, so they are not recomputed when generating each new token. Without it, generating token N requires reprocessing all N-1 previous tokens — O(N^2) total computation for N tokens. With the cache, each new token only computes its own Q/K/V and attends to cached K/V — O(N) per token. For a 70B model with 80 layers, d_model=8192, using float16: KV cache per token = 2 (K+V) * 80 layers * 8192 dims * 2 bytes = ~2.6MB per token. At 128K context: ~327GB just for KV cache. This is why long-context inference requires multiple GPUs and techniques like GQA (Grouped Query Attention) which reduces KV heads.
8. **GPT-4**: Best general reasoning and code, most battle-tested for enterprises, but API-only with data sent to OpenAI. **Claude**: Excellent long context (200K), strong safety, good for regulated industries, API-only (Anthropic). **LLaMA 3 70B**: Strong open-weight model, can run on-premises for data sovereignty, fine-tunable for domain specialization, but requires GPU infrastructure. **Mistral/Mixtral**: Efficient MoE architecture, good cost/performance ratio, available as both open weights and API. Key decision factors: (1) Data sovereignty — if data cannot leave your infrastructure, choose open weights (LLaMA, Mistral). (2) Fine-tuning needs — open weights only. (3) Context length requirements — Gemini (1M+) or Claude (200K) for very long context. (4) Cost — open weights on your GPUs for high-volume, API for low-volume. (5) Compliance — some industries require on-premises deployment.
