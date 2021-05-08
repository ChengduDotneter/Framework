using Common.MessageQueueClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers.KafkaSyncTest
{
    [Route("KafkaSyncProduceTest")]
    [ApiController]
    public class KafkaSyncProduceTestController : ControllerBase
    {
        [HttpGet]
        public void Get()
        {
            var productor = MessageQueueFactory.GetKafkaProducer<KafkaMQData>();
            MQContext context = new MQContext(KafkaSyncTestController.KafkaTest, null);
            productor.Produce(context, new KafkaMQData());
        }
    }
}
