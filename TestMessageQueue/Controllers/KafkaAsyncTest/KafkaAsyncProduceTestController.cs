using Common.MessageQueueClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers.KafkaAsyncTest
{
    [Route("KafkaAsyncProduceTest")]
    [ApiController]
    public class KafkaAsyncProduceTestController : ControllerBase
    {
        [HttpGet]
        public async Task Get()
        {
            var productor = MessageQueueFactory.GetKafkaProducer<KafkaMQData>();
            MQContext context = new MQContext(KafkaAsyncTestController.KafkaAsyncTest, null);
            await productor.ProduceAsync(context, new KafkaMQData());
        }
    }
}
