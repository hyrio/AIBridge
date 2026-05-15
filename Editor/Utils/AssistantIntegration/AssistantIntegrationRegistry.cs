using System.Collections.Generic;

namespace AIBridge.Editor
{
    /// <summary>
    /// AI 助手集成目标注册表
    /// 
    /// Skills 目录支持说明：
    /// - Claude: 支持 .claude/skills/ 目录（Agent Skills 开放标准）
    /// - Cursor: 支持 .cursor/skills/ 目录（Agent Skills 开放标准）
    /// - Codex/Cline: AIBridge 统一安装到项目内 Skills 目录，便于规则文件引用和后续扩展。
    /// </summary>
    internal static class AssistantIntegrationRegistry
    {
        public static IReadOnlyList<AssistantIntegrationTarget> GetTargets()
        {
            return new[]
            {
                new AssistantIntegrationTarget
                {
                    Id = "claude",
                    DisplayName = "Claude",
                    SupportsSkillDirectory = true,
                    RootRuleFileName = "CLAUDE.md",
                    SkillDirectoryRelativePath = ".claude/skills/aibridge",
                    SkillFileName = "SKILL.md",
                    RootRuleTemplateRelativePath = "Templates~/Rules/Claude.RootRule.md",
                    MissingRootRuleStrategy = MissingRootRuleStrategy.CreateWithInjectedBlock,
                    TemplateId = "unity-integration",
                    RuleTarget = "root-rule"
                },
                new AssistantIntegrationTarget
                {
                    Id = "codex",
                    DisplayName = "Codex",
                    SupportsSkillDirectory = true,
                    RootRuleFileName = "AGENTS.md",
                    SkillDirectoryRelativePath = ".codex/skills/aibridge",
                    SkillFileName = "SKILL.md",
                    RootRuleTemplateRelativePath = "Templates~/Rules/Codex.RootRule.md",
                    MissingRootRuleStrategy = MissingRootRuleStrategy.CreateWithInjectedBlock,
                    TemplateId = "unity-project-rules",
                    RuleTarget = "root-rule"
                },
                new AssistantIntegrationTarget
                {
                    Id = "cursor",
                    DisplayName = "Cursor",
                    SupportsSkillDirectory = true,
                    RootRuleFileName = ".cursor/rules/aibridge.mdc",
                    SkillDirectoryRelativePath = ".cursor/skills/aibridge",
                    SkillFileName = "SKILL.md",
                    RootRuleTemplateRelativePath = "Templates~/Rules/Cursor.RootRule.md",
                    MissingRootRuleStrategy = MissingRootRuleStrategy.CreateWithInjectedBlock,
                    TemplateId = "unity-project-rules",
                    RuleTarget = "root-rule"
                },
                new AssistantIntegrationTarget
                {
                    Id = "cline",
                    DisplayName = "Cline",
                    SupportsSkillDirectory = true,
                    RootRuleFileName = ".clinerules/aibridge.md",
                    SkillDirectoryRelativePath = ".clinerules/skills/aibridge",
                    SkillFileName = "SKILL.md",
                    RootRuleTemplateRelativePath = "Templates~/Rules/Cline.RootRule.md",
                    MissingRootRuleStrategy = MissingRootRuleStrategy.CreateWithInjectedBlock,
                    TemplateId = "unity-project-rules",
                    RuleTarget = "root-rule"
                }
            };
        }
    }
}
