using System.IO;
using AIBridge.Editor.ScriptExecution;
using AIBridge.Editor.ScriptExecution.Commands;
using NUnit.Framework;

namespace AIBridge.Editor.Tests
{
    public class BatchScriptCommandTests
    {
        [Test]
        public void ParserRecognizesExtendedBatchCommands()
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "aibridge_batch_" + Path.GetRandomFileName() + ".txt");
            File.WriteAllText(
                scriptPath,
                "wait_compile 120000\nwait_playmode playing 30000\nassert_log_empty Error\nassert_object \"Canvas/Button\"\nset_var name value\nprint_var name\n");

            try
            {
                var commands = ScriptParser.Parse(scriptPath);

                Assert.That(commands[0], Is.TypeOf<WaitCompileCommand>());
                Assert.That(commands[1], Is.TypeOf<WaitPlayModeCommand>());
                Assert.That(commands[2], Is.TypeOf<AssertLogEmptyCommand>());
                Assert.That(commands[3], Is.TypeOf<AssertObjectCommand>());
                Assert.That(commands[4], Is.TypeOf<SetVarCommand>());
                Assert.That(commands[5], Is.TypeOf<PrintVarCommand>());
            }
            finally
            {
                if (File.Exists(scriptPath))
                {
                    File.Delete(scriptPath);
                }
            }
        }

        [Test]
        public void VariablesCanBePassedBetweenScriptCommands()
        {
            var context = new ScriptExecutionContext();

            var setResult = new SetVarCommand("token", "ready").Execute(context);
            var printResult = new PrintVarCommand("token").Execute(context);

            Assert.That(setResult.Success, Is.True);
            Assert.That(printResult.Success, Is.True);
            Assert.That(printResult.Message, Is.EqualTo("ready"));
        }
    }
}
