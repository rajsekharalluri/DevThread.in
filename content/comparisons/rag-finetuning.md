---
id: comparisons-rag-finetuning
slug: rag-finetuning
title: RAG vs Fine-Tuning vs Prompting
category: comparisons
categoryTitle: Decision Guides
difficulty: advanced
estimatedMinutes: 35
version:
  minimum: "Generative AI concepts"
prerequisites: [genai-rag-fundamentals, genai-transformers]
tags: [genai, rag, fine-tuning, prompting, comparison]
relatedTopics: [genai-production-evaluation, genai-security-guardrails]
order: 70
status: published
---
# RAG vs Fine-Tuning vs Prompting

## Decision table

| Approach | Changes | Best fit | Main cost |
|---|---|---|---|
| Prompting | Instructions/context per request | Behavior/task guidance | Context/token limits |
| RAG | Retrieved external knowledge | Current/private/traceable facts | Retrieval/index/evaluation |
| Fine-tuning | Model behavior/weights | Repeated style/task patterns | Data/training/update cost |

## Example

```text
Need current company policy → RAG
Need answer format/tone → Prompt/structured output
Need consistent classification behavior → Evaluate prompting, then fine-tune if justified
```

Fine-tuning does not reliably turn a model into a live database. RAG does not automatically make reasoning/style consistent. Use each for the problem it solves.

## Interview Questions
- **[L1]** What problem does RAG solve?
- **[L1]** What does fine-tuning change?
- **[L2]** Why is prompting not a knowledge-storage strategy?
- **[L2]** When does fine-tuning make sense?
- **[L3]** How do you choose between RAG and fine-tuning for private company knowledge?
- **[L3]** Can RAG and fine-tuning be combined?

## Interview Answers
1. It retrieves current/private source context at request time for grounded generation.
2. It updates model behavior/weights from training examples; it is not a simple live document lookup.
3. Prompts have context/token limits, can be stale/manual, and do not make new knowledge part of model weights.
4. When high-quality representative data supports a repeated behavior/style/task and evaluation proves benefit over prompting.
5. Start with authorized RAG for knowledge freshness/citations; fine-tune only for behavior/style/classification after evaluation.
6. Yes: fine-tune behavior/format while RAG supplies current/private facts, with separate evaluation for both.
