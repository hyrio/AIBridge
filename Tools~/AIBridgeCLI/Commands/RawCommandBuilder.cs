using System;
using System.Collections.Generic;
using AIBridgeCLI.Core;

namespace AIBridgeCLI.Commands
{
    /// <summary>
    /// Generic command builder for project-local AIBridge commands.
    /// </summary>
    public class RawCommandBuilder : BaseCommandBuilder
    {
        public override string Type => "raw";
        public override string Description => "Send a raw request to any AIBridge command type";

        public override string[] Actions => new[]
        {
            "send"
        };

        protected override Dictionary<string, List<ParameterInfo>> ActionParameters => new Dictionary<string, List<ParameterInfo>>
        {
            ["send"] = new List<ParameterInfo>
            {
                new ParameterInfo("type", "Target AIBridge command type", true),
                new ParameterInfo("action", "Target command action", false),
                new ParameterInfo("targetAction", "Alias for target command action when the parameter name 'action' is reserved by the raw command", false)
            }
        };

        public override CommandRequest Build(string action, Dictionary<string, string> options)
        {
            if (string.IsNullOrEmpty(action))
            {
                action = "send";
            }

            if (!string.Equals(action, "send", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"Unknown raw action: {action}. Supported: send");
            }

            var request = base.Build(action, options);
            if (!request.@params.TryGetValue("type", out var typeValue) || typeValue == null || string.IsNullOrWhiteSpace(typeValue.ToString()))
            {
                throw new ArgumentException("Missing required parameter: --type");
            }

            request.type = typeValue.ToString();
            request.@params.Remove("type");
            if (request.@params.TryGetValue("targetAction", out var targetActionValue))
            {
                request.@params["action"] = targetActionValue;
                request.@params.Remove("targetAction");
            }
            else if (!options.ContainsKey("action") && request.@params.TryGetValue("action", out var actionValue) && string.Equals(actionValue?.ToString(), "send", StringComparison.OrdinalIgnoreCase))
            {
                request.@params.Remove("action");
            }

            return request;
        }

        public override string GetHelp(string action = null)
        {
            var help = base.GetHelp(string.IsNullOrEmpty(action) ? "send" : action);
            return help + Environment.NewLine
                        + "Examples:" + Environment.NewLine
                        + "  AIBridgeCLI raw --type custom_command --action run --name value" + Environment.NewLine
                        + "  AIBridgeCLI raw send --type custom_command --targetAction run --payloadBase64 <base64>" + Environment.NewLine
                        + "Project-specific examples belong in the project-local command description." + Environment.NewLine;
        }
    }
}
