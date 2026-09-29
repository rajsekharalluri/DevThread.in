---
id: genai-eng-multimodal
slug: multimodal
title: "Module 12: Multimodal GenAI"
category: genai-engineer
categoryTitle: GenAI Engineer
difficulty: advanced
estimatedMinutes: 100
version:
  minimum: "OpenAI SDK 1.x, sentence-transformers 3.x (CLIP), Python 3.11+"
prerequisites: [genai-eng-fine-tuning]
tags: [genai-engineer, multimodal, vision, audio, speech, image-generation, video, clip, document-ai]
relatedTopics: [genai-eng-ai-engineering]
order: 12
status: published
---
# Module 12: Multimodal GenAI

## Introduction

The real world is not only text. Customers send screenshots of errors, insurance claims include photos of damage, invoices arrive as scanned PDFs, meetings produce hours of audio, and factories stream video. **Multimodal GenAI** lets models understand and generate across **text, images, audio, and video**.

This module explains how multimodal models work, then covers practical engineering for each modality: vision-language understanding (image Q&A, document extraction, charts), multimodal embeddings and search (CLIP), speech-to-text and text-to-speech, real-time voice agents, image generation and editing, video understanding, multimodal RAG, and the cost, latency, and safety considerations specific to non-text data.

---

## Part 1: How Multimodal Models Work

### From Text Transformers to Multimodal Transformers

Transformers operate on sequences of vectors (tokens). Multimodal models convert other modalities into token-like vectors so the same transformer can process them.

| Modality | Encoder | Becomes |
|---|---|---|
| **Text** | Tokenizer + embedding table | Text tokens |
| **Image** | Vision Transformer (ViT): image split into patches (e.g. 14x14 or 16x16 pixels), each patch embedded | Image tokens (hundreds to thousands per image) |
| **Audio** | Spectrogram + audio encoder (e.g. Whisper-style) or audio tokenizer | Audio tokens |
| **Video** | Sampled frames through the image encoder (+ timing) | Sequences of image tokens |

### Architecture Patterns

| Pattern | Description | Examples |
|---|---|---|
| **Encoder + projector + LLM** | A pretrained vision encoder's outputs are mapped by a small projection layer into the LLM's embedding space | LLaVA, many open VLMs |
| **Natively multimodal** | One model trained from the start on interleaved text, image, and audio tokens | GPT-4o, Gemini |
| **Contrastive dual encoder** | Separate image and text encoders trained so matching pairs have similar embeddings | CLIP, SigLIP |
| **Diffusion models** | Generate images by iteratively denoising random noise, guided by a text embedding | Stable Diffusion, DALL-E 3, Imagen, FLUX |

### Why This Matters for Engineers

- **Images cost tokens.** A high-resolution image may consume ~1,000+ tokens; resolution directly drives cost and latency
- **Resolution vs detail trade-off.** Small text in screenshots needs high detail; a photo of a cat does not
- **Models reason over what they "see" imperfectly** - counting, precise spatial relations, and tiny text remain error-prone

---

## Part 2: Vision - Image Understanding

### Sending an Image to a Vision Model

```python
import base64
from pathlib import Path
from openai import OpenAI

client = OpenAI()

def image_to_data_url(path: str) -> str:
    mime = "image/png" if path.lower().endswith(".png") else "image/jpeg"
    b64 = base64.b64encode(Path(path).read_bytes()).decode()
    return f"data:{mime};base64,{b64}"

response = client.chat.completions.create(
    model="gpt-4o-mini",
    messages=[{
        "role": "user",
        "content": [
            {"type": "text", "text": "This is a screenshot a customer sent. What error are they seeing and what should they do?"},
            {"type": "image_url", "image_url": {"url": image_to_data_url("error_screenshot.png"), "detail": "high"}},
        ],
    }],
    max_tokens=300,
)
print(response.choices[0].message.content)
```

**`detail` parameter:** `"low"` uses a fixed small token budget (fast, cheap, good for general scenes); `"high"` tiles the image for fine detail (needed for text, UI, documents). Use `"auto"` to let the API decide.

### Anthropic Claude Vision

```python
import anthropic

claude = anthropic.Anthropic()
message = claude.messages.create(
    model="claude-3-5-sonnet-latest",
    max_tokens=300,
    messages=[{
        "role": "user",
        "content": [
            {"type": "image", "source": {"type": "base64", "media_type": "image/png",
                                         "data": base64.b64encode(Path("chart.png").read_bytes()).decode()}},
            {"type": "text", "text": "Summarize the trend in this chart in 3 bullets."},
        ],
    }],
)
print(message.content[0].text)
```

### Common Vision Use Cases

| Use Case | Example |
|---|---|
| **Screenshot support** | Diagnose UI errors from customer screenshots |
| **Document understanding** | Extract fields from invoices, receipts, IDs, forms |
| **Chart and diagram Q&A** | Interpret dashboards, architecture diagrams |
| **Visual inspection** | Detect damage in insurance claims, defects in manufacturing |
| **Accessibility** | Generate alt text for images |
| **Content moderation** | Flag unsafe images |
| **Retail** | Product recognition, catalog tagging from photos |

### Structured Extraction from Documents (Invoices)

Combine vision with structured outputs (Module 3) for reliable document AI.

```python
from pydantic import BaseModel, Field
from typing import Optional

class InvoiceLine(BaseModel):
    description: str
    quantity: float
    unit_price: float
    amount: float

class InvoiceData(BaseModel):
    vendor_name: str
    invoice_number: str
    invoice_date: str = Field(description="ISO format YYYY-MM-DD")
    currency: str
    subtotal: float
    tax: Optional[float] = None
    total: float
    lines: list[InvoiceLine]

def extract_invoice(image_path: str) -> InvoiceData:
    completion = client.beta.chat.completions.parse(
        model="gpt-4o",
        temperature=0,
        messages=[
            {"role": "system", "content": "Extract invoice data exactly as printed. Use null for missing fields. Do not guess."},
            {"role": "user", "content": [
                {"type": "text", "text": "Extract all invoice fields."},
                {"type": "image_url", "image_url": {"url": image_to_data_url(image_path), "detail": "high"}},
            ]},
        ],
        response_format=InvoiceData,
    )
    return completion.choices[0].message.parsed

def validate_invoice(inv: InvoiceData) -> list[str]:
    issues = []
    line_sum = round(sum(l.amount for l in inv.lines), 2)
    if abs(line_sum - inv.subtotal) > 0.01:
        issues.append(f"Line items sum {line_sum} != subtotal {inv.subtotal}")
    if abs(inv.subtotal + (inv.tax or 0) - inv.total) > 0.01:
        issues.append("subtotal + tax != total")
    return issues

invoice = extract_invoice("invoice_scan.jpg")
problems = validate_invoice(invoice)
print(invoice.model_dump_json(indent=2))
print("Needs human review:" if problems else "Auto-approved", problems)
```

**Production pattern:** vision extraction, then **deterministic business validation**, then human review queue for anything that fails. Never auto-post financial data solely on model output.

### Vision-Language Model vs Traditional OCR

| | Traditional OCR / Document AI (Tesseract, Textract, Azure Document Intelligence) | Vision-Language Model |
|---|---|---|
| Output | Text + bounding boxes, key-value pairs | Understanding, reasoning, structured JSON |
| Accuracy on text | Very high, character-level | Good, but may misread or "correct" characters |
| Layout reasoning | Rules/templates or prebuilt models | Flexible, handles unseen layouts |
| Hallucination | Rare | Possible (invents plausible values) |
| Cost at scale | Lower | Higher |
| Best practice | Use OCR for exact text, VLM for interpretation - or combine both (OCR text + image to VLM) |

---

## Part 3: Multimodal Embeddings and Image Search

### CLIP: Text and Images in One Vector Space

CLIP was trained on hundreds of millions of (image, caption) pairs with contrastive learning, so an image and its description land near each other. This enables:
- **Text-to-image search** ("red running shoes on a white background")
- **Image-to-image search** (find visually similar products)
- **Zero-shot image classification** (compare image to text labels)

```python
# pip install sentence-transformers pillow
from sentence_transformers import SentenceTransformer
from PIL import Image
import numpy as np

clip = SentenceTransformer("clip-ViT-B-32")

image_paths = ["shoe_red.jpg", "shoe_blue.jpg", "laptop.jpg", "coffee_mug.jpg"]
image_vecs = clip.encode([Image.open(p) for p in image_paths], normalize_embeddings=True)

def search_images(query: str, k: int = 2) -> list[tuple[str, float]]:
    q = clip.encode(query, normalize_embeddings=True)
    scores = image_vecs @ q
    top = np.argsort(-scores)[:k]
    return [(image_paths[i], float(scores[i])) for i in top]

print(search_images("a red sneaker"))

# Zero-shot classification
labels = ["a photo of footwear", "a photo of electronics", "a photo of kitchenware"]
label_vecs = clip.encode(labels, normalize_embeddings=True)
for path, vec in zip(image_paths, image_vecs):
    print(path, "->", labels[int(np.argmax(label_vecs @ vec))])
```

Store CLIP vectors in any vector database (Module 5). Newer multimodal embedding models (SigLIP, Cohere/Voyage/Vertex multimodal embeddings, ColPali for document pages) improve quality further.

---

## Part 4: Audio - Speech-to-Text

### Transcription with Whisper-Class Models

```python
from openai import OpenAI

client = OpenAI()

with open("support_call.mp3", "rb") as audio:
    transcript = client.audio.transcriptions.create(
        model="whisper-1",
        file=audio,
        response_format="verbose_json",   # includes segments with timestamps
        language="en",                    # optional hint improves accuracy and speed
    )

print(transcript.text[:500])
for seg in transcript.segments[:3]:
    print(f"[{seg.start:6.1f}s - {seg.end:6.1f}s] {seg.text}")
```

### Local Transcription

```python
# pip install faster-whisper
from faster_whisper import WhisperModel

model = WhisperModel("small", device="cpu", compute_type="int8")
segments, info = model.transcribe("meeting.wav", vad_filter=True)
print("Detected language:", info.language)
for s in segments:
    print(f"[{s.start:.1f}-{s.end:.1f}] {s.text}")
```

### From Transcript to Insight: Call Analytics Pipeline

```python
from pydantic import BaseModel
from typing import Literal

class CallAnalysis(BaseModel):
    summary: str
    customer_sentiment: Literal["positive", "neutral", "negative"]
    issue_category: str
    resolved: bool
    action_items: list[str]
    compliance_flags: list[str]

def analyze_call(audio_path: str) -> CallAnalysis:
    with open(audio_path, "rb") as f:
        text = client.audio.transcriptions.create(model="whisper-1", file=f).text
    completion = client.beta.chat.completions.parse(
        model="gpt-4o-mini",
        temperature=0,
        messages=[
            {"role": "system", "content": "Analyze this customer support call transcript. Flag if the agent failed to read the required disclosure."},
            {"role": "user", "content": text},
        ],
        response_format=CallAnalysis,
    )
    return completion.choices[0].message.parsed
```

### Speech-to-Text Engineering Concerns

| Concern | Practice |
|---|---|
| Long audio | Chunk files (API size limits), use VAD to split on silence |
| Speaker identification | Use diarization (pyannote, provider diarization features) |
| Domain terms | Provide a prompt/vocabulary hint with product names and acronyms |
| Accuracy measurement | Word Error Rate (WER) on a labeled sample |
| PII | Redact card numbers, addresses in transcripts before storage/LLM calls |
| Consent | Record and process calls only with legal consent |

---

## Part 5: Audio - Text-to-Speech and Voice Agents

### Text-to-Speech

```python
speech = client.audio.speech.create(
    model="tts-1",
    voice="alloy",
    input="Your order has shipped and will arrive on Thursday.",
)
speech.write_to_file("order_update.mp3")
```

### Voice Agent Architectures

| Architecture | Flow | Pros | Cons |
|---|---|---|---|
| **Cascaded (pipeline)** | Speech-to-text, then LLM, then text-to-speech | Modular, any LLM, easy to log text, mature | Higher latency, loses tone/emotion |
| **Speech-to-speech (native audio)** | Realtime model consumes and produces audio directly | Low latency, natural turn-taking, preserves prosody | Fewer model choices, harder to inspect, pricier |

### Latency Budget for Natural Conversation

Humans expect a response within about 500-800 ms after they stop speaking.

```text
Voice activity detection (end of speech):   ~200 ms
Speech-to-text (streaming):                 ~150-300 ms
LLM time to first token:                    ~200-400 ms
Text-to-speech time to first audio:         ~100-200 ms
Network overhead:                           ~50-150 ms
```

Techniques: stream everything (STT partials, LLM tokens, TTS audio chunks), start TTS on the first sentence, support **barge-in** (user interrupts the agent), keep prompts short, and use WebRTC/WebSockets for transport. Realtime APIs from major providers and frameworks such as LiveKit Agents and Pipecat handle much of this plumbing.

---

## Part 6: Image Generation and Editing

### Diffusion Models in One Paragraph

Diffusion models learn to reverse a noising process. During training, images are progressively corrupted with noise and the model learns to predict and remove that noise. During generation, the model starts from pure noise and denoises step by step, guided by a text embedding of the prompt, until a coherent image emerges. **Latent diffusion** performs this in a compressed latent space for efficiency.

### Generating Images via API

```python
result = client.images.generate(
    model="dall-e-3",
    prompt="Isometric illustration of a cloud data center with glowing server racks, clean flat style, blue and white palette",
    size="1024x1024",
    quality="standard",
    n=1,
)
print(result.data[0].url)
```

### Prompting Image Models

Include: **subject, style, composition, lighting, color palette, medium, aspect ratio**.

```text
WEAK:   A dashboard
STRONG: A modern analytics dashboard UI on a laptop screen, dark theme, line charts and KPI cards,
        soft studio lighting, shallow depth of field, product photography style, 16:9
```

### Open-Source Image Generation

```python
# pip install diffusers transformers accelerate torch
import torch
from diffusers import AutoPipelineForText2Image

pipe = AutoPipelineForText2Image.from_pretrained(
    "stabilityai/sdxl-turbo", torch_dtype=torch.float16, variant="fp16"
).to("cuda")

image = pipe(prompt="A watercolor painting of a lighthouse at dawn", num_inference_steps=2, guidance_scale=0.0).images[0]
image.save("lighthouse.png")
```

### Business Use Cases and Cautions

| Use | Caution |
|---|---|
| Marketing creatives, product mockups | Brand consistency; human approval |
| Personalized visuals at scale | Cost control; content moderation |
| Synthetic training data for vision models | Distribution mismatch with real images |
| Image editing / inpainting / background removal | Disclose edits where required |

Legal and ethical considerations: copyright and licensing of generated content, likeness of real people, deepfake risks, provenance labeling (C2PA content credentials), and platform policies.

---

## Part 7: Video Understanding

Most video understanding today works by **sampling frames** (plus transcribing the audio track) and sending them to a vision-language model; some models (e.g. Gemini) accept video natively.

```python
# pip install opencv-python
import cv2
import base64

def sample_frames(video_path: str, every_n_seconds: float = 5.0, max_frames: int = 20) -> list[str]:
    cap = cv2.VideoCapture(video_path)
    fps = cap.get(cv2.CAP_PROP_FPS) or 25
    step = int(fps * every_n_seconds)
    frames, idx = [], 0
    while len(frames) < max_frames:
        cap.set(cv2.CAP_PROP_POS_FRAMES, idx)
        ok, frame = cap.read()
        if not ok:
            break
        frame = cv2.resize(frame, (768, int(768 * frame.shape[0] / frame.shape[1])))
        _, buf = cv2.imencode(".jpg", frame, [cv2.IMWRITE_JPEG_QUALITY, 80])
        frames.append("data:image/jpeg;base64," + base64.b64encode(buf).decode())
        idx += step
    cap.release()
    return frames

frames = sample_frames("warehouse_cam.mp4", every_n_seconds=10)
content = [{"type": "text", "text": "These frames are 10 seconds apart from a warehouse camera. Describe any safety violations with approximate timestamps."}]
content += [{"type": "image_url", "image_url": {"url": f, "detail": "low"}} for f in frames]
r = client.chat.completions.create(model="gpt-4o-mini", messages=[{"role": "user", "content": content}])
print(r.choices[0].message.content)
```

**Trade-offs:** sampling rate vs cost (each frame costs tokens), missing short events between samples, and privacy of people in footage. For long videos, index segments (transcript + frame captions + embeddings) and retrieve relevant segments instead of sending everything.

---

## Part 8: Multimodal RAG

Enterprise documents contain tables, charts, diagrams, and scanned pages that text-only RAG loses.

### Strategies

| Strategy | How | Trade-off |
|---|---|---|
| **Convert everything to text** | OCR + generate captions/descriptions for images and charts with a VLM, then index text | Simple, reuses text RAG; descriptions may lose detail |
| **Multimodal embeddings** | Embed images with CLIP-like models alongside text | Direct image retrieval; weaker for dense document pages |
| **Page-image retrieval** | Embed whole page images (e.g. ColPali-style late interaction), retrieve pages, send page images to a VLM | Preserves layout, tables, charts; larger storage and VLM cost |
| **Hybrid** | Text chunks + image captions, with links to the original images passed to the VLM at answer time | Balanced; most common in production |

### Caption-and-Index Pipeline

```python
def describe_figure(image_path: str, page_context: str) -> str:
    r = client.chat.completions.create(
        model="gpt-4o-mini",
        messages=[{"role": "user", "content": [
            {"type": "text", "text": f"Describe this figure for search indexing. Include all numbers, labels, axes, and the key insight. Page context: {page_context[:500]}"},
            {"type": "image_url", "image_url": {"url": image_to_data_url(image_path), "detail": "high"}},
        ]}],
    )
    return r.choices[0].message.content

# Index the description as a chunk with metadata {"type": "figure", "image_path": ..., "page": ...}
# At answer time, if a figure chunk is retrieved, send the ORIGINAL image to the VLM along with the text context.
```

---

## Part 9: Production Considerations for Multimodal Systems

| Area | Practice |
|---|---|
| **Cost** | Downscale images, use `detail: low` when possible, crop to regions of interest, sample fewer frames |
| **Latency** | Pre-process asynchronously (queue OCR/transcription), stream outputs, cache results by file hash |
| **Accuracy** | Validate extracted values with business rules; human review for low confidence |
| **Storage** | Store originals in object storage (S3/Blob) with lifecycle policies; keep derived text/embeddings in DBs |
| **Privacy** | Faces, license plates, IDs, voices are sensitive; blur/redact; consent; data residency |
| **Security** | Images can contain **visual prompt injection** (hidden text instructing the model); treat image-derived text as untrusted |
| **Evaluation** | Field-level accuracy for extraction, WER for speech, human ratings for generation, retrieval recall for multimodal search |
| **File handling** | Validate MIME types and sizes, scan for malware, handle HEIC/TIFF/multi-page PDFs |

---

## Summary

| Modality | Key Techniques |
|---|---|
| Vision understanding | VLMs with image inputs, detail levels, structured extraction + validation |
| Multimodal embeddings | CLIP/SigLIP for text-image search and zero-shot classification |
| Speech-to-text | Whisper-class models, diarization, WER, PII redaction |
| Voice agents | Cascaded vs speech-to-speech, streaming, sub-second latency budgets |
| Image generation | Diffusion models, prompt structure, legal/provenance cautions |
| Video | Frame sampling + transcripts, segment indexing |
| Multimodal RAG | Captions, multimodal embeddings, page-image retrieval |

---

## Interview Questions

- **[L1]** What is a multimodal model, and how do vision-language models process images?
- **[L1]** What is CLIP and what kinds of applications does it enable?
- **[L2]** How would you build a reliable invoice extraction system using a vision-language model?
- **[L2]** Compare cascaded voice agents (STT, LLM, TTS) with native speech-to-speech models.
- **[L2]** How do image resolution and detail settings affect cost, latency, and accuracy?
- **[L3]** Design a multimodal RAG system for technical manuals full of diagrams and tables.
- **[L3]** What security and privacy risks are specific to multimodal applications and how do you mitigate them?
- **[L3]** Design a call-center analytics platform that processes 100,000 recorded calls per day.

## Interview Answers

1. A multimodal model can take inputs and/or produce outputs in more than one modality, such as text, images, audio, or video. Vision-language models typically split an image into patches, encode them with a vision transformer into embeddings, and project those embeddings into the language model's token embedding space (or train natively on interleaved tokens), so the transformer attends over image tokens and text tokens together and generates text conditioned on both.
2. CLIP is a contrastive dual-encoder model with an image encoder and a text encoder trained on hundreds of millions of image-caption pairs so matching images and texts have similar embeddings in a shared vector space. It enables text-to-image search, image-to-image similarity search, zero-shot image classification by comparing an image to text label embeddings, content moderation, deduplication, and multimodal retrieval for RAG.
3. Ingest documents by validating file types and converting PDFs to page images; optionally run OCR to obtain exact text. Send the page image (and OCR text) to a VLM with a strict Pydantic schema via structured outputs and instructions to use null rather than guessing. Apply deterministic validation: line items sum to subtotal, subtotal plus tax equals total, dates are valid, the vendor exists in the master data, and there are no duplicate invoice numbers. Auto-approve only when all checks pass, and route failures or low-confidence extractions to a human review queue whose corrections feed an evaluation set and future fine-tuning. Measure field-level accuracy, keep an audit trail with the original image, and redact sensitive data.
4. Cascaded agents chain streaming speech-to-text, a text LLM, and text-to-speech. They are modular (any LLM, tools, and RAG), easy to log and moderate as text, and cheaper to debug, but they add latency at each stage and lose paralinguistic cues such as tone, emotion, and hesitation. Native speech-to-speech models process and generate audio directly, giving lower latency, natural turn-taking, interruption handling, and expressive prosody, but offer fewer model choices, higher cost, and harder inspection and control. Choose cascaded for complex tool-heavy workflows and compliance logging, and speech-to-speech for highly natural low-latency conversations.
5. Higher resolution produces more image tokens, which increases cost and latency roughly proportionally. Low detail modes use a small fixed token budget and are adequate for general scenes and classification, while high detail tiles the image and is necessary for small text, UI screenshots, tables, and documents. Engineers optimize by downscaling to the minimum resolution that preserves needed detail, cropping regions of interest, choosing detail per use case, and evaluating accuracy versus cost on a sample.
6. During ingestion, parse manuals with a layout-aware parser to extract text sections, tables (converted to Markdown/HTML with headers preserved), and figures with their captions and page numbers. Generate detailed VLM descriptions for each diagram including labels and part numbers, and index text chunks, table chunks, and figure descriptions with metadata linking back to the page image and section path. Optionally also index page images with a page-level multimodal retriever for layout-heavy pages. At query time use hybrid search plus reranking; when figure or table chunks are retrieved, pass the original images and tables together with text context to a VLM so it can reason over the visual itself, and cite page numbers. Evaluate with questions that require diagrams and tables specifically.
7. Risks include visual prompt injection (text hidden in images instructing the model), sensitive personal data in images, audio, and video (faces, voices, IDs, license plates, medical images), biometric data regulations, deepfakes and misuse of generated media, copyright of generated content, malicious files, and data residency. Mitigations: treat all image- and audio-derived text as untrusted with strict tool permissions, validate and scan uploads, redact or blur PII before storage or model calls, obtain consent, apply retention limits and encryption, use providers with appropriate data processing agreements, moderate generated content, add provenance metadata (C2PA) to generated media, and log access for audits.
8. Recordings land in object storage and trigger events on a queue. Autoscaled GPU workers (self-hosted faster-whisper or a batch transcription API) transcribe with diarization and domain vocabulary hints; transcripts then pass through PII redaction. A second stage uses a cost-efficient LLM with structured outputs to extract summary, sentiment, category, resolution, compliance flags, and action items, with a larger model for flagged or complex calls. Results are stored in a warehouse for dashboards and in a vector index for semantic search over calls. The pipeline is idempotent and retryable with dead-letter queues, batch APIs are used where latency allows, and throughput is sized for peak hours. Quality is monitored via WER sampling, human QA of extracted labels, and drift detection, and access control, retention policies, and call-recording consent compliance are enforced.
