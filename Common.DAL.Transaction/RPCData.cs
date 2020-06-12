using Common.RPC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Common.DAL.Transaction
{
    public struct ApplyRequestData : IRPCData
    {
        public string ResourceName { get; set; }
        public long Identity { get; set; }
        public int Weight { get; set; }
        public int TimeOut { get; set; }

        public byte MessageID { get { return 0x1; } }
    }

    public struct ApplyResponseData : IRPCData
    {
        public bool Success { get; set; }
        public byte MessageID { get { return 0x2; } }
    }

    public struct ReleaseRequestData : IRPCData
    {
        public string ResourceName { get; set; }
        public long Identity { get; set; }
        public byte MessageID { get { return 0x3; } }
    }

    public struct ReleaseResponseData : IRPCData
    {
        public byte MessageID { get { return 0x4; } }
    }
}
