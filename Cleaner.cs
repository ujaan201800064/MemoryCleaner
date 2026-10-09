using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace MemoryCleaner
{
    internal static class Cleaner
    {
        public static int Run(string[] args)
        {
            // No arguments = clean everything.
            if (args == null || args.Length == 0)
            {
                CleanAll();
                return 0;
            }

            bool success = true;

            foreach (string arg in args)
            {
                if (IsHelp(arg))
                {
                    ShowHelp();
                    continue;
                }

                if (!RunArgument(arg))
                {
                    success = false;
                }
            }

            return success ? 0 : 1;
        }

        private static bool RunArgument(string argument)
        {
            if (String.IsNullOrEmpty(argument))
                return false;

            string option = argument.Trim();

            // Accept /option as well as -option.
            if (option.StartsWith("/") ||
                option.StartsWith("-"))
            {
                option = option.Substring(1);
            }

            switch (option.ToLower())
            {
                case "combinedpagelist":
                case "cpl":
                    return Run(
                        "Combined Page List",
                        CleanCombinedPageList);

                case "modifiedfilecache":
                case "mfc":
                    return Run(
                        "Modified File Cache",
                        CleanModifiedFileCache);

                case "modifiedpagelist":
                case "mpl":
                    return Run(
                        "Modified Page List",
                        CleanModifiedPageList);

                case "registrycache":
                case "rc":
                    return Run(
                        "Registry Cache",
                        CleanRegistryCache);

                case "standbylist":
                case "sl":
                    return Run(
                        "Standby List",
                        CleanStandbyList);

                case "standbylistlowpriority":
                case "slp":
                    return Run(
                        "Standby List (Low Priority)",
                        CleanLowPriorityStandbyList);

                case "systemfilecache":
                case "sc":
                    return Run(
                        "System File Cache",
                        CleanSystemFileCache);

                case "workingset":
                case "ws":
                    return Run(
                        "Working Set",
                        CleanWorkingSet);

                case "all":
                    CleanAll();
                    return true;

                default:
                    Log("[ERROR] Unknown argument: " + argument);
                    return false;
            }
        }

        private static bool IsHelp(string argument)
        {
            if (String.IsNullOrEmpty(argument))
                return false;

            string option = argument.Trim().ToLower();

            return option == "/?" ||
                   option == "-?" ||
                   option == "/help" ||
                   option == "-help";
        }

        private static void ShowHelp()
        {
            Console.WriteLine();
            Console.WriteLine("MemoryCleaner");
            Console.WriteLine();
            Console.WriteLine("No arguments:");
            Console.WriteLine(
                "  Runs all memory cleaning operations.");
            Console.WriteLine();

            Console.WriteLine("Cleaning options:");
            Console.WriteLine(
                "  /CombinedPageList        /cpl");
            Console.WriteLine(
                "  /ModifiedFileCache       /mfc");
            Console.WriteLine(
                "  /ModifiedPageList        /mpl");
            Console.WriteLine(
                "  /RegistryCache           /rc");
            Console.WriteLine(
                "  /StandbyList             /sl");
            Console.WriteLine(
                "  /StandbyListLowPriority  /slp");
            Console.WriteLine(
                "  /SystemFileCache         /sc");
            Console.WriteLine(
                "  /WorkingSet              /ws");
            Console.WriteLine(
                "  /all                     Run all operations");
            Console.WriteLine();

            Console.WriteLine("Help:");
            Console.WriteLine("  /?");
            Console.WriteLine("  /help");
            Console.WriteLine();

            Console.WriteLine("Examples:");
            Console.WriteLine("  MemoryCleaner.exe");
            Console.WriteLine("  MemoryCleaner.exe /ws");
            Console.WriteLine("  MemoryCleaner.exe /ws /sl /mpl");
            Console.WriteLine("  MemoryCleaner.exe /?");
            Console.WriteLine();
        }

        public static void CleanAll()
        {
            Run(
                "Working Set",
                CleanWorkingSet);

            Run(
                "System File Cache",
                CleanSystemFileCache);

            Run(
                "Modified Page List",
                CleanModifiedPageList);

            Run(
                "Standby List",
                CleanStandbyList);

            Run(
                "Standby List (Low Priority)",
                CleanLowPriorityStandbyList);

            Run(
                "Combined Page List",
                CleanCombinedPageList);

            Run(
                "Registry Cache",
                CleanRegistryCache);

            Run(
                "Modified File Cache",
                CleanModifiedFileCache);

            Run(
                "Garbage Collector",
                ReleaseOwnMemory);
        }

        private static bool Run(string name, Action action)
        {
            try
            {
                action();
                Log("[OK] " + name);
                return true;
            }
            catch (Exception ex)
            {
                Log("[FAILED] " + name + ": " + ex.Message);
                return false;
            }
        }

        private const int ERROR_NOT_ALL_ASSIGNED = 1300;

        private static bool EnablePrivilege(string privilegeName)
        {
            using (WindowsIdentity identity =
                   WindowsIdentity.GetCurrent(
                       TokenAccessLevels.Query |
                       TokenAccessLevels.AdjustPrivileges))
            {
                NativeMethods.TokenPrivileges privileges =
                    new NativeMethods.TokenPrivileges();

                privileges.Count = 1;
                privileges.Luid = 0L;
                privileges.Attr =
                    WindowsConstants.PrivilegeEnabled;

                if (!NativeMethods.LookupPrivilegeValue(
                    null,
                    privilegeName,
                    ref privileges.Luid))
                {
                    return false;
                }

                if (!NativeMethods.AdjustTokenPrivileges(
                    identity.Token,
                    false,
                    ref privileges,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero))
                {
                    return false;
                }

                return Marshal.GetLastWin32Error() !=
                       WindowsConstants.ErrorNotAllAssigned;
            }
        }

        private static void CleanWorkingSet()
        {
            if (!EnablePrivilege(
                WindowsConstants.SeProfileSingleProcessPrivilege))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            int command =
                WindowsConstants.MemoryEmptyWorkingSets;

            IntPtr memory =
                Marshal.AllocHGlobal(sizeof(int));

            try
            {
                Marshal.WriteInt32(memory, command);

                int status =
                    NativeMethods.NtSetSystemInformation(
                        WindowsConstants.SystemMemoryListInformation,
                        memory,
                        sizeof(int));

                if (status !=
                    WindowsConstants.ErrorSuccess)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        private static void CleanModifiedPageList()
        {
            if (!EnablePrivilege(
                WindowsConstants.SeProfileSingleProcessPrivilege))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            int command =
                WindowsConstants.MemoryFlushModifiedList;

            IntPtr memory =
                Marshal.AllocHGlobal(sizeof(int));

            try
            {
                Marshal.WriteInt32(memory, command);

                int status =
                    NativeMethods.NtSetSystemInformation(
                        WindowsConstants.SystemMemoryListInformation,
                        memory,
                        sizeof(int));

                if (status !=
                    WindowsConstants.ErrorSuccess)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        private static void CleanStandbyList()
        {
            CleanStandbyList(
                WindowsConstants.MemoryPurgeStandbyList);
        }

        private static void CleanLowPriorityStandbyList()
        {
            CleanStandbyList(
                WindowsConstants.MemoryPurgeLowPriorityStandbyList);
        }

        private static void CleanStandbyList(int command)
        {
            if (!EnablePrivilege(
                WindowsConstants.SeProfileSingleProcessPrivilege))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            IntPtr memory =
                Marshal.AllocHGlobal(sizeof(int));

            try
            {
                Marshal.WriteInt32(memory, command);

                int status =
                    NativeMethods.NtSetSystemInformation(
                        WindowsConstants.SystemMemoryListInformation,
                        memory,
                        sizeof(int));

                if (status !=
                    WindowsConstants.ErrorSuccess)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        private static void CleanCombinedPageList()
        {
            if (!EnablePrivilege(
                WindowsConstants.SeProfileSingleProcessPrivilege))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            MemoryStructs.MemoryCombineInformationEx info =
                new MemoryStructs.MemoryCombineInformationEx();

            int size =
                Marshal.SizeOf(
                    typeof(
                        MemoryStructs.MemoryCombineInformationEx));

            IntPtr memory =
                Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(
                    info,
                    memory,
                    false);

                int status =
                    NativeMethods.NtSetSystemInformation(
                        WindowsConstants
                            .SystemCombinePhysicalMemoryInformation,
                        memory,
                        (uint)size);

                if (status !=
                    WindowsConstants.ErrorSuccess)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }
        }

        private static void CleanRegistryCache()
        {
            int status =
                NativeMethods.NtSetSystemInformation(
                    WindowsConstants
                        .SystemRegistryReconciliationInformation,
                    IntPtr.Zero,
                    0);

            if (status !=
                WindowsConstants.ErrorSuccess)
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }
        }

        private static void CleanSystemFileCache()
        {
            if (!EnablePrivilege(
                WindowsConstants.SeIncreaseQuotaPrivilege))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }

            MemoryStructs.SystemFileCacheInformation64 info =
                new MemoryStructs.SystemFileCacheInformation64();

            info.MinimumWorkingSet = -1;
            info.MaximumWorkingSet = -1;

            int size =
                Marshal.SizeOf(
                    typeof(
                        MemoryStructs.SystemFileCacheInformation64));

            IntPtr memory =
                Marshal.AllocHGlobal(size);

            try
            {
                Marshal.StructureToPtr(
                    info,
                    memory,
                    false);

                int status =
                    NativeMethods.NtSetSystemInformation(
                        WindowsConstants.SystemFileCacheInformation,
                        memory,
                        (uint)size);

                if (status !=
                    WindowsConstants.ErrorSuccess)
                {
                    throw new Win32Exception(
                        Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            IntPtr flushValue = new IntPtr(-1);

            if (!NativeMethods.SetSystemFileCacheSize(
                flushValue,
                flushValue,
                0))
            {
                throw new Win32Exception(
                    Marshal.GetLastWin32Error());
            }
        }

        private static void CleanModifiedFileCache()
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive == null ||
                    drive.DriveType != DriveType.Fixed ||
                    String.IsNullOrEmpty(drive.Name))
                {
                    continue;
                }

                using (SafeFileHandle handle =
                       OpenVolumeHandle(drive.Name))
                {
                    if (handle == null || handle.IsInvalid)
                        continue;

                    int bytesReturned;

                    try
                    {
                        IntPtr buffer =
                            Marshal.AllocHGlobal(1);

                        try
                        {
                            Marshal.WriteByte(buffer, 0);

                            NativeMethods.DeviceIoControl(
                                handle,
                                WindowsConstants
                                    .IoControlResetWriteOrder,
                                buffer,
                                1,
                                IntPtr.Zero,
                                0,
                                out bytesReturned,
                                IntPtr.Zero);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(buffer);
                        }
                    }
                    catch
                    {
                        // Best effort.
                    }

                    try
                    {
                        NativeMethods.DeviceIoControl(
                            handle,
                            WindowsConstants
                                .FsctlDiscardVolumeCache,
                            IntPtr.Zero,
                            0,
                            IntPtr.Zero,
                            0,
                            out bytesReturned,
                            IntPtr.Zero);
                    }
                    catch
                    {
                        // Best effort.
                    }

                    if (!NativeMethods.FlushFileBuffers(handle))
                    {
                        throw new Win32Exception(
                            Marshal.GetLastWin32Error());
                    }
                }
            }
        }

        private static SafeFileHandle OpenVolumeHandle(
            string driveName)
        {
            return NativeMethods.CreateFile(
                @"\\.\" +
                driveName.TrimEnd(':', '\\') +
                ":",
                FileAccess.ReadWrite,
                FileShare.Read | FileShare.Write,
                IntPtr.Zero,
                FileMode.Open,
                (int)FileAttributes.Normal |
                WindowsConstants.FileFlagsNoBuffering,
                IntPtr.Zero);
        }

        private static void ReleaseOwnMemory()
        {
            try
            {
                GC.Collect(
                    GC.MaxGeneration,
                    GCCollectionMode.Forced);

                GC.WaitForPendingFinalizers();

                GC.Collect(
                    GC.MaxGeneration,
                    GCCollectionMode.Forced);
            }
            catch
            {
                // Best effort.
            }

            try
            {
                NativeMethods.EmptyWorkingSet(
                    Process.GetCurrentProcess().Handle);
            }
            catch
            {
                // Best effort.
            }
        }

        private static void Log(string message)
        {
            try
            {
                string directory =
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.CommonApplicationData),
                        "MemoryCleaner");

                Directory.CreateDirectory(directory);

                string file =
                    Path.Combine(
                        directory,
                        "MemoryCleaner.log");

                File.AppendAllText(
                    file,
                    DateTime.Now.ToString(
                        "yyyy-MM-dd HH:mm:ss") +
                    " " +
                    message +
                    Environment.NewLine);
            }
            catch
            {
                // Never allow logging failure to break cleaning.
            }
        }
    }
}