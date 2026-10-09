using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;

namespace MemoryCleaner
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                // Automatically request elevation when necessary.
                if (!HasAdministrativePrivileges())
                {
                    return RestartAsAdministrator(args);
                }

                // Already elevated: execute the requested operations.
                return Cleaner.Run(args);
            }
            catch (Exception ex)
            {
                LogError(ex);
                return 1;
            }
        }

        private static bool HasAdministrativePrivileges()
        {
            using (WindowsIdentity identity =
                   WindowsIdentity.GetCurrent())
            {
                // SYSTEM is already a privileged account.
                if (identity.User != null &&
                    identity.User.Value == "S-1-5-18")
                {
                    return true;
                }

                WindowsPrincipal principal =
                    new WindowsPrincipal(identity);

                return principal.IsInRole(
                    WindowsBuiltInRole.Administrator);
            }
        }

        private static int RestartAsAdministrator(string[] args)
        {
            try
            {
                ProcessStartInfo startInfo =
                    new ProcessStartInfo();

                startInfo.FileName =
                    Assembly.GetExecutingAssembly().Location;

                startInfo.Arguments = BuildArguments(args);

                startInfo.WorkingDirectory =
                    AppDomain.CurrentDomain.BaseDirectory;

                startInfo.UseShellExecute = true;
                startInfo.Verb = "runas";

                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        LogMessage(
                            "ERROR: Could not start elevated process.");

                        return 1;
                    }

                    process.WaitForExit();

                    return process.ExitCode;
                }
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode == 1223)
                {
                    LogMessage(
                        "ERROR: Administrator elevation was cancelled.");
                }
                else
                {
                    LogError(ex);
                }

                return 1;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return 1;
            }
        }

        private static string BuildArguments(string[] args)
        {
            StringBuilder result = new StringBuilder();

            if (args == null)
                return String.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                if (i > 0)
                    result.Append(' ');

                result.Append(QuoteArgument(args[i]));
            }

            return result.ToString();
        }

        private static string QuoteArgument(string argument)
        {
            if (argument == null)
                return "\"\"";

            StringBuilder result = new StringBuilder();
            result.Append('"');

            int backslashes = 0;

            foreach (char c in argument)
            {
                if (c == '\\')
                {
                    backslashes++;
                }
                else if (c == '"')
                {
                    result.Append('\\', backslashes * 2 + 1);
                    result.Append('"');
                    backslashes = 0;
                }
                else
                {
                    result.Append('\\', backslashes);
                    backslashes = 0;
                    result.Append(c);
                }
            }

            result.Append('\\', backslashes * 2);
            result.Append('"');

            return result.ToString();
        }

        private static void LogError(Exception ex)
        {
            LogMessage("ERROR: " + ex.ToString());
        }

        private static void LogMessage(string message)
        {
            try
            {
                string directory = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.CommonApplicationData),
                    "MemoryCleaner");

                Directory.CreateDirectory(directory);

                string logFile = Path.Combine(
                    directory,
                    "MemoryCleaner.log");

                File.AppendAllText(
                    logFile,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                    " " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
                // Logging failures must not crash the application.
            }
        }
    }
}