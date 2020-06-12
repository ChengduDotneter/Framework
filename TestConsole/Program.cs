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



            Parallel.For(0, 1, (index) =>
            {
                int count = 0;
                int time = Environment.TickCount;

                while (true)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        try
                        {
                            TransactionResourceHelper.ApplayResource(typeof(A), index, 0);
                        }
                        catch
                        {

                        }

                        Thread.Sleep(5);

                        try
                        {
                            TransactionResourceHelper.ReleaseResource(typeof(A), index);
                        }
                        catch
                        {

                        }

                        count++;

                        if (Environment.TickCount - time > 1000)
                        {
                            Console.WriteLine(count);
                            time = Environment.TickCount;
                            count = 0;
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(0.01));
                }
            });
        }
    }
}
