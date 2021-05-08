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
    [Route("KafkaSyncTest")]
    [ApiController]
    public class KafkaSyncTestController : ControllerBase
    {
        public static readonly string KafkaTest = "KafkaTest";
        [HttpGet]
        public void Get()
        {
            MQContext mqContext = new MQContext(KafkaTest, new KafkaMQData());
            IMQConsumer<KafkaMQData> mqConsumer = MessageQueueFactory.GetKafkaConsumer<KafkaMQData>(KafkaTest);
            mqConsumer.Subscribe(mqContext);

            mqConsumer.Consume(mqContext, data =>
            {
                return true;
            });

          

        }
    }
}
