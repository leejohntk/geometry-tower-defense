#!/usr/bin/env bash
# Health check — verifies harness integrity
# Usable by orchestrator at session start and human anytime via /health

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PASS=0
FAIL=0
WARN=0

green() { echo "  PASS: $1"; PASS=$((PASS + 1)); }
red() { echo "  FAIL: $1"; FAIL=$((FAIL + 1)); }
yellow() { echo "  WARN: $1"; WARN=$((WARN + 1)); }

echo "=== Harness Health Check ==="
echo "Repository: $REPO_ROOT"
echo ""

# 1. dotnet build
echo "--- Build ---"
if dotnet build "$REPO_ROOT" --nologo -v q > /dev/null 2>&1; then
    green "dotnet build passes"
else
    yellow "dotnet build failed — Godot may not be installed yet. Run: brew install --cask godot"
fi

# 2. Godot MCP
echo "--- Godot MCP ---"
if [ -f "/Applications/Godot.app/Contents/MacOS/Godot" ]; then
    green "Godot installed at /Applications/Godot.app"
else
    yellow "Godot not installed. MCP tools will fail. Install: brew install --cask godot"
fi

# 3. Hook scripts exist + executable
echo "--- Hooks ---"
for hook in session-start.sh pre-tool-use.sh post-tool-use.sh stop.sh normalize-godot-output.sh; do
    HOOK_PATH="$REPO_ROOT/.claude/hooks/$hook"
    if [ -f "$HOOK_PATH" ]; then
        if [ -x "$HOOK_PATH" ]; then
            green "Hook $hook exists and is executable"
        else
            red "Hook $hook exists but is NOT executable. Run: chmod +x $HOOK_PATH"
        fi
    else
        red "Hook $hook MISSING"
    fi
done

# 4. Agent definitions exist
echo "--- Agents ---"
for agent in orchestrator implementer reviewer investigator distiller; do
    AGENT_PATH="$REPO_ROOT/.claude/agents/${agent}.md"
    if [ -f "$AGENT_PATH" ]; then
        green "Agent $agent defined"
    else
        red "Agent $agent MISSING"
    fi
done

# 5. settings.json is valid JSON
echo "--- Config ---"
SETTINGS="$REPO_ROOT/.claude/settings.json"
if [ -f "$SETTINGS" ]; then
    if jq empty "$SETTINGS" 2>/dev/null; then
        green "settings.json is valid JSON"
    else
        red "settings.json is INVALID JSON"
    fi
else
    red "settings.json MISSING"
fi

# 6. Memory files parse
echo "--- Memory ---"
MEMORY_COUNT=$(find "$REPO_ROOT/.claude/memory" -name "*.md" 2>/dev/null | wc -l | tr -d ' ')
if [ "$MEMORY_COUNT" -ge 5 ]; then
    green "$MEMORY_COUNT memory files exist"
else
    yellow "$MEMORY_COUNT memory files (expecting 8+)"
fi

if [ -f "$REPO_ROOT/MEMORY.md" ]; then
    green "MEMORY.md index exists"
else
    red "MEMORY.md index MISSING"
fi

# 7. Git branch check
echo "--- Git ---"
CURRENT_BRANCH=$(git -C "$REPO_ROOT" branch --show-current 2>/dev/null || echo "unknown")
echo "  INFO: Current branch: $CURRENT_BRANCH"
if [ "$CURRENT_BRANCH" = "main" ] || [ "$CURRENT_BRANCH" = "master" ]; then
    yellow "On main branch — create feature branch before implementing"
fi

# Summary
echo ""
echo "=== Results: ${PASS} passed, ${FAIL} failed, ${WARN} warnings ==="
if [ "$FAIL" -gt 0 ]; then
    echo "Fix failures above before proceeding."
    exit 1
elif [ "$WARN" -gt 0 ]; then
    echo "Warnings present but harness is functional."
    exit 0
else
    echo "All checks passed. Harness healthy."
    exit 0
fi
