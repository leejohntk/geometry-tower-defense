# Branch Protection Setup

## GitHub Branch Protection Rules

After pushing the initial scaffold, configure these on the `main` branch:

### Required Settings

1. **Require a pull request before merging**
   - Require approvals: 1 (human approval)
   - Dismiss stale pull request approvals when new commits are pushed: yes
   - Require review from Code Owners: no (human is the sole approver)

2. **Require status checks to pass before merging**
   - `dotnet build` (if CI is set up)
   - `dotnet test` (if CI is set up)

3. **Require conversation resolution before merging** — yes

4. **Do not allow bypassing the above settings** — yes
   - Do not allow admins to bypass: yes

5. **Restrict who can push to matching branches** — yes

6. **Allow force pushes** — no

7. **Allow deletions** — no

### Setup via GitHub CLI

```bash
# After pushing initial commit to main
gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/branches/main/protection \
  --method PUT \
  --input - <<EOF
{
  "required_status_checks": null,
  "enforce_admins": true,
  "required_pull_request_reviews": {
    "required_approving_review_count": 1,
    "dismiss_stale_reviews": true
  },
  "restrictions": null,
  "allow_force_pushes": false,
  "allow_deletions": false,
  "block_creations": false,
  "required_conversation_resolution": true
}
EOF
```

### Hook + Permission Defense in Depth

Branch protection on GitHub is the primary defense. The pre-tool-use hook and permission deny rules provide defense in depth within the session — they catch push-to-main attempts before they reach GitHub.
