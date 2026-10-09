using System;
using System.Runtime.InteropServices;

namespace MemoryCleaner
{
    internal static class MemoryStructs
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MemoryCombineInformationEx
        {
            internal IntPtr Handle;
            internal IntPtr PagesCombined;
            internal long Flags;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SystemFileCacheInformation64
        {
            internal long CurrentSize;
            internal long PeakSize;
            internal long PageFaultCount;
            internal long MinimumWorkingSet;
            internal long MaximumWorkingSet;
            internal long CurrentSizeIncludingTransitionInPages;
            internal long PeakSizeIncludingTransitionInPages;
            internal long TransitionRePurposeCount;
            internal long Flags;
        }
    }
}