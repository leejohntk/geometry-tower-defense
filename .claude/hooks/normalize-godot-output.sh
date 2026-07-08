#!/usr/bin/env bash
# PostToolUse hook — triggered after mcp__godot__get_debug_output
# Transforms verbose Godot logs into structured error summaries
# Reads from stdin, outputs structured summary

INPUT=$(cat)

# Extract errors and warnings
ERRORS=$(echo "$INPUT" | grep -iE 'error|exception|fail|crash' | head -20)
WARNINGS=$(echo "$INPUT" | grep -iE 'warn|deprecated' | head -20)

# If nothing actionable, output minimal
if [ -z "$ERRORS" ] && [ -z "$WARNINGS" ]; then
    echo "=== Godot Output: Clean ==="
    echo "(no errors or warnings detected)"
    exit 0
fi

# Output structured summary
if [ -n "$ERRORS" ]; then
    echo "=== Godot Errors ==="
    echo "$ERRORS"
    echo ""
fi

if [ -n "$WARNINGS" ]; then
    echo "=== Godot Warnings ==="
    echo "$WARNINGS"
    echo ""
fi

echo "=== Summary ==="
echo "Errors: $(echo "$ERRORS" | grep -c '.' 2>/dev/null || echo 0)"
echo "Warnings: $(echo "$WARNINGS" | grep -c '.' 2>/dev/null || echo 0)"
