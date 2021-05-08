using Common.MessageQueueClient;
using Common.MessageQueueClient.RabbitMQ;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MqContents;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers.RabbitMQSycnTest
{
    [Route("RabbitMQSycnProduceTest")]
    [ApiController]
    public class RabbitMQSycnProduceTestController : ControllerBase
    {
        [HttpGet]
        public void Get()
        {
            var productor = MessageQueueFactory.GetRabbitMQProducer<RabbitMQData>(ExChangeTypeEnum.Direct);
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey= RabbitMQSycnTestController.RabbitMQSycnTest };
            MQContext context1 = new MQContext(RabbitMQSycnTestController.RabbitMQSycnTest, context);
            productor.Produce(context1, new RabbitMQData());
        }
    }
}
