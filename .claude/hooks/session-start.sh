#!/usr/bin/env bash
# SessionStart hook — inject context + health check
# Reads state.json for crash recovery, warns if on main branch

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
STATE_FILE="$REPO_ROOT/.claude/state.json"
MEMORY_FILE="$REPO_ROOT/.claude/memory/current-feature.md"

echo "=== Session Start ==="
echo "Repository: $REPO_ROOT"

# 1. Check if we're on main branch
CURRENT_BRANCH=$(git -C "$REPO_ROOT" branch --show-current 2>/dev/null || echo "unknown")
if [ "$CURRENT_BRANCH" = "main" ] || [ "$CURRENT_BRANCH" = "master" ]; then
    echo "WARNING: On '$CURRENT_BRANCH' branch. Create a feature branch before implementing."
    echo "  git checkout -b feature/your-feature-name"
fi
echo "Branch: $CURRENT_BRANCH"

# 2. Check for state.json and resume if present
if [ -f "$STATE_FILE" ]; then
    echo ""
    echo "=== Crash Recovery: state.json found ==="
    cat "$STATE_FILE"
    echo ""
    echo "Resume from phase: $(jq -r '.phase' "$STATE_FILE" 2>/dev/null || echo "unknown")"
    echo "Feature: $(jq -r '.feature' "$STATE_FILE" 2>/dev/null || echo "unknown")"
    echo ""
    echo "=== Orchestrator: load state.json and resume from last checkpoint ==="
else
    echo "No state.json — clean start."
fi

# 3. Show current feature context if available
if [ -f "$MEMORY_FILE" ]; then
    echo ""
    echo "=== Current Feature Context ==="
    head -5 "$MEMORY_FILE"
fi

# 4. Quick health check
echo ""
echo "=== Quick Health Check ==="
echo "dotnet SDK: $(dotnet --version 2>/dev/null || echo 'NOT FOUND')"
echo "godot CLI: $(which godot 2>/dev/null || echo 'NOT FOUND')"
echo "git: $(which git 2>/dev/null || echo 'NOT FOUND')"

echo ""
echo "=== Session Ready ==="
