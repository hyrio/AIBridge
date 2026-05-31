---
name: aibridge-development-workflow
description: "AIBridge/Unity 多分支开发工作流入口。Use when creating, modifying, fixing, debugging, reviewing, or validating C# code, Unity assets, Prefabs, Editor tools, package structure, tests, Skills, Runtime behavior, logs, or workflow recipes. Do not use for pure Q&A or simple code explanation with no investigation."
---

# AIBridge Development Workflow

## 目标

作为 AIBridge/Unity 任务入口，完成 Skill 匹配、Harness 能力探测、任务分流、风险判断、执行和收尾验证。各阶段默认内部执行；只有能力降级、风险确认、验证失败或用户要求时，才把阶段状态展开给用户。

## 按需读取

- `references/branch-selection.md`：任务分流规则、触发条件和交接边界。
- `references/harness-readiness.md`：Harness 能力探测模式、snapshot、fallback、resume、证据回传。
- `references/risk-gates.md`：需求确认、风险分级、必须暂停确认的场景。
- `references/coding-rules.md`：C#、Unity、注释、硬编码、重复代码规则。
- `references/editor-generation.md`：复杂一次性 Editor C# 脚本规范。
- `references/checklist.md`：实施分支最终检查清单。
- `references/debug-investigation-workflow.md`：调试诊断分支流程和证据规则。
- `references/debug-investigation-checklist.md`：调试诊断分支检查清单。

## Skill 匹配

- 所有开发、调试、审查、验证或 workflow 任务至少包含 `aibridge-development-workflow`。
- 涉及 AIBridge CLI、Unity 编译、日志、资源、Prefab、场景或 Inspector 操作时，加入 `aibridge`。
- 涉及 Runtime/Player/Play Mode、输入、截图、性能、handler 或运行时调用分析时，加入 `aibridge`；需要多目标采集、对抗验证或 recipe 时，再加入 `aibridge-workflow-orchestration`。
- C# 代码查找、源码导航、符号、定义、引用、实现、派生类型、调用者或诊断查询：只有 `aibridge-code-index` 已安装且项目规则未关闭 Code Index 时，优先加入 `aibridge-code-index`；否则使用 `rg` 和常规文件读取。
- 字面量字符串、注释、配置、文件名、非 C# 代码、Unity 资源、Prefab 或 Scene 搜索：使用 `rg`、文件读取或常规 AIBridge 命令，不使用 Code Index。
- 复杂 Prefab 资源修改、批量 SerializedProperty、子物体/组件创建或引用写入：加入 `aibridge-prefab-patch`。
- 直接修改 Unity YAML 文本序列化文件，或 AIBridge 不支持的 Prefab/Scene/ScriptableObjectTable 结构修改：加入 `unity-yaml-editing`。
- batch、multi、长脚本、stdin 或脚本自动化：加入 `aibridge-batch-script`。
- Workflow recipe、多 Agent 编排、并行/流水线分工、对抗验证、多目标 sweep、结构化 artifact 或 workflow 方案设计：加入 `aibridge-workflow-orchestration`。
- 创建或修改 Skill：加入 `skill-creator`。
- 复杂一次性 Editor 侧 C# 任务：读取 `references/editor-generation.md`，优先评估 `.aibridge/code/*.csx`。

## Harness 能力探测模式

进入开发、调试、审查、验证或 workflow 任务前，读取 `references/harness-readiness.md` 的入口规则。优先使用 `.aibridge/harness/capabilities.json` 或 `$CLI harness status` 的 compact 结果；只有 snapshot 缺失、过期、无效，或任务需要未确认能力时，才补测任务必需能力。

不把静态检查、`dotnet build` 或推断说成 Unity 验证通过。`agent` / `manual` step 需要当前 harness、主 agent 或外部执行器完成，并用 `workflow import` 回传结构化结果。

## 任务分流

读取 `references/branch-selection.md`，选择一个主分支；其它分支只作为交接或验证补充。

- **实施分支**：创建、修改、修复、重构、迁移或生成代码/资源。完成后按 `references/checklist.md` 验证。
- **调试诊断分支**：排查、复现、追踪日志/Runtime/Player/Play Mode、分析根因。读取调试 workflow 和 checklist。
- **审查分支**：review/audit/检查风险且用户未要求修改。只读优先，结论区分 confirmed/refuted/uncertain。
- **验证分支**：编译、日志、截图、Runtime/UI 验证或回归确认。选择匹配的 AIBridge 命令或 recipe。
- **编排分支**：workflow recipe、多 Agent、并行 sweep、对抗验证或结构化 artifact。读取 `aibridge-workflow-orchestration` references。

## 风险门控

低风险且目标明确时继续执行；以下情况先按 `references/risk-gates.md` 给方案并等待确认：

- 需求目标、交互行为、数据来源或验收标准不明确。
- 多个合理方案会影响公共 API、配置格式、资源结构或用户体验。
- 可能破坏兼容性、迁移数据、批量改 Prefab/场景、删除资源或引入新依赖。
- 用户明确要求先分析、先给方案或不要直接改。

## 执行规则

- 修改前用 `rg` / `rg --files` 搜索现有实现、工具类、命名模式和相邻代码。
- 涉及 Unity 对象、Prefab、资源、Console 时，结合 `aibridge` 查询。
- 涉及 Runtime/Player/Play Mode 调试时，先收集 target、日志、截图、性能、handler 或调用结果，再提出候选根因。
- 修改 C# 或 Unity 逻辑前读取 `references/coding-rules.md`。
- 使用一次性 Editor 脚本前读取 `references/editor-generation.md`。
- 使用多 Agent 编排前读取 `aibridge-workflow-orchestration/references/orchestration-patterns.md`。

## 收尾

结束前按主分支执行对应检查清单。最终回复只报告实际执行的验证、失败/阻塞项、未验证原因和必要的关键改动；不要为了展示流程而输出完整模式表。
