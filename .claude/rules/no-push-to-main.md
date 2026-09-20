---
paths: ["**/*"]
---

# No Push to Main

**Deterministic enforcement.** Cannot be bypassed.

## Rule

- `git push origin main` — blocked
- `git push origin master` — blocked
- `git push --force` to any branch — blocked

## No Local Commits on Main

- Never `git commit` while on `main` — including chore, harness, memory, and merge-bookkeeping commits.
- All changes — game code, harness config, memory, holdouts — flow through a feature branch → PR → squash merge.
- Reason: local commits on `main` diverge from `origin/main` and can never be pushed (see above), leaving `main` permanently "ahead N" with work that has no remote copy to recover from.

## Why

Branch protection on main. All changes flow through feature branches → PR → squash merge.

## Correct Workflow

```bash
git checkout -b feature/my-feature
# ... work ...
git push origin feature/my-feature
# PR created via gh CLI
```

## Enforcement

Pre-tool-use hook blocks these commands with `{"decision": "deny"}`. Permission deny in `settings.json` provides belt-and-suspenders.
