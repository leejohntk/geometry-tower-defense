#!/usr/bin/env bash
# PostToolUse hook — triggered after Write or Edit
# Reminds about dotnet build after .cs file edits

FILE="$1"

# Only trigger for C# files
if echo "$FILE" | grep -qE '\.cs$'; then
    echo ""
    echo "=== REMINDER: .cs file edited ==="
    echo "File: $FILE"
    echo "Run: dotnet build"
    echo "Run: dotnet test (if tests exist)"
    echo ""
fi

# For .tscn files edited through MCP, no reminder needed (MCP handles consistency)
