# {Feature Name}

## Verification Checklist

### Build
- [ ] `dotnet build` passes with zero errors

### Tests
- [ ] `dotnet test` passes — {N} tests
- [ ] New tests cover acceptance criteria
- [ ] Edge cases covered

### Multi-Lens Review
- [ ] Security lens: {X CRITICAL, Y WARNING, Z INFO}
- [ ] Performance lens: {X CRITICAL, Y WARNING, Z INFO}
- [ ] Correctness lens: {X CRITICAL, Y WARNING, Z INFO}
- [ ] All CRITICAL findings fixed
- [ ] Adversarial challenge rounds resolved

### Holdout Verification
- [ ] All {N} holdout scenarios pass
- [ ] Holdout results: {summary}

### Simplify
- [ ] `/simplify` refactor pass completed
- [ ] Build + tests still pass after simplify

### Spec Compliance
- [ ] All acceptance criteria met
- [ ] Edge cases handled

## Summary

**Files changed:** {count}
**Lines added:** {count}
**Lines removed:** {count}

## Notes

{Any notes for the human reviewer — known limitations, design decisions, future work}

---
🤖 Generated with [Claude Code](https://claude.com/claude-code)
