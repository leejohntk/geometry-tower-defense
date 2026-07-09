#!/usr/bin/env bash
# Stop hook — tracks tool call count, signals distiller when threshold met.
# Threshold: 12+ tool calls since last distillation.
# Creates flag file that orchestrator checks after session or on long runs.
#
# Files (all under .claude/transcripts/):
#   .tool_count        — integer counter, incremented each stop
#   .distiller_needed  — flag file created when counter >= threshold
#   .last_distillation — timestamp written by distiller when it completes

set -euo pipefail

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
TRANSCRIPT_DIR="$REPO_ROOT/.claude/transcripts"
COUNTER_FILE="$TRANSCRIPT_DIR/.tool_count"
FLAG_FILE="$TRANSCRIPT_DIR/.distiller_needed"
MARKER_FILE="$TRANSCRIPT_DIR/.last_distillation"
THRESHOLD=12

mkdir -p "$TRANSCRIPT_DIR"

# Read current counter (default 0 if file missing or corrupt)
COUNTER=0
if [ -f "$COUNTER_FILE" ]; then
    COUNTER=$(cat "$COUNTER_FILE" 2>/dev/null || echo 0)
    # Validate it's an integer
    if ! echo "$COUNTER" | grep -qE '^[0-9]+$'; then
        COUNTER=0
    fi
fi

# Increment
COUNTER=$((COUNTER + 1))
echo "$COUNTER" > "$COUNTER_FILE"

# Determine last distillation time
LAST_DISTILL="never"
if [ -f "$MARKER_FILE" ]; then
    LAST_DISTILL=$(cat "$MARKER_FILE" 2>/dev/null || echo "unknown")
fi

# Check threshold: if counter >= threshold AND flag not already set, create flag
if [ "$COUNTER" -ge "$THRESHOLD" ]; then
    if [ ! -f "$FLAG_FILE" ]; then
        echo "$(date -u +%Y-%m-%dT%H:%M:%S)" > "$FLAG_FILE"
        echo "DISTILLER_STATUS: needed (counter=${COUNTER}, last_distillation=${LAST_DISTILL})"
    else
        FLAG_AGE=$(cat "$FLAG_FILE" 2>/dev/null || echo "unknown")
        echo "DISTILLER_STATUS: still_needed (counter=${COUNTER}, flag_set_at=${FLAG_AGE})"
    fi
else
    REMAINING=$((THRESHOLD - COUNTER))
    echo "DISTILLER_STATUS: not_needed (counter=${COUNTER}/${THRESHOLD}, ${REMAINING} calls remaining)"
fi
