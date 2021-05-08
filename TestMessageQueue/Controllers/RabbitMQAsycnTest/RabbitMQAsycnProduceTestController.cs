using Common.MessageQueueClient;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MqContents;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers.RabbitMQAsycnTest
{
    [Route("RabbitMQAsycnProduceTest")]
    [ApiController]
    public class RabbitMQAsycnProduceTestController : ControllerBase
    {
        [HttpGet]
        public async Task Get()
        {
            var productor = MessageQueueFactory.GetRabbitMQProducer<RabbitMQData>(ExChangeTypeEnum.Direct);
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey = RabbitMQSycnTestController.RabbitMQSycnTest };
            MQContext context1 = new MQContext(RabbitMQAsycnTestController.RabbitMQAsycnTest, context);
            await productor.ProduceAsync(context1, new RabbitMQData());
        }
    }
}
