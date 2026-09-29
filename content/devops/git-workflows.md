---
id: devops-git-workflows
slug: git-workflows
title: "Git Workflows: Branching, Merge vs Rebase, Pull Requests, and Conflicts"
category: devops
categoryTitle: DevOps & Cloud
difficulty: beginner
estimatedMinutes: 45
version:
  minimum: "Git 2.40+"
prerequisites: []
tags: [git, branching, trunk-based-development, rebase, merge, pull-requests, code-review]
relatedTopics: [devops-cicd-fundamentals, devops-source-control-migrations]
order: 5
status: published
---
# Git Workflows: Branching, Merge vs Rebase, Pull Requests, and Conflicts

## Introduction

Git is used by almost every software team, but many engineers only know `add`, `commit`, `push`, and `pull`. Understanding how Git actually stores history, how to choose a branching strategy, when to merge versus rebase, how to run effective pull requests, and how to resolve conflicts calmly makes a big difference to team speed and release safety.

## Part 1: How Git Stores History

| Concept | What It Is |
|---|---|
| **Commit** | A snapshot of the whole project plus metadata (author, message) and a pointer to its parent commit(s). Identified by a SHA hash |
| **Branch** | A movable pointer (label) to a commit. Creating a branch is instant and cheap |
| **HEAD** | Pointer to the branch or commit you currently have checked out |
| **Remote** | A named copy of the repository elsewhere (`origin`) |
| **Remote-tracking branch** | Your local record of where a remote branch was at last fetch (`origin/main`) |
| **Working tree / index (staging area)** | Your files on disk / the set of changes selected for the next commit |

```bash
git log --oneline --graph --decorate --all   # visualize branches and history
git status                                    # what is staged, modified, untracked
git diff                                      # unstaged changes
git diff --staged                             # staged changes (what will be committed)
```

Because branches are just pointers, most Git operations (branching, merging, rebasing) are about **moving pointers and creating new commits**, not copying files.

## Part 2: Everyday Workflow

```bash
git switch main
git pull --ff-only                         # update main without creating merge commits
git switch -c feature/order-notes          # create a short-lived feature branch

# ... edit files ...
git add -p                                 # stage hunks interactively; review what you commit
git commit -m "Add notes field to orders"
git push -u origin feature/order-notes     # publish and set upstream

# Keep up to date with main while working
git fetch origin
git rebase origin/main                     # replay your commits on top of the latest main
git push --force-with-lease                # safe force push for YOUR branch only
```

### Writing Good Commits

- **Small and focused:** one logical change per commit
- **Message explains why:** subject line in imperative mood under ~72 characters, then a body explaining the reason
- **Never commit secrets**; use `.gitignore` and secret scanning (pre-commit hooks, GitHub push protection)

```text
Add optimistic concurrency to order updates

Two support agents editing the same order could silently overwrite each
other's changes. Adds a row version token and returns 409 on conflict.
```

## Part 3: Branching Strategies

| Strategy | How It Works | Best For | Watch Out For |
|---|---|---|---|
| **Trunk-based development** | Everyone integrates into `main` at least daily via short-lived branches (hours to a couple of days). Incomplete features are hidden behind feature flags | Teams with good CI and continuous delivery | Requires strong automated tests and feature flags |
| **GitHub Flow** | Branch from `main`, open PR, review, merge, deploy from `main` | Most web services and SaaS | Similar to trunk-based with slightly longer branches |
| **GitFlow** | Long-lived `develop` and `main`, plus feature, release, and hotfix branches | Versioned products with scheduled releases (installed software, mobile apps with store review) | Long-lived branches cause painful merges; slows continuous delivery |
| **Release branches** | Cut `release/2.4` from `main` for stabilization; fixes cherry-picked | Products supporting multiple versions | Cherry-pick discipline; fixes must also land on `main` |

### Why Trunk-Based Development Is the Default for Services

Research on software delivery performance (the DORA reports) consistently associates short-lived branches and frequent integration with better delivery outcomes. Long-lived branches drift from `main`, making merges large, risky, and slow, and delaying feedback from CI.

```text
Trunk-based day:
09:00  branch feature/discount-rules from main
11:30  PR opened (120 lines, tests included), CI green
12:15  review approved, squash-merged to main, deployed behind flag "discount-rules"
15:00  second small PR extends the feature, flag still off
Next week: flag enabled for 5% of users, then 100%, then flag code removed
```

## Part 4: Merge vs Rebase vs Squash

All three combine work from a branch into another, but they shape history differently.

| Method | What Happens | History Result | Use When |
|---|---|---|---|
| **Merge commit** (`git merge --no-ff`) | Creates a new commit with two parents | Preserves full branch history, non-linear graph | You want to preserve the exact branch structure |
| **Fast-forward** | Moves the pointer forward when no divergence | Linear, no extra commit | Branch is strictly ahead of target |
| **Rebase** (`git rebase main`) | Rewrites your commits as new commits on top of the target | Linear, commits get new hashes | Updating **your own** feature branch before merging |
| **Squash merge** | Combines all branch commits into one commit on target | One commit per PR on `main` | Keeping `main` history clean, one change per PR |

### The Golden Rule of Rebasing

**Never rebase commits that other people have based work on** (shared branches like `main` or a branch teammates also push to). Rebasing rewrites commit hashes; others' history then diverges and they must untangle it. Rebasing your own unshared feature branch is safe.

### Force Push Safely

```bash
git push --force-with-lease   # refuses to overwrite if the remote has commits you have not seen
# never: git push --force on main or shared branches
```

Protect `main` with branch protection rules so force pushes and direct pushes are blocked.

### Interactive Cleanup Before Review

```bash
git rebase -i origin/main     # reorder, squash "fix typo" commits, edit messages (editor-based)
git commit --fixup <sha>      # mark a commit as a fix for an earlier one
git rebase -i --autosquash origin/main
```

## Part 5: Pull Requests and Code Review

### What a Good PR Looks Like

| Practice | Why |
|---|---|
| **Small** (ideally under ~400 changed lines) | Reviewers find more issues in small PRs, reviews are faster |
| **One purpose** | Refactors separate from behavior changes |
| **Clear description** | What, why, how tested, risks, screenshots for UI |
| **Tests included** | Proves behavior, prevents regressions |
| **CI green before review** | Do not waste reviewer time on broken builds |
| **Linked issue/ticket** | Traceability |

```markdown
## What
Adds optimistic concurrency to order updates.

## Why
Agents could overwrite each other's edits (INC-231).

## How
- Adds `RowVersion` concurrency token to `Order`
- PUT /orders/{id} returns 409 Conflict with current state on mismatch

## Testing
- Integration test: two concurrent updates, second returns 409
- Manually verified in staging

## Risks
Migration adds a column to Orders (nullable, safe for rolling deploy).
```

### Branch Protection and Automation

- Require PR reviews (1-2 approvers; CODEOWNERS for critical areas)
- Require status checks: build, tests, lint, security scans
- Require branches to be up to date before merging (or use a merge queue)
- Block force pushes and deletion of `main`
- Require signed commits where policy demands

### Reviewing Well

- Review for **correctness, design, tests, security, and readability**, not personal style (let formatters and linters handle style)
- Ask questions rather than issue commands ("What happens if the list is empty?")
- Distinguish blocking issues from suggestions ("nit:")
- Respond within a working day; slow reviews push teams toward bigger PRs

## Part 6: Resolving Merge Conflicts

A conflict happens when two branches change the same lines (or one deletes a file the other edits) and Git cannot decide automatically.

```bash
git fetch origin
git rebase origin/main
# CONFLICT (content): Merge conflict in src/Orders/OrderService.cs
```

```text
<<<<<<< HEAD (upstream main, when rebasing)
    var total = lines.Sum(l => l.Quantity * l.UnitPrice) - discount;
=======
    var total = lines.Sum(l => l.Quantity * l.UnitPrice) + shippingFee;
>>>>>>> Add shipping fee (your commit)
```

### Resolution Steps

```bash
# 1. Understand both changes - read the commits that introduced them
git log --oneline -3 origin/main -- src/Orders/OrderService.cs

# 2. Edit the file to the correct combined result, e.g.:
#    var total = lines.Sum(l => l.Quantity * l.UnitPrice) - discount + shippingFee;

# 3. Build and run tests - a conflict-free merge can still be semantically wrong
dotnet test

# 4. Mark resolved and continue
git add src/Orders/OrderService.cs
git rebase --continue          # or: git merge --continue

# If things go wrong
git rebase --abort             # return to the state before the rebase
```

### Reducing Conflicts

- Integrate frequently (small, short-lived branches)
- Avoid mass reformatting mixed with feature changes
- Split large files with many owners into cohesive smaller ones
- Enable `git rerere` to reuse recorded resolutions for repeated conflicts: `git config --global rerere.enabled true`

## Part 7: Recovering from Mistakes

| Situation | Command |
|---|---|
| Undo unstaged changes to a file | `git restore path/to/file` |
| Unstage a file | `git restore --staged path/to/file` |
| Fix the last commit message or add a forgotten file (not yet pushed) | `git commit --amend` |
| Undo a pushed commit on a shared branch | `git revert <sha>` (creates a new inverse commit) |
| Move branch back locally (unpushed work) | `git reset --soft HEAD~1` (keep changes staged) |
| Find "lost" commits after a bad reset or rebase | `git reflog`, then `git switch -c rescue <sha>` |
| Temporarily set aside work | `git stash push -m "wip"` / `git stash pop` |
| Find which commit introduced a bug | `git bisect start`, `git bisect bad`, `git bisect good <sha>` |
| Apply one commit from another branch | `git cherry-pick <sha>` |

**Revert on shared branches, reset only on private ones.** `git reset --hard` discards uncommitted work permanently, so check `git status` first.

## Part 8: Monorepos, Tags, and Releases

- **Tags** mark release points: `git tag -a v2.4.0 -m "Release 2.4.0"` then `git push origin v2.4.0`. CI can build release artifacts from tags
- **Semantic versioning** (MAJOR.MINOR.PATCH) communicates compatibility
- **Conventional Commits** (`feat:`, `fix:`, `chore:`) enable automated changelogs and version bumps
- **Monorepos** keep many projects in one repository; use path-based CI triggers, CODEOWNERS per folder, and sparse checkout for large repos

## Common Mistakes

| Mistake | Better Approach |
|---|---|
| Week-long feature branches | Small PRs merged daily, feature flags for incomplete work |
| `git push --force` on shared branches | `--force-with-lease` on your own branch only; protect `main` |
| Rebasing a branch others use | Merge instead, or coordinate |
| Giant PRs mixing refactors and features | Separate, small, focused PRs |
| Resolving conflicts without running tests | Build and test after every resolution |
| Committing secrets and "deleting" them in the next commit | Rotate the secret immediately; history still contains it |
| `git reset --hard` on a shared branch | `git revert` |

## Interview Questions
- **[L1]** What is the difference between a commit and a branch in Git?
- **[L1]** What is the difference between git fetch and git pull?
- **[L2]** Compare merge, rebase, and squash merge. When would you use each?
- **[L2]** Why is it dangerous to rebase a shared branch, and what does --force-with-lease protect against?
- **[L2]** Compare trunk-based development and GitFlow.
- **[L3]** How would you set up a branching and PR process for a team that deploys a service to production several times a day?
- **[L3]** A secret was committed and pushed to a shared repository. What do you do?
- **[L3]** A bug appeared somewhere in the last 200 commits. How do you find the cause efficiently?

## Interview Answers
1. A commit is an immutable snapshot of the entire project at a point in time, with metadata and pointers to its parent commits, identified by a hash. A branch is just a lightweight, movable pointer to a commit; when you commit on a branch, the pointer moves to the new commit. Creating or deleting a branch does not copy or delete files, which is why branching in Git is cheap.
2. `git fetch` downloads new commits and updates remote-tracking branches such as `origin/main` without changing your local branches or working tree, so you can inspect incoming changes safely. `git pull` is a fetch followed by integrating the remote branch into your current branch, by merging by default or rebasing if configured. Using `git pull --ff-only` or `--rebase` avoids unintended merge commits.
3. A merge commit joins two histories with a commit that has two parents, preserving the exact branch structure; use it when branch history matters, for example integrating long-lived release branches. Rebase replays your commits on top of another branch, creating new commits and a linear history; use it to update your own feature branch with the latest `main` before merging. Squash merge combines all of a PR's commits into one commit on the target branch; use it to keep `main` history clean with one commit per change, at the cost of losing intermediate commits.
4. Rebasing rewrites commits into new ones with new hashes. If others have already based work on the original commits, their history now diverges from the rewritten branch, leading to duplicated commits, confusing conflicts, and possibly lost work when someone force pushes. `--force-with-lease` only overwrites the remote branch if it still points where your local repository last saw it, so it refuses to clobber commits someone else pushed in the meantime, unlike a plain `--force`.
5. Trunk-based development has everyone integrate small changes into `main` at least daily using short-lived branches, with feature flags hiding incomplete work and continuous integration validating every change; it minimizes merge pain and supports continuous delivery. GitFlow uses long-lived `develop` and `main` branches plus feature, release, and hotfix branches; it suits versioned products with scheduled releases and multiple supported versions, but long-lived branches cause large, risky merges and slow feedback. For frequently deployed services, trunk-based development or GitHub Flow is usually preferable.
6. Use trunk-based development: protected `main`, short-lived branches, and small PRs that are merged within a day. Require CI checks (build, tests, lint, security scanning) and at least one review with CODEOWNERS for sensitive areas, a merge queue to keep `main` green under frequent merges, and squash merges for a clean history. Every merge to `main` triggers an automated deployment pipeline with progressive rollout, and incomplete features are hidden behind feature flags. Keep reviews fast via team agreements, track PR size and review latency, and use `git revert` for quick rollback of bad changes.
7. Treat the secret as compromised immediately, regardless of how quickly it was removed: revoke or rotate it at the provider, check access logs for misuse, and update the systems that use it with the new secret from a secret manager. Removing it from history with `git filter-repo` or BFG and force pushing, coordinated with the team, reduces future exposure, and hosting providers may need to purge cached references, but rotation is what actually fixes the risk. Then add prevention: secret scanning with push protection, pre-commit hooks, and moving secrets out of code into a secret manager.
8. Use `git bisect`, which performs a binary search through history. Start with `git bisect start`, mark the current commit as bad with `git bisect bad`, and mark a known good commit with `git bisect good <sha>`. Git checks out the midpoint; you test and mark it good or bad, and it halves the range each time, finding the first bad commit among 200 in about eight steps. It can be fully automated with `git bisect run <test-script>` that exits non-zero on failure. Afterwards, inspect the commit, write a regression test, and fix it.
