using System;
using Apache.Ignite.Core.Cache.Configuration;
using Common;
using Common.DAL;
using Common.Model;

namespace TestConsole
{
    internal class TestModelA : IEntity
    {
        [QuerySqlField(IsIndexed = true)]
        public long ID { get; set; }

        [QuerySqlField(IsIndexed = true)]
        public string Name { get; set; }

        public int Value { get; set; }
    }

    internal class TestModelB : IEntity
    {
        [QuerySqlField(IsIndexed = true)]
        public long ID { get; set; }

        [QuerySqlField(IsIndexed = true)]
        public string Name { get; set; }

        public int Value { get; set; }
    }

    internal class Program
    {
        private static unsafe void Main(string[] _)
        {
            ConfigManager.Init("Development");

            IEditQuery<TestModelA> editQueryA = DaoFactory.GetEditIgniteQuery<TestModelA>();
            IEditQuery<TestModelB> editQueryB = DaoFactory.GetEditIgniteQuery<TestModelB>();

            using (ITransaction transaction = editQueryA.BeginTransaction())
            {
                for (int i = 0; i < 10000; i++)
                {
                    editQueryA.Insert(new TestModelA()
                    {
                        Name = Guid.NewGuid().ToString("D"),
                        Value = Environment.TickCount,
                        ID = IDGenerator.NextID()
                    });

                    editQueryA.Insert(new TestModelA()
                    {
                        Name = Guid.NewGuid().ToString("D"),
                        Value = Environment.TickCount,
                        ID = IDGenerator.NextID()
                    });

                    editQueryB.Insert(new TestModelB()
                    {
                        Name = Guid.NewGuid().ToString("D"),
                        Value = Environment.TickCount,
                        ID = IDGenerator.NextID()
                    });
                }

                transaction.Submit();
            }

            Console.Read();
        }
    }
}