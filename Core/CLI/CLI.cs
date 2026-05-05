using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TERMINAL_FREQUENCY.Config.Settings;

namespace TERMINAL_FREQUENCY.Core.CLI
{
    public static class CLI
    {
        private static bool _isChild = false;

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
