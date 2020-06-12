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
                int time = Environment.TickCount;
                int count = 0;

                while (true)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        TransactionResourceHelper.ApplayResource(typeof(A), index, 0);
                        TransactionResourceHelper.ReleaseResource(typeof(A), index);

                        count++;

                        if (Environment.TickCount - time > 1000)
                        {
                            Console.WriteLine(count);
                            time = Environment.TickCount;
                            count = 0;
                        }
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(0.1));
                }
            });
        }
    }
}
