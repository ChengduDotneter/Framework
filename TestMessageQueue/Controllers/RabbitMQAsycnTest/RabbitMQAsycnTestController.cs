using Common.MessageQueueClient;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MqContents;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers
{
    [Route("RabbitMQAsycnTest")]
    [ApiController]
    public class RabbitMQAsycnTestController : ControllerBase
    {
        public static readonly string RabbitMQAsycnTest = "RabbitMQAsycnTest";
        private static IMQConsumer<RabbitMQData> mqConsumer;
        public async Task Get()
        {
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey = RabbitMQSycnTestController.RabbitMQSycnTest };
            MQContext mqContext = new MQContext(RabbitMQAsycnTest, context);
            mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<RabbitMQData>(ExChangeTypeEnum.Direct);
            mqConsumer.Subscribe(mqContext);

            Task.Run(async () =>
            {
                await mqConsumer.ConsumeAsync(mqContext, data =>
                {
                    return new Task<bool>(() => { return true; });
                });
            });
        }
    }
}
