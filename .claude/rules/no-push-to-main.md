---
paths: ["**/*"]
---

# No Push to Main

**Deterministic enforcement.** Cannot be bypassed.

## Rule

- `git push origin main` — blocked
- `git push origin master` — blocked
- `git push --force` to any branch — blocked

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
