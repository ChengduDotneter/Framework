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

            //HashSet<long> ids = new HashSet<long>(1024 * 1024 * 1024);

            //for (int asd = 0; asd < 4; asd++)
            //{
            //    Task.Factory.StartNew(() =>
            //    {
            //        while (true)
            //        {
            //            long id = Common.IDGenerator.NextID();
            //            if (ids.Contains(id))
            //            {
            //                var ld = ids.Last();
            //            }
            //            else
            //            {
            //                ids.Add(id);
            //            }
            //        }
            //    });
            //}

            //Console.Read();
            //return;

            for (int cindex = 0; cindex < 2; cindex++)
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
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(10);



















                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(B), index, 5, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(10);

                                try
                                {
                                   TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch (Exception ex)
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
                                    //wcount = 0;
                                }
                            }

                            Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
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
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(10);






                                try
                                {
                                    if (!TransactionResourceHelper.ApplayResource(typeof(A), index, 0, 10000))
                                    {
                                        wcount++;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"index: {index} Apply Error");
                                }

                                Thread.Sleep(10);

                                try
                                {
                                    TransactionResourceHelper.ReleaseResource(index);
                                }
                                catch (Exception ex)
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
                                    //wcount = 0;
                                }
                            }

                            Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
                        }
                    }
                });
            }

            Console.Read();
        }
    }
}