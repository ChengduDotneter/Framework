using System;
using System.Net;
using System.Text;
using Common;
using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;

namespace UDPConsoleB
{
    public struct TestDataA : IRPCData
    {
        public byte MessageID => 0xff;

        public string Data { get; set; }
    }

    public struct TestDataB : IRPCData
    {
        public byte MessageID => 0xfe;

        public string Data { get; set; }
    }

    public class TestProcessorB : ResponseProcessorBase<TestDataA>
    {
        private readonly ServiceClient serviceClient;

        public TestProcessorB(ServiceClient serviceClient) : base(serviceClient)
        {
            this.serviceClient = serviceClient;
        }

        protected override void ProcessData(SessionContext sessionContext, TestDataA data)
        {
            SendSessionData(serviceClient, sessionContext, new TestDataB() { Data = data.Data });
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            ITransferAdapter transferTesterB = TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse("192.168.10.200"), 5555), UDPCRCSocketTypeEnum.Server);
            ServiceClient serviceClientB = new ServiceClient(transferTesterB, BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));
            TestProcessorB testProcessorB = new TestProcessorB(serviceClientB);

            serviceClientB.Start();

            Console.Read();
        }
    }
}
