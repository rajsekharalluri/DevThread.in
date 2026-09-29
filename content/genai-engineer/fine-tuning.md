---
id: genai-eng-fine-tuning
slug: fine-tuning
title: "Module 11: Fine-Tuning & Model Adaptation"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 110
version:
  minimum: "transformers 4.4x, peft 0.12+, trl 0.12+, bitsandbytes"
prerequisites: [genai-eng-mcp-tool-ecosystem]
tags: [genai-engineer, fine-tuning, sft, lora, qlora, peft, dpo, distillation, quantization]
relatedTopics: [genai-eng-multimodal]
order: 11
status: published
---
# Module 11: Fine-Tuning & Model Adaptation

## Introduction

Prompting and RAG handle most GenAI use cases. But sometimes you need the model itself to behave differently: always produce a strict output format, adopt a brand voice, master a narrow domain task, run cheaply as a small model that performs like a large one, or run entirely on-premises.

**Fine-tuning** continues training a pre-trained model on your own examples so its weights adapt to your task. Modern **parameter-efficient fine-tuning (PEFT)** methods like **LoRA** and **QLoRA** make this possible on a single GPU by training only a tiny fraction of parameters.

This module covers when (and when not) to fine-tune, the adaptation spectrum, dataset preparation, full fine-tuning vs PEFT, LoRA and QLoRA in depth with working code, preference tuning (DPO), distillation, hosted fine-tuning APIs, evaluation, and serving fine-tuned models.

---

## Part 1: When Should You Fine-Tune?

### The Adaptation Ladder

Always climb this ladder from the bottom. Each step costs more.

| Step | Technique | Changes | Effort |
|---|---|---|---|
| 1 | **Prompt engineering** | Instructions and examples | Hours |
| 2 | **RAG** | Knowledge available at query time | Days |
| 3 | **Fine-tuning (PEFT)** | Model behavior via small adapter weights | Days-weeks |
| 4 | **Full fine-tuning** | All model weights | Weeks, expensive GPUs |
| 5 | **Continued pre-training** | Domain language on large unlabeled corpora | Weeks-months, large compute |
| 6 | **Pre-training from scratch** | Everything | Millions of dollars |

### Fine-Tune For Behavior, Not Knowledge

| Good Reasons to Fine-Tune | Poor Reasons to Fine-Tune |
|---|---|
| Consistent output format / structure that prompting cannot achieve reliably | Adding facts that change frequently (use RAG) |
| Specific tone, style, or brand voice | "The model doesn't know our documents" (use RAG) |
| Narrow, high-volume task where a small fine-tuned model can replace a large model (cost/latency) | You have fewer than ~100 quality examples |
| Domain-specific language or classification (medical codes, legal clauses) | You have not tried good prompts and few-shot examples yet |
| Reducing prompt length (bake long instructions into weights) | You need citations to sources |
| On-prem / offline requirements with open models | Task requirements are still changing weekly |

### Decision Example

**Scenario:** A support platform classifies 2 million tickets per month into 45 categories. GPT-4o with a long few-shot prompt reaches 93% accuracy but costs too much.

**Solution:** Use GPT-4o outputs (reviewed by humans) to create 5,000 labeled examples, fine-tune a small open model (e.g. 8B) with LoRA, reach ~92-94% accuracy at a fraction of the cost and latency. This is **distillation** plus fine-tuning - one of the most valuable production uses.

---

## Part 2: Types of Fine-Tuning

| Type | Data | Purpose |
|---|---|---|
| **Supervised Fine-Tuning (SFT)** | Input to ideal output pairs (instructions, conversations) | Teach tasks, formats, style |
| **Preference tuning (RLHF, DPO, ORPO, KTO)** | Prompt with chosen vs rejected responses | Align outputs with preferences (helpfulness, tone, safety) |
| **Continued pre-training (domain adaptation)** | Raw domain text | Learn domain vocabulary and patterns |
| **Reinforcement fine-tuning (RFT)** | Tasks with a programmatic grader/reward | Improve reasoning on verifiable tasks (math, code, extraction) |
| **Embedding model fine-tuning** | Query to relevant passage pairs | Improve retrieval for your domain (RAG) |

---

## Part 3: Dataset Preparation - The Most Important Part

Model quality is bounded by data quality. **1,000 excellent examples beat 50,000 noisy ones.**

### Chat Format (JSONL)

Most frameworks and hosted APIs use the chat messages format, one example per line:

```json
{"messages": [{"role": "system", "content": "You classify support tickets. Reply with JSON."}, {"role": "user", "content": "I was charged twice for March."}, {"role": "assistant", "content": "{\"category\": \"billing\", \"subcategory\": \"duplicate_charge\", \"priority\": \"high\"}"}]}
{"messages": [{"role": "system", "content": "You classify support tickets. Reply with JSON."}, {"role": "user", "content": "The app crashes when I open settings on Android."}, {"role": "assistant", "content": "{\"category\": \"technical\", \"subcategory\": \"mobile_crash\", \"priority\": \"medium\"}"}]}
```

### Data Quality Checklist

| Check | Why |
|---|---|
| **Correctness** | The model learns your mistakes too - have experts review |
| **Consistency** | Same input style gets the same output style; contradictory labels confuse training |
| **Diversity** | Cover the real distribution: lengths, topics, edge cases, languages |
| **Balance** | Avoid 90% of one class unless that reflects reality and is intended |
| **Deduplication** | Duplicates cause memorization and inflate evaluation |
| **No leakage** | Test examples must not appear in training data |
| **Privacy** | Remove or mask PII and secrets - models can memorize and regurgitate |
| **Format fidelity** | The exact format you want at inference, including system prompt |

### Building and Validating a Dataset

```python
import json
import random
import hashlib
from collections import Counter

def load_jsonl(path: str) -> list[dict]:
    with open(path, encoding="utf-8") as f:
        return [json.loads(line) for line in f if line.strip()]

def validate_example(ex: dict) -> list[str]:
    errors = []
    msgs = ex.get("messages", [])
    if not msgs or msgs[-1]["role"] != "assistant":
        errors.append("last message must be from assistant")
    for m in msgs:
        if m["role"] not in {"system", "user", "assistant"}:
            errors.append(f"invalid role {m['role']}")
        if not m.get("content", "").strip():
            errors.append("empty content")
    try:
        json.loads(msgs[-1]["content"])  # our task requires JSON outputs
    except Exception:
        errors.append("assistant output is not valid JSON")
    return errors

def dedupe(examples: list[dict]) -> list[dict]:
    seen, unique = set(), []
    for ex in examples:
        key = hashlib.md5(ex["messages"][-2]["content"].strip().lower().encode()).hexdigest()
        if key not in seen:
            seen.add(key)
            unique.append(ex)
    return unique

data = [ex for ex in dedupe(load_jsonl("tickets_raw.jsonl")) if not validate_example(ex)]
print("Label distribution:", Counter(json.loads(ex["messages"][-1]["content"])["category"] for ex in data))

random.seed(42)
random.shuffle(data)
n = len(data)
splits = {"train": data[: int(0.8 * n)], "val": data[int(0.8 * n): int(0.9 * n)], "test": data[int(0.9 * n):]}
for name, rows in splits.items():
    with open(f"{name}.jsonl", "w", encoding="utf-8") as f:
        f.writelines(json.dumps(r, ensure_ascii=False) + "\n" for r in rows)
    print(name, len(rows))
```

### Synthetic Data Generation

When labeled data is scarce, use a strong model to generate examples - then **filter and human-review** them.

```python
from openai import OpenAI
from pydantic import BaseModel

client = OpenAI()

class SyntheticTicket(BaseModel):
    ticket_text: str
    category: str
    subcategory: str
    priority: str

def generate_examples(category: str, n: int = 10) -> list[SyntheticTicket]:
    out = []
    for _ in range(n):
        r = client.beta.chat.completions.parse(
            model="gpt-4o",
            temperature=1.0,
            messages=[{"role": "user", "content": f"Write one realistic, varied customer support ticket for category '{category}' and label it."}],
            response_format=SyntheticTicket,
        )
        out.append(r.choices[0].message.parsed)
    return out
```

Check the model provider's terms of use before using its outputs to train other models.

### How Much Data?

| Goal | Typical Examples |
|---|---|
| Format / style adaptation | 50-500 |
| Narrow classification / extraction | 500-5,000 |
| Complex domain task | 5,000-50,000 |
| Distilling a large model's behavior broadly | 10,000+ |

Start small, measure, and add data where evaluation shows errors.

---

## Part 4: Full Fine-Tuning vs PEFT

### Memory Math for Full Fine-Tuning

Training a 7B model in full precision with Adam needs roughly:

```text
Weights (fp16/bf16):      7B x 2 bytes  = 14 GB
Gradients (bf16):         7B x 2 bytes  = 14 GB
Adam optimizer states:    7B x 8 bytes  = 56 GB  (fp32 momentum + variance)
Master weights (fp32):    7B x 4 bytes  = 28 GB
Activations:              varies with batch size and sequence length
Total:                    ~110+ GB -> multiple 80 GB GPUs
```

### Parameter-Efficient Fine-Tuning (PEFT)

PEFT freezes the base model and trains a small number of new parameters.

| Method | Idea |
|---|---|
| **LoRA** | Add small low-rank matrices to attention/MLP weights |
| **QLoRA** | LoRA on top of a 4-bit quantized frozen base model |
| **Prefix / prompt tuning** | Learn virtual tokens prepended to input |
| **Adapters** | Insert small trainable layers between frozen layers |
| **DoRA** | LoRA variant decomposing magnitude and direction |

LoRA and QLoRA dominate in practice.

---

## Part 5: LoRA - How It Works

### The Core Idea

A weight matrix `W` in a transformer might be 4096 x 4096 (~16.8M parameters). Fine-tuning changes it to `W + ΔW`. LoRA's insight: **ΔW is low-rank** - the useful update can be expressed as the product of two thin matrices.

```text
ΔW = B x A
W: d x d (frozen)     A: r x d (trainable)     B: d x r (trainable)     r << d (e.g. 8, 16, 32)

Forward pass: h = W·x + (alpha / r) · B·A·x
```

For d = 4096 and r = 16: `A` + `B` = 2 x 4096 x 16 = **131K parameters instead of 16.8M** (0.8%).

```python
import numpy as np

d, r = 4096, 16
full_params = d * d
lora_params = 2 * d * r
print(f"Full: {full_params:,}  LoRA: {lora_params:,}  Ratio: {lora_params / full_params:.2%}")

# LoRA forward pass illustration
W = np.random.randn(d, d).astype(np.float32) * 0.01    # frozen pretrained weight
A = np.random.randn(r, d).astype(np.float32) * 0.01    # trainable, random init
B = np.zeros((d, r), dtype=np.float32)                  # trainable, zero init -> ΔW starts at 0
alpha = 32
x = np.random.randn(d).astype(np.float32)
h = W @ x + (alpha / r) * (B @ (A @ x))
```

`B` is initialized to zero, so at the start of training the model behaves exactly like the base model.

### Key LoRA Hyperparameters

| Parameter | Typical | Effect |
|---|---|---|
| `r` (rank) | 8-64 | Capacity of the adapter; higher = more expressive, more memory |
| `lora_alpha` | 16-64 (often 2 x r) | Scaling of the update |
| `target_modules` | `q_proj, k_proj, v_proj, o_proj` (+ `gate_proj, up_proj, down_proj`) | Which layers get adapters; targeting all linear layers usually works best |
| `lora_dropout` | 0.0-0.1 | Regularization |
| Learning rate | 1e-4 to 2e-4 | Higher than full fine-tuning |

### Benefits of LoRA

- **Tiny adapters** (10-200 MB) instead of full model copies (14+ GB)
- **Swap adapters** per task or customer on the same base model
- **Merge** adapter into base weights for zero inference overhead
- **Serve many adapters** concurrently on one GPU (multi-LoRA serving in vLLM, LoRAX)

---

## Part 6: QLoRA - Fine-Tuning on a Single GPU

QLoRA quantizes the frozen base model to **4-bit (NF4)**, keeps LoRA adapters in 16-bit, and uses paged optimizers to handle memory spikes. A 7-8B model can be fine-tuned on a single 24 GB GPU; a 70B model on a single 80 GB GPU (slowly).

### Complete QLoRA SFT Example

```python
# pip install torch transformers peft trl datasets bitsandbytes accelerate
import torch
from datasets import load_dataset
from transformers import AutoTokenizer, AutoModelForCausalLM, BitsAndBytesConfig
from peft import LoraConfig, prepare_model_for_kbit_training
from trl import SFTTrainer, SFTConfig

BASE_MODEL = "meta-llama/Llama-3.1-8B-Instruct"   # any instruct model you have access to

# 1. Load the base model in 4-bit
bnb_config = BitsAndBytesConfig(
    load_in_4bit=True,
    bnb_4bit_quant_type="nf4",
    bnb_4bit_compute_dtype=torch.bfloat16,
    bnb_4bit_use_double_quant=True,
)
tokenizer = AutoTokenizer.from_pretrained(BASE_MODEL)
tokenizer.pad_token = tokenizer.pad_token or tokenizer.eos_token

model = AutoModelForCausalLM.from_pretrained(
    BASE_MODEL,
    quantization_config=bnb_config,
    device_map="auto",
    torch_dtype=torch.bfloat16,
)
model = prepare_model_for_kbit_training(model)

# 2. LoRA configuration
lora_config = LoraConfig(
    r=16,
    lora_alpha=32,
    lora_dropout=0.05,
    target_modules=["q_proj", "k_proj", "v_proj", "o_proj", "gate_proj", "up_proj", "down_proj"],
    task_type="CAUSAL_LM",
)

# 3. Dataset in chat "messages" format (train.jsonl / val.jsonl from Part 3)
dataset = load_dataset("json", data_files={"train": "train.jsonl", "validation": "val.jsonl"})

# 4. Training configuration
training_args = SFTConfig(
    output_dir="./ticket-classifier-lora",
    num_train_epochs=3,
    per_device_train_batch_size=4,
    gradient_accumulation_steps=4,        # effective batch size 16
    learning_rate=2e-4,
    lr_scheduler_type="cosine",
    warmup_ratio=0.03,
    logging_steps=10,
    eval_strategy="steps",
    eval_steps=100,
    save_steps=100,
    bf16=True,
    gradient_checkpointing=True,
    max_seq_length=1024,
    report_to="none",
)

# 5. Train (SFTTrainer applies the tokenizer's chat template to "messages")
trainer = SFTTrainer(
    model=model,
    args=training_args,
    train_dataset=dataset["train"],
    eval_dataset=dataset["validation"],
    peft_config=lora_config,
    processing_class=tokenizer,
)
trainer.model.print_trainable_parameters()   # e.g. "trainable params: 42M || all params: 8B || trainable%: 0.5"
trainer.train()
trainer.save_model("./ticket-classifier-lora/final")   # saves only the adapter
```

### Inference with the Adapter and Merging

```python
from peft import PeftModel

base = AutoModelForCausalLM.from_pretrained(BASE_MODEL, torch_dtype=torch.bfloat16, device_map="auto")
model = PeftModel.from_pretrained(base, "./ticket-classifier-lora/final")

messages = [
    {"role": "system", "content": "You classify support tickets. Reply with JSON."},
    {"role": "user", "content": "My refund from last week still hasn't arrived."},
]
inputs = tokenizer.apply_chat_template(messages, add_generation_prompt=True, return_tensors="pt").to(model.device)
output = model.generate(inputs, max_new_tokens=60, do_sample=False)
print(tokenizer.decode(output[0][inputs.shape[-1]:], skip_special_tokens=True))

# Merge adapter into base weights for deployment (load base in bf16, not 4-bit, before merging)
merged = model.merge_and_unload()
merged.save_pretrained("./ticket-classifier-merged")
tokenizer.save_pretrained("./ticket-classifier-merged")
```

### Reading Training Curves

| Pattern | Meaning | Action |
|---|---|---|
| Train and val loss both decrease and flatten | Healthy | Evaluate on test set |
| Train loss decreases, val loss rises | Overfitting | Fewer epochs, more data, more dropout, lower rank |
| Both losses stay high | Underfitting / bad data | Higher LR or rank, check data formatting |
| Loss spikes / NaN | Instability | Lower LR, check for bad examples, use bf16 |

**Loss is not the goal.** Always evaluate with task metrics (accuracy, F1, JSON validity, human ratings) on a held-out test set.

### Catastrophic Forgetting

Fine-tuning on a narrow task can degrade general abilities. Mitigations: LoRA (base weights frozen), fewer epochs, lower learning rate, mixing in some general instruction data, and evaluating general benchmarks alongside task metrics.

---

## Part 7: Preference Tuning with DPO

SFT teaches **what** a good answer looks like. Preference tuning teaches **which of two answers is better** - useful for tone, helpfulness, conciseness, and safety where "correct" is subjective.

**DPO (Direct Preference Optimization)** trains directly on `(prompt, chosen, rejected)` triples without a separate reward model or RL loop, making it far simpler than classic RLHF with PPO.

```json
{"prompt": "Customer: My order is late again!", "chosen": "I'm sorry your order is delayed again. I've checked it: it's arriving tomorrow by 6 PM. I've added a 10% credit to your account for the trouble.", "rejected": "Orders can be delayed due to many factors. Please wait."}
```

```python
from trl import DPOTrainer, DPOConfig

pref_dataset = load_dataset("json", data_files="preferences.jsonl")["train"]

dpo_args = DPOConfig(
    output_dir="./support-tone-dpo",
    beta=0.1,                        # how strongly to stay close to the reference model
    learning_rate=5e-6,
    per_device_train_batch_size=2,
    gradient_accumulation_steps=8,
    num_train_epochs=1,
    bf16=True,
)

dpo_trainer = DPOTrainer(
    model=sft_model,                 # typically start from your SFT model
    args=dpo_args,
    train_dataset=pref_dataset,
    processing_class=tokenizer,
    peft_config=lora_config,
)
dpo_trainer.train()
```

Preference pairs come from human ratings, A/B feedback in your product (thumbs up/down), or a strong model acting as judge (with human spot-checks).

---

## Part 8: Distillation and Model Compression

### Knowledge Distillation

Train a small **student** model to imitate a large **teacher** model.

```text
1. Collect real inputs from your task (unlabeled prompts)
2. Generate outputs with the teacher (large model), optionally with reasoning
3. Filter outputs with validators and human review
4. Fine-tune the student (small model) on teacher outputs via SFT
5. Evaluate student vs teacher on a held-out test set
```

Hosted providers offer distillation workflows (store completions from a large model, fine-tune a smaller one on them).

### Quantization for Deployment

| Format | Bits | Use |
|---|---|---|
| BF16 / FP16 | 16 | Standard GPU inference |
| FP8 | 8 | Modern GPUs (H100+), near-lossless |
| INT8 / GPTQ / AWQ | 8 / 4 | GPU inference with lower memory |
| GGUF (llama.cpp) | 2-8 | CPU / laptop / edge inference, Ollama |

A 4-bit 8B model fits in ~5-6 GB of memory, enabling laptop and edge deployment.

---

## Part 9: Hosted Fine-Tuning APIs

If you use closed models, providers offer managed fine-tuning.

```python
from openai import OpenAI

client = OpenAI()

train_file = client.files.create(file=open("train.jsonl", "rb"), purpose="fine-tune")
val_file = client.files.create(file=open("val.jsonl", "rb"), purpose="fine-tune")

job = client.fine_tuning.jobs.create(
    model="gpt-4o-mini-2024-07-18",
    training_file=train_file.id,
    validation_file=val_file.id,
    suffix="ticket-classifier",
)
print(job.id, job.status)

# Poll status / events
for event in client.fine_tuning.jobs.list_events(fine_tuning_job_id=job.id, limit=10).data:
    print(event.message)

# When complete, use the returned model name like any other model
# job = client.fine_tuning.jobs.retrieve(job.id); job.fine_tuned_model -> "ft:gpt-4o-mini-2024-07-18:org:ticket-classifier:abc123"
```

### Hosted vs Self-Hosted Fine-Tuning

| | Hosted (OpenAI, Azure OpenAI, Vertex AI, Bedrock) | Self-hosted (open models + PEFT) |
|---|---|---|
| Effort | Low - upload data, start job | High - GPUs, training code, serving |
| Control | Limited hyperparameters | Full control |
| Data | Sent to provider (check agreements) | Stays in your environment |
| Cost model | Per training token + higher inference price | GPU hours + your serving infra |
| Portability | Locked to provider | Own the weights |

---

## Part 10: Evaluating Fine-Tuned Models

```python
import json
from sklearn.metrics import classification_report

def evaluate(predict_fn, test_path: str = "test.jsonl"):
    y_true, y_pred, invalid = [], [], 0
    for ex in load_jsonl(test_path):
        expected = json.loads(ex["messages"][-1]["content"])["category"]
        raw = predict_fn(ex["messages"][:-1])
        try:
            predicted = json.loads(raw)["category"]
        except Exception:
            predicted, invalid = "INVALID", invalid + 1
        y_true.append(expected)
        y_pred.append(predicted)
    print(classification_report(y_true, y_pred, zero_division=0))
    print(f"Invalid JSON rate: {invalid / len(y_true):.2%}")
```

### Evaluation Checklist

- Compare against **baselines**: base model with best prompt, larger model with few-shot
- Measure **task metrics** (accuracy, F1, exact match, JSON validity)
- Check **general capability regression** on a small general benchmark
- Test **safety**: the fine-tune should not have lost refusal behavior (fine-tuning can erode safety alignment)
- Measure **latency and cost per request**
- Human review of a sample of outputs

---

## Part 11: Serving Fine-Tuned Models

| Option | Notes |
|---|---|
| **vLLM** | High-throughput serving, OpenAI-compatible API, multi-LoRA adapters |
| **Text Generation Inference (TGI)** | Hugging Face serving stack |
| **Ollama / llama.cpp** | Local/edge with GGUF quantized models |
| **Managed endpoints** | SageMaker, Vertex AI, Azure ML, Bedrock custom models |

```bash
# Serve base model with multiple LoRA adapters via vLLM (OpenAI-compatible)
vllm serve meta-llama/Llama-3.1-8B-Instruct \
  --enable-lora \
  --lora-modules ticket-classifier=./ticket-classifier-lora/final support-tone=./support-tone-dpo
# Clients select the adapter by using model="ticket-classifier" in the request
```

Version every fine-tuned artifact with its dataset version, base model, hyperparameters, and evaluation results (Module 14).

---

## Summary

| Concept | Key Takeaway |
|---|---|
| When to fine-tune | Behavior, format, style, cost reduction - not changing knowledge |
| Data | Quality, consistency, diversity, no leakage, no PII |
| SFT | Train on ideal input/output pairs |
| LoRA | Low-rank adapters: ~0.1-1% trainable params, swappable, mergeable |
| QLoRA | 4-bit base + LoRA = single-GPU fine-tuning |
| DPO | Preference tuning without reward models |
| Distillation | Small student learns from large teacher |
| Evaluation | Task metrics vs baselines, regression, safety, cost |

---

## Interview Questions

- **[L1]** What is fine-tuning, and how does it differ from prompt engineering and RAG?
- **[L1]** When should you fine-tune a model and when should you not?
- **[L2]** Explain how LoRA works, including the role of rank and alpha. Why is it so parameter-efficient?
- **[L2]** What is QLoRA and how does it make fine-tuning feasible on a single GPU?
- **[L2]** How do you prepare a high-quality fine-tuning dataset? What mistakes should you avoid?
- **[L3]** Compare SFT and DPO. When would you use preference tuning, and how would you collect preference data?
- **[L3]** Your company pays heavily for GPT-4-class calls on a high-volume classification task. Design a plan to replace it with a fine-tuned small model.
- **[L3]** After fine-tuning, your model scores well on the task but users report it became worse at general questions and less safe. What happened and how do you fix it?

## Interview Answers

1. Fine-tuning continues training a pre-trained model on task-specific examples so its weights change and the desired behavior becomes built in. Prompt engineering changes only the input instructions and examples at inference time without modifying the model. RAG supplies external knowledge in the prompt at query time. Fine-tuning is best for changing behavior (format, style, specialized task performance, efficiency), while RAG is best for providing up-to-date or private knowledge, and prompting is the cheapest first step for both.
2. Fine-tune when good prompts and few-shot examples cannot reliably achieve the required format, style, or task accuracy; when a high-volume narrow task could run on a cheaper, faster small model; when long prompts should be baked into weights; when domain-specific language or labels are needed; or when open models must run on-premises. Do not fine-tune to add frequently changing knowledge or documents (use RAG), when you lack quality data (fewer than ~100 good examples), when requirements are still changing, when citations are required, or before exhausting prompt engineering.
3. LoRA freezes the original weight matrix W and learns the update as a product of two small matrices, ΔW = B·A, where A is r x d and B is d x r with rank r much smaller than d. The layer output becomes W·x + (alpha/r)·B·A·x. Rank r controls the adapter's capacity (how expressive the update can be) and memory; alpha scales the update's magnitude relative to the base weights. Because only A and B are trained, a 4096 x 4096 layer with r=16 trains about 131K parameters instead of 16.8M, typically under 1% of total parameters. This drastically reduces gradient and optimizer memory, produces small swappable adapter files, and can be merged into the base weights for zero inference overhead.
4. QLoRA loads the frozen base model in 4-bit NormalFloat (NF4) quantization with double quantization of scaling constants, while training LoRA adapters in 16-bit precision and using paged optimizers to avoid memory spikes. Computation dequantizes weights on the fly to bf16. Since the frozen weights take roughly a quarter of the memory of 16-bit weights and only the small adapters need gradients and optimizer states, a 7-8B model can be fine-tuned on a single 24 GB GPU with quality close to 16-bit LoRA.
5. Define the exact input/output format used at inference, including the system prompt. Collect real examples from production data, expert-written examples, or reviewed synthetic data; ensure correctness through expert review, consistency of labeling guidelines, diversity across topics, lengths, and edge cases, and reasonable balance. Deduplicate, remove PII and secrets, validate format programmatically, and split into train, validation, and test sets without leakage. Mistakes to avoid: noisy or contradictory labels, training data that differs in format from inference, duplicates inflating metrics, test data leakage, synthetic data without review, and optimizing only for loss rather than task metrics.
6. SFT trains the model to reproduce ideal responses given inputs; it is best when a correct answer can be written for each example (formats, classification, extraction, specific tasks). DPO trains on pairs of chosen and rejected responses for the same prompt, increasing the likelihood of preferred outputs relative to a reference model; it is best for subjective qualities such as tone, helpfulness, conciseness, and safety where ranking is easier than writing perfect answers. Typically SFT is done first and DPO refines it. Preference data can be collected from human raters comparing two model outputs, product feedback such as thumbs up/down or choices between regenerated answers, or a strong LLM judge with rubric-based comparisons validated by human spot-checks.
7. Measure the current baseline: accuracy per class, latency, and monthly cost. Build a dataset by sampling real production inputs, labeling them with the large model, and having humans review a large portion, especially uncertain or disagreeing cases, aiming for several thousand examples covering all classes. Select a few small open models, fine-tune with LoRA/QLoRA, and evaluate on a held-out, human-verified test set against the large-model baseline, including per-class F1 and JSON validity. Serve the best model with vLLM on appropriately sized GPUs and compute cost per million requests. Roll out via shadow mode (run both, compare), then canary with a confidence-based fallback to the large model for low-confidence predictions. Monitor drift and periodically retrain with newly reviewed data.
8. This is catastrophic forgetting combined with safety alignment erosion: training on a narrow dataset shifted the weights away from general instruction-following and refusal behavior, especially with full fine-tuning, high learning rates, many epochs, or data lacking general and safety examples. Fixes: switch to or tune LoRA with lower rank or learning rate and fewer epochs; mix general instruction data and safety/refusal examples into the training set; start from the instruct/aligned model rather than a base model; apply a DPO safety pass; route only in-domain requests to the fine-tuned model and send general requests to a general model; and add general capability and safety benchmarks to the evaluation gate before deployment.
