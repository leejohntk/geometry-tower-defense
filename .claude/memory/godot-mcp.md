---
name: godot-mcp
description: Available Godot MCP tools, usage patterns, and limitations
metadata:
  type: reference
---

# Godot MCP

## Available Tools (14)

### Scene Building
- `mcp__godot__create_scene` — create new .tscn file
- `mcp__godot__add_node` — add node to existing scene
- `mcp__godot__load_sprite` — load texture into Sprite2D node
- `mcp__godot__save_scene` — save scene changes (optional newPath for variants)
- `mcp__godot__export_mesh_library` — export scene as MeshLibrary .res

### Project Info
- `mcp__godot__get_project_info` — project metadata
- `mcp__godot__list_projects` — find Godot projects in directory
- `mcp__godot__get_godot_version` — installed Godot version
- `mcp__godot__get_uid` — get UID for file (Godot 4.4+)
- `mcp__godot__update_project_uids` — update UID references

### Running
- `mcp__godot__run_project` — run game (headless or windowed)
- `mcp__godot__stop_project` — stop running game
- `mcp__godot__get_debug_output` — get debug output + errors

### Editor
- `mcp__godot__launch_editor` — launch Godot editor GUI

## Usage Rules

- **Always use MCP for scene work.** Never hand-edit .tscn files.
- **Agents NEVER call `launch_editor`.** The Godot editor is for human visual debugging only.
- **Playtesting uses `run_project`** — launches game directly without editor.
- **No screenshot/visual observation in MCP.** Human is the visual observer via playtesting.

## Patterns

### Creating a New Scene
```
mcp__godot__create_scene(projectPath, scenePath, rootNodeType?)
→ mcp__godot__add_node(projectPath, scenePath, parentNodePath, nodeType, nodeName, properties?)
→ mcp__godot__save_scene(projectPath, scenePath)
```

### Loading a Sprite
```
mcp__godot__load_sprite(projectPath, scenePath, nodePath, texturePath)
```

### Running for Verification
```
mcp__godot__run_project(projectPath, scene?)
→ (wait for output)
→ mcp__godot__get_debug_output()
→ mcp__godot__stop_project()
```
