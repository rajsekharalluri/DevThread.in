---
id: leadership-code-reviews
slug: code-reviews
title: Effective Code Reviews and Technical Feedback
category: leadership
categoryTitle: Leadership & Soft Skills
difficulty: senior
estimatedMinutes: 35
version:
  minimum: "Role-agnostic"
prerequisites: [leadership-mentoring]
tags: [leadership, code-review, feedback, quality]
relatedTopics: [leadership-conflict-resolution, leadership-technical-decisions]
order: 30
status: published
---
# Effective Code Reviews and Technical Feedback

## Introduction
A code review is both a quality activity and a human communication interaction. It checks correctness, security, maintainability, and team conventions while also teaching and building trust.

```text
Pull request
   ├── correctness/security risk
   ├── maintainability/design
   ├── tests/operability
   └── feedback conversation
```

## Purpose
Good reviews catch important problems early without turning every reviewer preference into a blocking requirement. They improve code and the author's future judgment.

## Real-world simple example
Weak feedback:

```text
“This is wrong. Fix it.”
```

Useful feedback:

```text
“If Customer can be null here, this dereference can fail in production.
Is non-null guaranteed by the caller, or should we validate/map it earlier?”
```

The second comment identifies risk, explains why, and invites a technical answer instead of judging the author.

## Professional company-level approach
Classify comments:

```text
Blocking → correctness/security/data loss/explicit standard
Suggestion → maintainability or alternative worth considering
Nit       → optional style/readability preference
Question  → uncertainty requiring clarification
```

Review in the right order: contract/security/correctness first, design/maintainability next, style last. Keep pull requests small enough to review meaningfully and respond quickly enough not to block delivery.

## Common failure scenarios
- Blocking on personal style preferences.
- Vague or personal comments.
- Inconsistent standards across people.
- Reviews waiting for days.
- No tests for behavior being changed.
- Reviewers redesigning code instead of addressing the actual risk.

## Comparison
| Review mode | Useful when |
|---|---|
| Async PR review | Normal change, clear context |
| Pairing | Complex/unclear design or learning |
| Design review | High-blast-radius decision before code |
| Automated checks | Formatting, static analysis, repeatable rules |
| Post-incident review | System/process improvement without blame |

## Interview Questions
- **[L1]** What makes a code-review comment useful?
- **[L1]** What is the difference between a blocking issue and a preference?
- **[L2]** How do you give critical feedback without discouraging the author?
- **[L2]** How should a team handle inconsistent review standards?
- **[L3]** How would you improve a team where code reviews are slow and overly adversarial?
- **[L3]** When should a design be discussed before implementation rather than in a PR?

## Interview Answers
1. It identifies a concrete behavior/risk, explains why it matters, and suggests or asks for a clear next step.
2. A blocking issue must change for correctness/security/explicit standards; a preference is optional and should be labeled as such.
3. Focus on code and impact, not character; explain reasoning, recognize good work, and ask questions where assumptions are unclear.
4. Agree on documented standards, automate objective checks, calibrate examples together, and review whether comments are consistent across authors.
5. Define review SLAs, keep PRs small, automate style checks, distinguish blockers from suggestions, coach harsh reviewers privately, and introduce design discussions for repeated architectural debates.
6. Discuss it before implementation when the decision is costly to reverse, affects many teams/services, or has significant data/security/operational consequences.

## Expert perspective
A review is a repeated culture-setting mechanism. Senior engineers protect the signal by blocking only real risks, explain reasoning, and make the process safe enough that people submit small work early instead of hiding large changes until the last moment.
