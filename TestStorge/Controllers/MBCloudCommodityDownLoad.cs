using Common.MessageQueueClient;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        private MQContext m_mQContext;
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
            if (m_mQContext == null)
                m_mQContext = new MQContext("StoreERP", new RabbitMqContent() { RoutingKey = "StoreERP" });

            if (m_mQConsumer == null)
            {
                m_mQConsumer = MessageQueueFactory.GetRabbitMQConsumer<CommodityMQData>(ExChangeTypeEnum.Direct);
                m_mQConsumer.Subscribe(m_mQContext);

                ConsumeDatas();
            }
        }

        public void ConsumeDatas()
        {
            try
            {
                m_mQConsumer.Consume(m_mQContext, commodityMQData =>
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
