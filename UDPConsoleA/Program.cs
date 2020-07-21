using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.ServiceCommon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UDPConsoleA
{
    public class TestData : IEntity
    {
        [QuerySqlField]
        public long ID { get; set; }

        [QuerySqlField]
        public string Name { get; set; }

        [QuerySqlField]
        public string Data { get; set; }
    }

    public class TestService : IHostedService
    {
        private readonly ISearchQuery<TestData> m_searchQuery;
        private readonly IEditQuery<TestData> m_editQuery;

        public TestService(ISearchQuery<TestData> searchQuery, IEditQuery<TestData> editQuery)
        {
            m_searchQuery = searchQuery;
            m_editQuery = editQuery;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            int[] parameter = new int[100];
            TestTaskScheduler testTaskScheduler = new TestTaskScheduler();

            int maxtime = 0;

            for (int i = 0; i < parameter.Length; i++)
            {
                Task.Factory.StartNew((state) =>
                {
                    int index = (int)state;

                    while (true)
                    {
                        try
                        {
                            using (ITransaction transaction = m_editQuery.BeginTransaction())
                            {
                                int time = Environment.TickCount;

                                int count = m_searchQuery.Count();

                                m_editQuery.Insert(new TestData()
                                {
                                    ID = IDGenerator.NextID(),
                                    Data = Guid.NewGuid().ToString(),
                                    Name = index.ToString()
                                });

                                parameter[index]++;
                                transaction.Submit();

                                maxtime = Math.Max(maxtime, Environment.TickCount - time);

                                //Thread.Sleep(10000);
                            }
                        }
                        catch
                        {
                            Console.WriteLine("error " + index);
                        }

                        Thread.Sleep(TimeSpan.FromMilliseconds(0.01));
                    }
                }, i, cancellationToken, TaskCreationOptions.None, testTaskScheduler);
            }

            Thread thread = new Thread(() =>
            {
                int time = Environment.TickCount;

                while (true)
                {
                    Console.WriteLine(parameter.Sum() * 1000.0 / (Environment.TickCount - time));
                    Console.WriteLine("transaction_time: " + maxtime);
                    Thread.Sleep(1000);
                }
            });

            thread.IsBackground = true;
            thread.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    public class TestTaskScheduler : TaskScheduler
    {
        private BlockingCollection<Task> m_tasks;
        private Thread m_doWorkThread;

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return m_tasks;
        }

        protected override void QueueTask(Task task)
        {
            m_tasks.Add(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        public TestTaskScheduler()
        {
            m_tasks = new BlockingCollection<Task>();
            m_doWorkThread = new Thread(DoWork);
            m_doWorkThread.IsBackground = true;
            m_doWorkThread.Start();
        }

        private void DoWork()
        {
            while (true)
            {
                if (m_tasks.TryTake(out Task task))
                {
                    Thread thread = new Thread(() => TryExecuteTask(task));
                    thread.IsBackground = true;
                    thread.Start();
                }
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            new HostBuilder().
            ConfigureServices((context, services) =>
            {
                services.Configure<ConsoleLifetimeOptions>(options =>
                {
                    options.SuppressStatusMessages = true;
                });

                services.AddQuerys(
                TypeReflector.ReflectType((type) =>
                {
                    if (type.GetInterface(typeof(IEntity).FullName) == null || type.IsInterface || type.IsAbstract)
                        return false;

                    if (type.GetCustomAttribute<IgnoreTableAttribute>() != null)
                        return false;

                    return true;
                }),
                (type) =>
                {
                    return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetSearchIgniteQuery)).MakeGenericMethod(type).Invoke(null, null);
                },
                (type) =>
                {
                    return typeof(DaoFactory).GetMethod(nameof(DaoFactory.GetEditIgniteQuery)).MakeGenericMethod(type).Invoke(null, null);
                });

                context.ConfigInit();
                services.AddHostedService<TestService>();
            }).
            ConfigureLogging(builder =>
            {
                builder.AddConsole();
            }).RunConsoleAsync().Wait();

            Console.WriteLine("work done..");
            Console.Read();
        }
    }
}
