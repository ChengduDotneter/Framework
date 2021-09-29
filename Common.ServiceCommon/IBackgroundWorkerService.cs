using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Microsoft.Extensions.DependencyInjection;
using Nito.AsyncEx;

namespace Common.ServiceCommon
{
    public interface IBackgroundWorkerService
    {
        Task AddWork(Func<Task> work);
        Task AddWork<T1>(Func<T1, Task> work, object workArguments = null);
        Task AddWork<T1, T2>(Func<T1, T2, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3>(Func<T1, T2, T3, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Task> work, object workArguments = null);
        Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, Task> work, object workArguments = null);
    }

    public class BackgroundWorkerService : IBackgroundWorkerService
    {
        private class WorkItem
        {
            public Delegate WorkHandler { get; }
            public object WorkArguments { get; }
            public Type[] WorkArgumentTypes { get; }

            public WorkItem(Delegate workHandler, object workArguments, Type[] workArgumentTypes)
            {
                WorkHandler = workHandler;
                WorkArguments = workArguments;
                WorkArgumentTypes = workArgumentTypes;
            }
        }

        private IServiceProvider m_serviceProvider;
        private ILogHelper m_logHelper;
        private readonly Queue<WorkItem> m_workItems;
        private readonly AsyncLock m_mutex;
        private const int THREAD_TIME_SPAN = 1;
        private bool m_running;

        public BackgroundWorkerService(IServiceProvider serviceProvider, ILogHelper logHelper)
        {
            m_serviceProvider = serviceProvider;
            m_logHelper = logHelper;
            m_running = true;
            m_workItems = new Queue<WorkItem>();
            m_mutex = new AsyncLock();

            for (int i = 0; i < Environment.ProcessorCount - 1; i++)
            {
                new Thread(DoWork)
                {
                    IsBackground = true,
                    Name = $"WORK_THREAD_{i + 1}"
                }.Start();
            }

            AppDomain.CurrentDomain.ProcessExit += ((_, _) => m_running = false);
        }

        public Task AddWork(Func<Task> work)
        {
            return AddWork(work, null, new Type[0]);
        }

        public Task AddWork<T1>(Func<T1, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1)
            });
        }

        public Task AddWork<T1, T2>(Func<T1, T2, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2)
            });
        }

        public Task AddWork<T1, T2, T3>(Func<T1, T2, T3, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3)
            });
        }

        public Task AddWork<T1, T2, T3, T4>(Func<T1, T2, T3, T4, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5>(Func<T1, T2, T3, T4, T5, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6>(Func<T1, T2, T3, T4, T5, T6, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7>(Func<T1, T2, T3, T4, T5, T6, T7, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8>(Func<T1, T2, T3, T4, T5, T6, T7, T8, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15)
            });
        }

        public Task AddWork<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, Task> work, object workArguments = null)
        {
            return AddWork(work, workArguments, new Type[]
            {
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7), typeof(T8), typeof(T9), typeof(T10), typeof(T11), typeof(T12), typeof(T13), typeof(T14), typeof(T15), typeof(T16)
            });
        }

        private async Task AddWork(Delegate workHandler, object wrokArguments, Type[] workArgumentTypes)
        {
            using (await m_mutex.LockAsync())
                m_workItems.Enqueue(new WorkItem(workHandler, wrokArguments, workArgumentTypes));
        }

        private void DoWork()
        {
            while (m_running)
            {
                WorkItem workItem = null;

                using (m_mutex.Lock())
                {
                    if (!m_workItems.IsNullOrEmpty())
                        workItem = m_workItems.Dequeue();
                }

                if (workItem != null)
                {
                    using (IServiceScope scope = m_serviceProvider.CreateScope())
                    {
                        object[] workArguments = new object [workItem.WorkArgumentTypes.Length];
                        object[] arguments = GetArguments(workItem.WorkArguments);

                        if (!workArguments.IsNullOrEmpty())
                        {
                            for (int i = 0; i < arguments.Length; i++)
                            {
                                if (arguments[i].GetType() != workItem.WorkArgumentTypes[i])
                                    m_logHelper.Error(nameof(BackgroundWorkerService), $"无法运行Work，传入参数arguments与实际构造参数类型不匹配，请考虑将arguments包含的参数放在构造函数最前面。").Wait();

                                workArguments[i] = arguments[i];
                            }
                        }

                        for (int i = arguments.IsNullOrEmpty() ? 0 : arguments.Length; i < workArguments.Length; i++)
                            workArguments[i] = scope.ServiceProvider.GetRequiredService(workItem.WorkArgumentTypes[i]);

                        try
                        {
                            Task task = (Task)workItem.WorkHandler.DynamicInvoke(workArguments);
                            task.Wait();
                        }
                        catch (Exception exception)
                        {
                            m_logHelper.Error(nameof(BackgroundWorkerService), ExceptionHelper.GetMessageAndStackTrace(exception)).Wait();
                        }
                    }
                }

                Thread.Sleep(THREAD_TIME_SPAN);
            }
        }

        private object[] GetArguments(object workArguments)
        {
            IList<object> arguments = new List<object>();

            foreach (PropertyInfo propertyInfo in workArguments.GetType().GetProperties())
                arguments.Add(propertyInfo.GetValue(workArguments));

            return arguments.ToArray();
        }
    }
}