using System;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.DAL.Transaction;

namespace TestConsole
{
    class A
    {

    }

    class B
    {

    }

    class Program
    {
        static void Main(string[] args)
        {
            ConfigManager.Init("Development");



            for (int cindex = 0; cindex < 1; cindex++)
            {
                int index = cindex;

                Task.Factory.StartNew(() =>
                {
                    int count = 0;
                    int time = Environment.TickCount;

                    while (true)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            try
                            {
                                TransactionResourceHelper.ApplayResource(typeof(A), index, 0);
                            }
                            catch
                            {
                                Console.WriteLine($"index: {index} Apply Error");
                            }

                            //Thread.Sleep(5);

                            try
                            {
                                TransactionResourceHelper.ReleaseResource(typeof(A), index);
                            }
                            catch
                            {
                                Console.WriteLine($"index: {index} Release Error");
                            }

                            count++;

                            if (Environment.TickCount - time > 1000)
                            {
                                Console.WriteLine($"index: {index}, count: {count}");
                                time = Environment.TickCount;
                                count = 0;
                            }
                        }

                        Thread.Sleep(TimeSpan.FromMilliseconds(10));
                    }
                });
            }

            Console.Read();
        }
    }
}
