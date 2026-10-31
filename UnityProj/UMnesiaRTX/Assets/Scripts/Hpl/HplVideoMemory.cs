using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace RTXMnesia.Hpl
{
    /// <summary>
    /// Video memory of this process as Windows accounts it (DXGI IDXGIAdapter3.QueryVideoMemoryInfo): usage of the
    /// card's own memory, of system memory used by the GPU, and the budget the OS gives the process. Past the budget
    /// the driver moves data to system memory and the frame rate collapses. Unity reports no such figure.
    /// The adapter is the one Unity renders on (vendor and device id). Without DXGI (another OS, failure): unavailable.
    /// </summary>
    public static class HplVideoMemory
    {
        public struct Info
        {
            /// <summary>Bytes. Local: the card's own memory; shared: system memory used by the GPU.</summary>
            public long localUsage, localBudget, sharedUsage, dedicated;
        }

        static IntPtr s_adapter;
        static bool s_tried;
        static long s_dedicated;
        static QueryVideoMemoryInfoFn s_query;

        public static bool Available => Adapter() != IntPtr.Zero;
        /// <summary>The card's own memory (DXGI DedicatedVideoMemory), 0 if unknown.</summary>
        public static long Dedicated => Available ? s_dedicated : 0;

        public static bool TryQuery(out Info info)
        {
            info = default;
            var adapter = Adapter();
            if (adapter == IntPtr.Zero) return false;
            try
            {
                if (s_query(adapter, 0, 0, out var local) != 0 || s_query(adapter, 0, 1, out var shared) != 0) return false;
                info = new Info { localUsage = (long)local.currentUsage, localBudget = (long)local.budget, sharedUsage = (long)shared.currentUsage, dedicated = s_dedicated };
                return true;
            }
            catch (Exception) { return false; }
        }

        static IntPtr Adapter()
        {
            if (s_tried) return s_adapter;
            s_tried = true;
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor) return IntPtr.Zero;
            IntPtr factory = IntPtr.Zero;
            try
            {
                var factoryId = new Guid("770aae78-f26f-4dba-a829-253c83d1b387");
                if (CreateDXGIFactory1(ref factoryId, out factory) != 0 || factory == IntPtr.Zero) return IntPtr.Zero;
                var enumAdapters = Method<EnumAdapters1Fn>(factory, 12);
                IntPtr best = IntPtr.Zero;
                long bestMemory = -1;
                bool bestMatches = false;
                for (uint i = 0; enumAdapters(factory, i, out var adapter) == 0; i++)
                {
                    var desc = new AdapterDesc1();
                    Method<GetDesc1Fn>(adapter, 10)(adapter, ref desc);
                    long memory = (long)desc.dedicatedVideoMemory;
                    bool software = (desc.flags & 2) != 0;
                    bool matches = desc.vendorId == SystemInfo.graphicsDeviceVendorID && desc.deviceId == SystemInfo.graphicsDeviceID;
                    // Prefer Unity's own device; otherwise the hardware adapter with the most memory.
                    if (!software && (matches && !bestMatches || matches == bestMatches && memory > bestMemory))
                    {
                        if (best != IntPtr.Zero) Release(best);
                        best = adapter; bestMemory = memory; bestMatches = matches;
                    }
                    else Release(adapter);
                }
                if (best == IntPtr.Zero) return IntPtr.Zero;
                var adapter3Id = new Guid("645967a4-1392-4310-a798-8053ce3e93fd");
                int hr = Method<QueryInterfaceFn>(best, 0)(best, ref adapter3Id, out var adapter3);
                Release(best);
                if (hr != 0 || adapter3 == IntPtr.Zero) return IntPtr.Zero;
                s_query = Method<QueryVideoMemoryInfoFn>(adapter3, 14);
                s_dedicated = bestMemory;
                // Kept for the lifetime of the process.
                s_adapter = adapter3;
            }
            catch (Exception e) { Debug.LogWarning("[HplVideoMemory] DXGI unavailable: " + e.Message); s_adapter = IntPtr.Zero; }
            finally { if (factory != IntPtr.Zero) Release(factory); }
            return s_adapter;
        }

        static T Method<T>(IntPtr com, int slot) where T : Delegate =>
            Marshal.GetDelegateForFunctionPointer<T>(Marshal.ReadIntPtr(Marshal.ReadIntPtr(com), slot * IntPtr.Size));

        static void Release(IntPtr com) => Method<ReleaseFn>(com, 2)(com);

        [DllImport("dxgi.dll")] static extern int CreateDXGIFactory1(ref Guid riid, out IntPtr factory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int QueryInterfaceFn(IntPtr self, ref Guid riid, out IntPtr result);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint ReleaseFn(IntPtr self);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int EnumAdapters1Fn(IntPtr self, uint index, out IntPtr adapter);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int GetDesc1Fn(IntPtr self, ref AdapterDesc1 desc);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int QueryVideoMemoryInfoFn(IntPtr self, uint node, int group, out MemoryInfo info);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct AdapterDesc1
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string description;
            public uint vendorId, deviceId, subSysId, revision;
            public UIntPtr dedicatedVideoMemory, dedicatedSystemMemory, sharedSystemMemory;
            public uint luidLow; public int luidHigh;
            public uint flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MemoryInfo { public ulong budget, currentUsage, availableForReservation, currentReservation; }
    }
}
