using Common.MessageQueueClient;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable UnusedVariable
// ReSharper disable RedundantCatchClause
// ReSharper disable UnusedParameter.Local

namespace TestStorge.Controllers
{
    /// <summary>
    /// 商品MQ数据
    /// </summary>
    public class CommodityMQData : IMQData
    {
        /// <summary>
        /// 消息类型
        /// </summary>
        public string MessageType { get; set; }

        /// <summary>
        /// 消息体
        /// </summary>
        public object Message { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }

    public class MBCloudCommodityDownLoad : IHostedService
    {
        private IMQConsumer<CommodityMQData> m_mQConsumer;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            MessageQueueInit();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private void MessageQueueInit()
        {
            if (m_mQConsumer == null)
            {
                m_mQConsumer = MessageQueueFactory.GetRabbitMQConsumer<CommodityMQData>("StoreERP", "StoreERP");
                m_mQConsumer.Subscribe();

                ConsumeDatas();
            }
        }

        public void ConsumeDatas()
        {
            try
            {
                m_mQConsumer.Consume(commodityMQData =>
                {
                    Random random = new Random();

                    //  if (random.Next(10) % 2 == 0)
                    throw new Exception("模拟错误");

                    //return true;
                });
            }
            catch
            {
                throw;
            }
        }
    }
}