---
id: genai-transformers
slug: transformers
title: Transformers, Tokens, and Attention
category: genai
categoryTitle: Generative AI
difficulty: advanced
estimatedMinutes: 60
version:
  minimum: "Framework agnostic concepts"
prerequisites: []
tags: [genai, transformers, attention, tokens, llm]
relatedTopics: [genai-embeddings-vector-db, genai-rag-fundamentals]
order: 10
status: published
---
# Transformers, Tokens, and Attention

## Introduction
Transformers are the neural architecture behind most modern large language models. Text is split into tokens, tokens become vectors, and attention lets each token use relevant information from other tokens in the context.

```text
Text
 ↓ tokenizer
Tokens
 ↓ token embeddings + position information
Transformer layers
 ├── self-attention
 ├── feed-forward network
 ├── residual connections
 └── normalization
 ↓
next-token probabilities
```

## Purpose
Transformers solved important limitations of sequential RNN-style models: long-range relationships are easier to model and training can process many sequence positions in parallel on GPUs/TPUs.

## Tokens
A token is not always a word. It may be a word, subword, punctuation, or part of a word. Token count affects:

- Context-window capacity.
- Input/output cost.
- Latency.
- Chunking choices.
- How code, languages, and identifiers are represented.

```python
tokens = tokenizer.encode("Order status: paid")
print(len(tokens))
```

## Self-attention
Each token produces Query, Key, and Value vectors:

```text
Query: what information is this token looking for?
Key:   what information does this token identify as?
Value: what content is contributed if selected?

attention = softmax(Q × Kᵀ / √dimension) × V
```

For “The bank near the river,” attention can connect `bank` to `river` and help resolve meaning from context.

## Professional company-level example
A model serving system must consider:

```text
Longer prompt
    ↓
More tokens
    ↓
More attention compute/memory
    ↓
Higher latency/cost
    ↓
Possible context-window overflow
```

Production systems therefore use token counting, truncation policy, chunking, summarization, retrieval, prompt budgets, batching, and model-specific cost/latency measurements.

## Important limitations
- Standard self-attention cost grows roughly quadratically with sequence length.
- Attention weights are not a complete explanation of model reasoning.
- A model can follow malicious instructions embedded in context.
- More context is not always better; irrelevant context can dilute useful evidence.
- A larger model is not automatically more accurate for every domain/task.

## Comparison
| Architecture | Strength | Limitation |
|---|---|---|
| RNN/LSTM | Sequential state, older/smaller workloads | Harder long-range/parallel training |
| Transformer | Parallel training/context relationships | Attention cost/context limits |
| Fine-tuned model | Specialized behavior | Training/data/update cost |
| RAG system | Current/private source grounding | Retrieval/index/prompt complexity |

## Interview Questions
- **[L1]** What problem did Transformers solve compared with RNNs?
- **[L1]** What is a token?
- **[L2]** What are Query, Key, and Value in attention?
- **[L2]** Why does standard self-attention become expensive for long context?
- **[L3]** How do token/context limits affect production LLM architecture?
- **[L3]** Why is prompt injection a structural concern for attention-based models?

## Interview Answers
1. They allow direct relationships between sequence positions and parallel training rather than processing every token strictly one after another.
2. A token is a model-specific text unit, such as a word, subword, punctuation mark, or identifier fragment.
3. Query represents what a token seeks, Key what it offers for matching, and Value the information contributed after attention weights are calculated.
4. Every token is compared with every other token, creating roughly an N×N attention matrix as sequence length N grows.
5. Design token budgets, chunking/retrieval, truncation/summarization, model selection, batching, latency/cost limits, and context-quality evaluation.
6. The model attends over trusted instructions and untrusted text in one context; the architecture does not create a perfect privilege boundary, so input isolation, retrieval filtering, tool authorization, and output validation are needed.

## Expert perspective
Transformer knowledge explains practical LLM engineering: token budgets, context windows, cost, long-document chunking, retrieval quality, and prompt-injection risk. It is more useful to understand these consequences than to memorize only the attention formula.
