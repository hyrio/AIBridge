using System.Collections.Generic;
using NUnit.Framework;

namespace AIBridge.Editor.Tests
{
    public class CodeCommandTests
    {
        [Test]
        public void Execute_WhenDisabled_ReturnsSettingsFailure()
        {
            var settings = AIBridgeProjectSettings.Instance;
            var previousEnabled = settings.EnableCodeExecution;
            var previousAccepted = settings.CodeExecutionRiskAccepted;
            settings.EnableCodeExecution = false;
            settings.CodeExecutionRiskAccepted = false;

            try
            {
                var result = new CodeCommand().Execute(new CommandRequest
                {
                    id = "code-disabled-test",
                    type = "code",
                    @params = new Dictionary<string, object>
                    {
                        { "action", "execute" },
                        { "code", "return 1;" },
                        { "allowExperimental", true }
                    }
                });

                Assert.That(result.success, Is.False);
                Assert.That(result.error, Does.Contain("disabled"));
                Assert.That(result.data, Is.Not.Null);
            }
            finally
            {
                settings.EnableCodeExecution = previousEnabled;
                settings.CodeExecutionRiskAccepted = previousAccepted;
            }
        }

        [Test]
        public void SkillDescriptionDocumentsSafetyGates()
        {
            var description = new CodeCommand().SkillDescription;

            Assert.That(description, Does.Contain("disabled by default"));
            Assert.That(description, Does.Contain("--allow-experimental true"));
            Assert.That(description, Does.Contain(".aibridge/code"));
        }
    }
}
