
using System.Diagnostics;

using TERMINAL_FREQUENCY.Config.Settings;

namespace TERMINAL_FREQUENCY.Core.CLI
{
    /// <summary>
    /// Static class to handle command line arguments.
    /// </summary>
    /// <remarks>Not implemented</remarks>
    public static class CLI
    {
        private static bool _isChild = false;

        /// <summary>
        /// Main method to process CLI arguments.
        /// </summary>
        /// <param name="args">CLI arguments passed from Program.Main</param>
        /// <param name="settings">Global settings instance</param>
        public static void HandleCliArgs(string[] args, GlobalSettings settings)
        {
            _isChild = args.Length > 0 && args[0] == "--child";
            if (!_isChild)
            {
                for (int i = 1; i < settings.ConsoleInstances; i++)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = Environment.ProcessPath,
                        Arguments = "--child",
                        UseShellExecute = true,
                        CreateNoWindow = false
                    });
                    Thread.Sleep(300);
                }
            }
        }

        private static void LaunchChildProcess()
        {
            
        }
    }
}
