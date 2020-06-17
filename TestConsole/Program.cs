﻿using Common;
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
                            catch (Exception ex)
                            {
                                Console.WriteLine($"index: {index} Apply Error");
                            }

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
                                time = Environment.TickCount;
                                count = 0;
                            }
                        }

                        Thread.Sleep(TimeSpan.FromMilliseconds(0.01));
                    }
                });
            }

            Console.Read();
        }
    }
}