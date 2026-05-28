using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace AIBridgeCodeIndex
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            try
            {
                return RunAsync(args).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<int> RunAsync(string[] args)
        {
            var options = CodeIndexOptions.Parse(args);
            if (string.IsNullOrWhiteSpace(options.ProjectRoot))
            {
                Console.Error.WriteLine("--project-root is required.");
                return 1;
            }

            options.ProjectRoot = Path.GetFullPath(options.ProjectRoot);
            if (!Directory.Exists(options.ProjectRoot))
            {
                Console.Error.WriteLine("Project root does not exist: " + options.ProjectRoot);
                return 1;
            }

            var server = new CodeIndexServer(options);
            await server.RunAsync();
            return 0;
        }
    }
}
