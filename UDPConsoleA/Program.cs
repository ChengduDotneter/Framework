using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Common;
using Common.RPC;
using Common.RPC.BufferSerializer;
using Common.RPC.TransferAdapter;

namespace UDPConsoleA
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

    public class TestProcessorA : RequestProcessorBase<TestDataA, TestDataB>
    {
        private readonly ServiceClient serviceClient;

        public TestProcessorA(ServiceClient serviceClient) : base(10000)
        {
            this.serviceClient = serviceClient;
        }

        public void Test(int[] parameter, int index)
        {
            Thread thread = new Thread(() =>
            {
                while (true)
                {
                    TestDataA testDataA = new TestDataA() { Data = Guid.NewGuid().ToString("D") };

                    bool success = RequestAsync(serviceClient, testDataA, testDataB =>
                    {
                        if (testDataA.Data == testDataB.Data)
                        {
                            parameter[index]++;
                            return true;
                        }

                        return false;
                    }).Result;

                    //bool success = Request1(serviceClient, testDataA, testDataB =>
                    //{
                    //    if (testDataA.Data == testDataB.Data)
                    //    {
                    //        parameter[index]++;
                    //        return true;
                    //    }

                    //    return false;
                    //});

                    if (!success)
                    {
                        Console.WriteLine("error");
                    }
                }
            });

            thread.IsBackground = true;
            thread.Start();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            int[] parameter = new int[100];

            for (int i = 0; i < parameter.Length; i++)
            {
                ITransferAdapter transferTesterA = TransferAdapterFactory.CreateUDPCRCTransferAdapter(new IPEndPoint(IPAddress.Parse("192.168.10.200"), 5555), UDPCRCSocketTypeEnum.Client);
                ServiceClient serviceClientA = new ServiceClient(transferTesterA, BufferSerialzerFactory.CreateBinaryBufferSerializer(Encoding.UTF8));
                TestProcessorA testProcessorA = new TestProcessorA(serviceClientA);

                serviceClientA.Start();
                testProcessorA.Test(parameter, i);
            }

            Thread thread = new Thread(() =>
            {
                int time = Environment.TickCount;

                while (true)
                {
                    Console.WriteLine(parameter.Sum() * 1000.0 / (Environment.TickCount - time));
                    Thread.Sleep(1000);
                }
            });

            thread.IsBackground = true;
            thread.Start();

            Console.Read();
        }
    }
}
