using Common;
using Common.DAL.Transaction;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestConsole
{
    internal class A
    {
    }

    internal class B
    {
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            ConfigManager.Init("Development");

            for (int cindex = 0; cindex < 4; cindex++)
            {
                int index = cindex;

                Task.Factory.StartNew(() =>
                {
                    int count = 0;
                    int wcount = 0;
                    int time = Environment.TickCount;

                    if (index % 2 == 0)
                    {
                        while (true)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(A), index, 5, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(B), index, 5, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Release Error");
                                }

                                count++;

                                if (Environment.TickCount - time > 1000)
                                {
                                    Console.WriteLine($"index: {index}, count: {count}");
                                    Console.WriteLine($"index: {index}, wcount: {wcount}");
                                    time = Environment.TickCount;
                                    count = 0;
                                }
                            }

                            Thread.Sleep(1);
                        }
                    }
                    else
                    {
                        while (true)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(B), index, 0, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(A), index, 0, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(1);

                                try
                                {
                                    TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch
                                {
                                    Console.WriteLine($"index: {index} Release Error");
                                }

                                count++;

                                if (Environment.TickCount - time > 1000)
                                {
                                    Console.WriteLine($"index: {index}, count: {count}");
                                    Console.WriteLine($"index: {index}, wcount: {wcount}");
                                    time = Environment.TickCount;
                                    count = 0;
                                }
                            }

                            Thread.Sleep(1);
                        }
                    }
                });
            }

            Console.Read();
        }
    }
}