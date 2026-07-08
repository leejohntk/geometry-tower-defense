# Branch Protection Setup

## GitHub Branch Protection Rules

After pushing the initial scaffold, configure these on the `main` branch:

### Required Settings

1. **Require a pull request before merging**
   - Require approvals: 1 (human approval)
   - Dismiss stale pull request approvals when new commits are pushed: yes
   - Require review from Code Owners: no (human is the sole approver)

2. **Require status checks to pass before merging**
   - `build` — `.github/workflows/ci.yml` runs `dotnet build` + `dotnet test` on every PR
   - Free tier: 2,000 mins/month (public repos unlimited)

3. **Require conversation resolution before merging** — yes

4. **Do not allow bypassing the above settings** — yes
   - Do not allow admins to bypass: yes

5. **Restrict who can push to matching branches** — yes

6. **Allow force pushes** — no

7. **Allow deletions** — no

### Status Checks via CI

`.github/workflows/ci.yml` runs on every PR. The `build` job runs `dotnet build` + `dotnet test` on Ubuntu. After the workflow runs once, add `build` as a required status check in GitHub branch protection settings. Free on public repos (2,000 mins/month).

### Setup via GitHub CLI

```bash
# After CI workflow runs at least once on the repo
gh api repos/$(gh repo view --json nameWithOwner -q .nameWithOwner)/branches/main/protection \
  --method PUT \
  --input - <<EOF
{
  "required_status_checks": {
    "strict": true,
    "contexts": ["build"]
  },
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
