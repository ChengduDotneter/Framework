using Common.MessageQueueClient;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers
{
    [Route("RabbitMQSycnTest")]
    [ApiController]
    public class RabbitMQSycnTestController : ControllerBase
    {
        public static readonly string RabbitMQSycnTest = "RabbitMQSycnTest";
        private static IMQConsumer<RabbitMQData> mqConsumer;
        public void Get()
        {
            
            MQContext mqContext = new MQContext(RabbitMQSycnTest, new KafkaMQData());
            mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<RabbitMQData>(ExChangeTypeEnum.Direct);
            mqConsumer.Subscribe(mqContext);

            Task.Run( () =>
            {
                mqConsumer.Consume(mqContext, data =>
                {
                    return true;
                });
            });
        }
    }
}
