using System.Collections.Generic;

namespace AIBridgeCLI.Commands
{
    public class HarnessCommandBuilder : BaseCommandBuilder
    {
        public override string Type => "harness";
        public override string Description => "Harness capability snapshot and readiness status (CLI-only)";

        public override string[] Actions => new[]
        {
            "status"
        };

        protected override Dictionary<string, List<ParameterInfo>> ActionParameters => new Dictionary<string, List<ParameterInfo>>
        {
            ["status"] = new List<ParameterInfo>
            {
                new ParameterInfo("file", "Capability snapshot path. Defaults to .aibridge/harness/capabilities.json", false),
                new ParameterInfo("max-age-minutes", "Freshness threshold in minutes", false, "60"),
                new ParameterInfo("detail", "Output detail: compact, full", false, "compact"),
                new ParameterInfo("include-snapshot", "Include the full snapshot JSON in output", false, "false")
            }
        };
    }
}
