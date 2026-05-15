---
templateId: unity-integration
assistant: claude
version: 3
target: root-rule
---
## AIBridge Unity Integration

**Skills**: `aibridge` - Unity CLI automation tool; `aibridge-prefab-patch` - complex prefab asset edits

**CLI**: `{{CLI_PATH}}` (outputs JSON by default)

**Core Workflows**:
- **Compile**: Use `compile unity` (default), `compile dotnet` (optional validation only)
- **Asset Search**: Use `asset search/find --format paths` before generic filesystem search
- **Property Edits**: Use `inspector get_properties/find_property/set_property/set_properties`; for prefab assets pass `assetPath + objectPath + componentName`
- **Prefab Patch**: Use `aibridge-prefab-patch` for complex prefab asset edits, then run `prefab patch --ops <file>` with dry-run first
- **Console Logs**: `get_logs --logType Error`
- **PowerShell JSON**: Avoid inline complex `--json`; build a JSON variable, escape embedded quotes, and pass `--values $values`
- **Multi Commands**: `multi --cmd` auto-wraps plain CLI lines as Batch `call`; use `multi --stdin` for long or JSON-heavy scripts
- **Scene/GameObject**: Create, modify, inspect hierarchy
- **Visual Verification**: `screenshot game`, `screenshot gif --frameCount 50` (Play Mode)

**Quick Reference**:
```bash
{{CLI_PATH}} compile unity
{{CLI_PATH}} get_logs --logType Error
{{CLI_PATH}} asset search --mode script --keyword "Player" --format paths
{{CLI_PATH}} gameobject create --name "Cube" --primitiveType Cube
{{CLI_PATH}} inspector set_property --assetPath "Assets/UI/LoginPanel.prefab" --objectPath "Root/Button" --componentName "RectTransform" --propertyName "m_AnchoredPosition.x" --value 100
{{CLI_PATH}} prefab patch --prefabPath "Assets/Prefabs/Player.prefab" --ops "patch_ops.json"
{{CLI_PATH}} multi --cmd "editor log --message Step1&get_logs --logType Error --count 1"
```

**Skill Documentation**: [AIBridge Skill]({{SKILL_DOC_PATH}}), [Prefab Patch Skill]({{PREFAB_PATCH_SKILL_DOC_PATH}})
