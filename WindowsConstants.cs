namespace MemoryCleaner
{
    internal static class WindowsConstants
    {
        internal const int SystemCombinePhysicalMemoryInformation = 130;
        internal const int SystemFileCacheInformation = 21;
        internal const int SystemMemoryListInformation = 80;
        internal const int SystemRegistryReconciliationInformation = 155;

        internal const int MemoryEmptyWorkingSets = 2;
        internal const int MemoryFlushModifiedList = 3;
        internal const int MemoryPurgeStandbyList = 4;
        internal const int MemoryPurgeLowPriorityStandbyList = 5;

        internal const int ErrorSuccess = 0;
        internal const int ErrorAccessDenied = 5;

        internal const int ErrorNotAllAssigned = 1300;
        internal const int PrivilegeEnabled = 2;

        internal const string SeDebugPrivilege =
            "SeDebugPrivilege";

        internal const string SeIncreaseQuotaPrivilege =
            "SeIncreaseQuotaPrivilege";

        internal const string SeProfileSingleProcessPrivilege =
            "SeProfileSingleProcessPrivilege";

        internal const int FileFlagsNoBuffering =
            0x20000000;

        internal const int FsctlDiscardVolumeCache =
            0x00090054;

        internal const int IoControlResetWriteOrder =
            0x000900F8;
    }
}