#!/usr/bin/env bash
# Stop hook — capture session transcript, trigger distiller if threshold met
# Threshold: 12+ tool calls since last distillation AND 2+ file edits

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
TRANSCRIPT_DIR="$REPO_ROOT/.claude/transcripts"
DISTILLER_MARKER="$REPO_ROOT/.claude/transcripts/.last_distillation"
TIMESTAMP=$(date -u +%Y-%m-%dT%H:%M:%S)
TRANSCRIPT_FILE="$TRANSCRIPT_DIR/session-${TIMESTAMP}.txt"

mkdir -p "$TRANSCRIPT_DIR"

echo "=== Stop Hook: ${TIMESTAMP} ==="

# Capture transcript (if available via stdin or env)
# The stop hook receives session metadata; actual transcript capture
# depends on what the harness provides
echo "Session ended at: ${TIMESTAMP}" > "$TRANSCRIPT_FILE"

# Count tool calls and edits since last distillation
TOOL_COUNT=0
EDIT_COUNT=0
LAST_DISTILL=0

if [ -f "$DISTILLER_MARKER" ]; then
    LAST_DISTILL=$(cat "$DISTILLER_MARKER")
fi

# Count activity in current transcript directory
if [ -d "$TRANSCRIPT_DIR" ]; then
    # Count transcripts since last distillation (approximate)
    NEW_TRANSCRIPTS=$(find "$TRANSCRIPT_DIR" -name "session-*.txt" -newer "$DISTILLER_MARKER" 2>/dev/null | wc -l | tr -d ' ')
    echo "Transcripts since last distillation: ${NEW_TRANSCRIPTS}"
fi

# Distiller trigger threshold check
# This is approximate — the harness itself tracks exact tool/edit counts
# The hook provides the signal; the orchestrator decides whether to spawn distiller
echo ""
echo "=== Distiller Trigger Check ==="
echo "Check if activity threshold met (12+ tool calls, 2+ file edits since last distillation)."
echo "If threshold met: orchestrator should spawn distiller sub-agent."
echo "Transcript stored in: $TRANSCRIPT_FILE"
