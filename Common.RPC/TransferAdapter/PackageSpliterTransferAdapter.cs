using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace Common.RPC.TransferAdapter
{
    internal class PackageSpliterTransferAdapter : ITransferAdapter, IDisposable
    {
        public event OnBufferRecievedHandler OnBufferRecieved;

        private const int SPLIT_PACKAGE_LENGTH = 1024; //1kb
        private const int CLEAR_TIME_SPAN = 1000;
        private const int CLEAR_TIME_OUT = 1000 * 30;
        private ConcurrentDictionary<long, PackageDataCache> m_packageDataCaches;
        private ITransferAdapter m_transferAdapter;
        private Thread m_clearThread;
        private static readonly int m_size;

        private class PackageDataCache
        {
            public byte[] Buffer { get; }
            public ISet<int> IndexSet { get; }
            public int LastRefreshTime { get; private set; }

            public void RefreshTime()
            {
                LastRefreshTime = Environment.TickCount;
            }

            public PackageDataCache(int bufferLength)
            {
                Buffer = new byte[bufferLength];
                IndexSet = new HashSet<int>();
                RefreshTime();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PackageData
        {
            public long PackageID;
            public int PackageIndex;
            public int PackageCount;
            public int TotalLength;
            public int Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SPLIT_PACKAGE_LENGTH)]
            public byte[] Buffer;
        }

        static PackageSpliterTransferAdapter()
        {
            m_size = Marshal.SizeOf<PackageData>();
        }

        public PackageSpliterTransferAdapter(ITransferAdapter transferAdapter)
        {
            m_transferAdapter = transferAdapter;
            m_transferAdapter.OnBufferRecieved += DoRecieve;
            m_packageDataCaches = new ConcurrentDictionary<long, PackageDataCache>();
            m_clearThread = new Thread(DoClear);
            m_clearThread.IsBackground = true;
            m_clearThread.Name = "PACKAGE_SPLITER_THREAD";
        }

        public void SendBuffer(SessionContext sessionContext, byte[] buffer, int length)
        {
            int index = 0;
            int packageCount = length % SPLIT_PACKAGE_LENGTH != 0 ? length / SPLIT_PACKAGE_LENGTH + 1 : length / SPLIT_PACKAGE_LENGTH;

            do
            {
                int offset = index * SPLIT_PACKAGE_LENGTH;
                PackageData packageData = new PackageData();
                packageData.PackageID = sessionContext.SessionID;
                packageData.PackageIndex = index;
                packageData.PackageCount = packageCount;
                packageData.TotalLength = length;
                packageData.Length = length > (index + 1) * SPLIT_PACKAGE_LENGTH ? SPLIT_PACKAGE_LENGTH : length - offset;
                packageData.Buffer = new byte[SPLIT_PACKAGE_LENGTH];

                unsafe
                {
                    fixed (byte* packageBufferPtr = packageData.Buffer)
                    fixed (byte* bufferPtr = buffer)
                        Buffer.MemoryCopy(bufferPtr + offset, packageBufferPtr, packageData.Buffer.Length, packageData.Length);
                }

                DoSend(sessionContext, packageData);
                index++;
            }
            while (index * SPLIT_PACKAGE_LENGTH < length);
        }

        private void DoRecieve(SessionContext sessionContext, byte[] buffer)
        {
            IntPtr structPtr = Marshal.AllocHGlobal(m_size);

            try
            {
                Marshal.Copy(buffer, 0, structPtr, m_size);
                PackageData packageData = Marshal.PtrToStructure<PackageData>(structPtr);

                if (!m_packageDataCaches.ContainsKey(packageData.PackageID))
                    m_packageDataCaches.TryAdd(packageData.PackageID, new PackageDataCache(packageData.TotalLength));

                if (m_packageDataCaches.TryGetValue(packageData.PackageID, out PackageDataCache packageDataCache))
                {
                    packageDataCache.RefreshTime();

                    unsafe
                    {
                        fixed (byte* dataPtr = packageDataCache.Buffer)
                        fixed (byte* bufferPtr = packageData.Buffer)
                            Buffer.MemoryCopy(bufferPtr, dataPtr + packageData.PackageIndex * SPLIT_PACKAGE_LENGTH, packageData.TotalLength, packageData.Length);
                    }

                    packageDataCache.IndexSet.Add(packageData.PackageIndex);

                    if (packageDataCache.IndexSet.Count == packageData.PackageCount)
                    {
                        m_packageDataCaches.TryRemove(packageData.PackageID, out PackageDataCache removePackageDataCache);
                        OnBufferRecieved?.Invoke(sessionContext, packageDataCache.Buffer);
                    }
                }
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }
        }

        private void DoSend(SessionContext sessionContext, PackageData packageData)
        {
            byte[] bytes = new byte[m_size];
            IntPtr structPtr = Marshal.AllocHGlobal(m_size);

            try
            {
                Marshal.StructureToPtr(packageData, structPtr, false);
                Marshal.Copy(structPtr, bytes, 0, m_size);
                m_transferAdapter.SendBuffer(sessionContext, bytes, m_size);
            }
            finally
            {
                Marshal.FreeHGlobal(structPtr);
            }
        }

        private void DoClear()
        {
            while (true)
            {
                if (!m_packageDataCaches.IsEmpty)
                {
                    long[] sessionIDs = m_packageDataCaches.Keys.ToArray();

                    for (int i = 0; i < sessionIDs.Length; i++)
                    {
                        if (m_packageDataCaches.TryGetValue(sessionIDs[i], out PackageDataCache packageDataCache) && Environment.TickCount - packageDataCache.LastRefreshTime > CLEAR_TIME_OUT)
                            m_packageDataCaches.TryRemove(sessionIDs[i], out PackageDataCache removePackageDataCache);
                    }
                }

                Thread.Sleep(CLEAR_TIME_SPAN);
            }
        }

        public void Strat()
        {
            m_transferAdapter.Strat();
            m_clearThread.Start();
        }

        public void Dispose()
        {
            if (m_transferAdapter is IDisposable)
                ((IDisposable)m_transferAdapter).Dispose();
        }
    }
}