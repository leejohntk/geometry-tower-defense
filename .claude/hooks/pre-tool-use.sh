#!/usr/bin/env bash
# PreToolUse hook — deterministic enforcement for Bash commands
# Blocks: push to main/master, force push, rm -rf, sudo
# Returns JSON: {"decision": "allow"} or {"decision": "deny", "reason": "..."}

COMMAND="$1"

# Block push to main
if echo "$COMMAND" | grep -qE 'git push.*(origin|upstream)\s+(main|master)'; then
    echo '{"decision": "deny", "reason": "Push to main/master is blocked. Use feature branches + PR. See .claude/rules/no-push-to-main.md"}'
    exit 0
fi

# Block force push
if echo "$COMMAND" | grep -qE 'git push.*(-f|--force)'; then
    echo '{"decision": "deny", "reason": "Force push is blocked. Use git push --force-with-lease only with explicit human approval."}'
    exit 0
fi

# Block rm -rf (belt-and-suspenders — also in user-level settings)
if echo "$COMMAND" | grep -qE '\brm\s+-rf\b'; then
    echo '{"decision": "deny", "reason": "rm -rf is blocked. Use targeted file removal."}'
    exit 0
fi

# Block sudo (belt-and-suspenders — also in user-level settings)
if echo "$COMMAND" | grep -qE '\bsudo\b'; then
    echo '{"decision": "deny", "reason": "sudo is blocked. Run privileged commands directly as the user."}'
    exit 0
fi

# Allow everything else
echo '{"decision": "allow"}'
