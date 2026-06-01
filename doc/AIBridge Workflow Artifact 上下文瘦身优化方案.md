# AIBridge Workflow Artifact 上下文瘦身优化方案

更新时间：2026-05-29

## 目标

把 AIBridge 获取到的日志、截图、GIF、Runtime 结果、测试结果、Code Index 结果等统一登记为 workflow artifact，让 Codex 在对话上下文中只保留必要摘要、路径、hash、gate 结论和复现命令。

说明：现有截图和 GIF 命令已经默认缓存到 `.aibridge/screenshots`，Agent 通常只拿到路径、尺寸等信息，不会把图片/GIF 二进制塞进上下文。本方案对截图/GIF的优化重点是把这些已有路径纳入 workflow run 的 manifest、artifact refs、gate 和 report，而不是默认复制或改变原截图输出目录。

核心目标不是让所有命令默认创建 workflow run，而是让复杂任务有一个可复用的 artifact sink：

- 大内容落盘或引用已有缓存路径，不进入聊天上下文。
- 聊天只保留高信号摘要和 artifact 引用。
- 简单命令保持原兼容行为。
- Codex 能通过 runId/reportPath 继续、复盘或交接任务。

## 适合放入 Artifact、不直接占用上下文的内容

| 内容 | Artifact kind | 上下文只保留 | 说明 |
|---|---|---|---|
| 原始 CLI command result JSON | `command-result` | commandId、success、resultPath | 所有 workflow step 默认归档，避免完整 JSON 反复进入上下文。 |
| Unity Console 日志 | `console-log` | error/warning count、前 3-5 条关键错误、artifact path | 完整日志、stack trace、长消息落盘。 |
| Runtime 日志 | `runtime-log` | targetId、error count、前 3-5 条关键错误、artifact path | 多目标 sweep 时尤其省上下文。 |
| Game/Scene screenshot | `screenshot` | path、尺寸、sha256、简短说明 | 复用 `.aibridge/screenshots` 现有缓存路径，默认只登记 ArtifactRef。 |
| GIF 录制 | `gif` | path、尺寸、帧数/时长、sha256 | 复用 `.aibridge/screenshots` 现有缓存路径，默认只登记 ArtifactRef；归档模式才复制。 |
| Runtime screenshot | `runtime-screenshot` | targetId、path、尺寸、sha256 | 复用 Runtime 返回的本机路径或 output 路径，默认登记到 run。 |
| Runtime perf | `runtime-perf` | avg/fps/p95/hitch 摘要、artifact path | 完整采样序列落盘。 |
| Runtime status / handlers / call | `runtime-status` / `runtime-handler-result` | reachable、targetId、handler 名称、核心返回字段 | 大 payload 或业务返回体落盘。 |
| Unity compile 结果 | `validation-report` | success、errorCount、前几条编译错误 | 完整编译详情落盘，最终仍以 `compile unity` 为权威。 |
| Unity test run 结果 | `validation-report` | total/passed/failed、失败测试名、artifact path | 失败堆栈和完整测试输出落盘。 |
| Code Index 查询结果 | `code-index-result` | semantic、top hits、artifact path | 大量 references/callers/diagnostics 不直接粘贴。 |
| Prefab patch dry-run / proposal | `patch-proposal` | affected files、risk、artifact path | 大 JSON patch、SerializedProperty 明细落盘。 |
| Batch/multi 执行 transcript | `validation-report` | step count、failed step、artifact path | 长脚本每步输出不进入主上下文。 |
| 最终报告 | `workflow-report` | reportPath、status、failed gates | Markdown 报告落盘，可按需打开。 |

## 不适合默认放入 Artifact 的内容

| 内容 | 建议 |
|---|---|
| `focus`、单次 `dialog status` 这类极小 CLI-only 状态 | 默认直接返回；只有绑定 run 时才归档。 |
| 用户 token、auth token、机器路径中的敏感片段 | 进入 artifact 前做字段级 redaction。 |
| 未确认的大型二进制、超长 GIF、长时间 perf 原始采样 | 默认引用 sourcePath；超过阈值不复制。 |
| Git diff 的完整大文本 | 优先让 Git 管理；workflow 中只记录摘要、文件列表和验证结果。 |
| LLM 推理过程 | 不落 artifact；只保存结构化结论、证据、Verdict。 |

## 上下文输出策略

CLI 输出分三层：

1. **Compact summary**：进入聊天上下文，保持小而稳定。
2. **ArtifactRef**：提供 kind、path、sourceCommand、sha256、summary。
3. **Payload**：完整 JSON、日志、图片、GIF、采样、报告，只写入 run 目录。

建议每个命令在 workflow 上下文中返回：

```json
{
  "success": true,
  "summary": {
    "kind": "console-log",
    "errorCount": 0,
    "artifactCount": 2
  },
  "artifacts": [
    {
      "artifactId": "art_console_log_x",
      "kind": "console-log",
      "path": ".aibridge/workflows/runs/<runId>/artifacts/art_console_log_x/artifact.json"
    }
  ]
}
```

避免默认返回：

- 完整日志数组。
- 图片/GIF 二进制或 base64。现有截图/GIF命令已满足这一点，后续只需补 artifact 登记。
- 大型 `JObject` payload。
- 超过预算的 stack trace。

## Skill 上下文瘦身

Artifact 瘦身解决“大证据不进聊天上下文”，Skill 作用域解决“模式专用规则不跨阶段堆积”。两者应配套使用：

- `aibridge-development-workflow` 作为轻量常驻入口，负责分流、风险门控和收尾。
- Skill 路由是 Preflight 步骤，不是业务模式；它只计算 baseline / active / deferred / guarded Skills。
- 其它 Skill 默认只在当前主分支、workflow phase 或具体 step 内有效。
- Mode Exit、phase / step 结束时释放模式专用 Skill 的上下文依赖，只保留 compact handoff、artifact refs、gate 状态和未关闭风险。
- 下一阶段如果仍需要同一 Skill，重新匹配并按需读取，不依赖上一阶段遗留上下文。
- 这里的释放是 workflow 级规则；是否真正从模型窗口移除已读 Skill 文本，取决于外部 AI harness 的子 Agent、上下文压缩或新会话能力。

推荐 handoff 字段：

```json
{
  "completedMode": "prefab-patch",
  "releasedSkills": ["aibridge-prefab-patch"],
  "nextRecommendedSkills": ["aibridge"],
  "summary": "已应用 Prefab patch，等待 Unity 编译验证。",
  "artifactRefs": ["art_patch_proposal_001"],
  "gates": [
    {
      "id": "unity-compile",
      "status": "pending"
    }
  ],
  "openRisks": []
}
```

对于 recipe，可使用 `requiredSkills` 和 `releaseSkillsAfter` 作为外部 AI harness 的提示元数据；跨 Agent 或跨 phase 的交接结果可按 `SkillHandoff` / `skill-handoff` artifact 导入。AIBridge CLI 只校验和展示这些字段，不负责安装、卸载或物理清理模型上下文。

## 推荐默认策略

### 普通命令

默认不创建 run，保持兼容：

```bash
$CLI get_logs --logType Error --count 50
$CLI screenshot game
```

### 显式绑定 run

新增通用选项：

```bash
$CLI get_logs --logType Error --workflow-run <runId>
$CLI screenshot game --workflow-run <runId>
$CLI runtime screenshot --target latest --workflow-run <runId>
```

或使用环境变量：

```bash
AIBRIDGE_WORKFLOW_RUN_ID=<runId>
```

### Codex 复杂任务

Codex 在复杂任务开始时创建或选择 active run：

```bash
$CLI workflow begin --recipe unity-change-implementation
$CLI compile unity --workflow-run <runId>
$CLI get_logs --logType Error --workflow-run <runId>
$CLI workflow report --run <runId> --format markdown
```

`workflow begin` 是建议新增命令；当前已落地的 `workflow run-cli` 已经具备 run artifact 自动归档能力。

## 设计方案

### 1. 抽出共享 `ArtifactSink`

把当前 `workflow run-cli` 内部的 artifact 收集能力下沉为共享服务：

```text
WorkflowArtifactSink
  RecordCommandResult(...)
  RecordJson(...)
  RecordFile(...)
  RecordLog(...)
  RecordScreenshot(...)
  RecordReport(...)
```

调用方：

- `workflow run-cli`
- 普通 CLI 命令的 `--workflow-run`
- Runtime command sender
- Screenshot/GIF 命令
- Test/compile/log/code_index 命令

### 2. 引入 `WorkflowRunContext`

统一解析当前命令是否应该归档：

优先级：

1. `--workflow-run <runId>`
2. `AIBRIDGE_WORKFLOW_RUN_ID`
3. 项目 `.aibridge/workflows/active-run.json`
4. 无 run：保持原行为

上下文内容：

```json
{
  "runId": "wf_x",
  "artifactMode": "auto",
  "copyLimitBytes": 52428800,
  "redaction": true
}
```

### 3. 增加 artifact mode

建议支持：

- `off`：不归档。
- `reference`：只写 ArtifactRef，不复制 payload。
- `copy`：尽量复制 payload 到 run。
- `auto`：小文件复制，大文件引用 sourcePath。

默认：

- 普通命令：`off`
- `--workflow-run`：`auto`
- `workflow run-cli`：`auto`

截图和 GIF 的 `auto` 策略应优先表现为 `reference`：复用 `.aibridge/screenshots` 或 Runtime output 中已有文件，只写 `ArtifactRef`、hash、尺寸、sourceCommand。只有用户要求“归档 run 可独立搬走”时才复制 payload 到 `runs/<runId>/artifacts/`。

### 4. 输出瘦身

在 workflow 上下文中，命令输出默认降级为摘要：

- 日志：只返回 count、top errors、artifact path。
- 截图/GIF：只返回 path、尺寸、sha256、artifact path。
- Runtime perf：只返回关键统计。
- Code Index：只返回 top hits 和 artifact path。

需要完整输出时显式传：

```bash
--artifact-output full
```

默认不建议给 Codex 使用 full。

### 5. 清理策略

复用并强化：

```bash
$CLI workflow clean --older-than 30d --dry-run true
$CLI workflow clean --older-than 30d --dry-run false
$CLI workflow clean --older-than 3d --save-settings true --auto-clean true
```

后续可增加：

- `--max-size`
- `--keep-failed`
- `--keep-latest <n>`

已落地的基础策略：

- `clean` 默认 dry-run。
- `--keep-failed` 默认保留 failed/blocked run。
- `--keep-latest` 默认保留最新 20 个 run。
- `--max-delete` 默认单次最多删除 100 个 run。
- `--save-settings true --auto-clean true` 会写入 `.aibridge/workflows/settings.json`，后续 `workflow run-cli` 开始前自动清理。

## 分阶段实施

### P1：显式 attach

交付：

- `--workflow-run <runId>` 通用选项。
- `AIBRIDGE_WORKFLOW_RUN_ID` 环境变量。
- `WorkflowArtifactSink` 共享化。
- `get_logs`、`screenshot`、`runtime logs`、`runtime screenshot` 优先接入。

验收：

- 普通命令行为不变。
- 显式传 runId 后自动写 artifact。
- 上下文只返回摘要和 artifact refs。

### P2：active run

交付：

- `workflow begin`
- `workflow attach`
- `workflow finish`
- `.aibridge/workflows/active-run.json`

验收：

- Codex 可在复杂任务开始时创建 active run。
- 后续命令不必每次传 `--workflow-run`。
- `workflow finish` 自动生成 report。

### P3：Codex Skill 默认策略

交付：

- 更新 `aibridge-development-workflow`：复杂任务优先创建 active run。
- 更新 `aibridge-workflow-orchestration`：要求大输出只引用 artifact。
- 增加 Skill 作用域生命周期：Skill 路由作为 Preflight 步骤，模式结束后只传 handoff summary、artifact refs 和 gate 状态。
- recipe 支持 `requiredSkills` / `releaseSkillsAfter` 元数据，供外部 AI harness 控制按需加载。
- README 增加“上下文瘦身模式”示例。

验收：

- Codex 对复杂任务默认输出 runId、gate、artifact path。
- 不在聊天中粘贴大日志、截图二进制、完整 JSON。
- 模式切换时不继续依赖已释放 Skill 的详细规则；下一模式需要时重新匹配加载。

## 推荐优先接入命令

第一批：

- `get_logs`
- `screenshot game`：登记已有截图路径、尺寸、hash，不默认复制。
- `screenshot scene_view`：登记已有截图路径、尺寸、hash，不默认复制。
- `screenshot gif`：登记已有 GIF 路径、尺寸、hash，不默认复制。
- `runtime logs`
- `runtime screenshot`：登记 Runtime 返回的 `imagePath` / `pcPath` / `output`。
- `runtime perf`
- `compile unity`
- `test run`

第二批：

- `runtime status`
- `runtime handlers`
- `runtime call`
- `code_index`
- `prefab patch --dry-run`
- `batch`
- `multi`

## 风险与处理

| 风险 | 处理 |
|---|---|
| `.aibridge` 体积变大 | 默认 `auto` 阈值，GIF/大日志只引用，提供 `workflow clean`。 |
| 旧脚本依赖完整 JSON | 普通命令默认不变；只有 workflow 上下文才瘦身。 |
| Codex 看不到完整细节 | 通过 artifact path 按需读取，不默认塞上下文。 |
| 敏感信息落盘 | 增加 redaction 规则，token/url/header 默认脱敏。 |
| active run 归属混乱 | active run 只由显式命令创建；可 `workflow attach/finish` 明确切换。 |

## 最小验收标准

1. 普通 AIBridge 命令兼容旧行为。
2. `--workflow-run` 能让日志、截图、GIF、Runtime 证据自动写入 run artifact。
3. Codex 默认只看到摘要、runId、artifact path、gate 结论。
4. 大文件不复制或可清理。
5. `workflow report` 能汇总显式 attach 的证据。
