//using Common.DAL.Transaction;
//using Microsoft.Extensions.Hosting;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;

//namespace ResourceManager
//{
//    class Test : IHostedService
//    {
//        IResourceManage resourceManage;

//        public Test(IResourceManage resourceManage)
//        {
//            this.resourceManage = resourceManage;
//        }

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            Task.Factory.StartNew(async () =>
//            {
//                int time = Environment.TickCount;
//                int count = 0;

//                while (true)
//                {
//                    bool ret = await resourceManage.GetResource("A").Apply(1, 1, 10000);
//                    Thread.Sleep(1);


//                    if (!ret)
//                        continue;


                    



//                    ret = await resourceManage.GetResource("B").Apply(1, 1, 10000);
//                    Thread.Sleep(1);


//                    if (!ret)
//                        continue;


//                    await ResourceDetection.EnQueued(new EnQueueData2() { Identity = 1, QueueDataType = QueueDataTypeEnum2.Release });







//                    count++;

//                    if (Environment.TickCount - time > 1000)
//                    {
//                        Console.WriteLine("a " + count);
//                        count = 0;
//                        time = Environment.TickCount;
//                    }
//                }
//            });

//            Task.Factory.StartNew(async () =>
//            {
//                int time = Environment.TickCount;
//                int count = 0;

//                while (true)
//                {
//                    bool ret = await resourceManage.GetResource("B").Apply(0, 0, 10000);
//                    Thread.Sleep(1);




//                    if (!ret)
//                        continue;



                    


//                    ret = await resourceManage.GetResource("A").Apply(0, 0, 10000);
//                    Thread.Sleep(1);




//                    if (!ret)
//                        continue;


//                    await ResourceDetection.EnQueued(new EnQueueData2() { Identity = 0, QueueDataType = QueueDataTypeEnum2.Release });




//                    count++;

//                    if (Environment.TickCount - time > 1000)
//                    {
//                        Console.WriteLine("b " + count);
//                        count = 0;
//                        time = Environment.TickCount;
//                    }
//                }
//            });

//            return Task.CompletedTask;
//        }

//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            return Task.CompletedTask;
//        }
//    }
//}
