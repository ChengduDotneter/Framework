using Common.MessageQueueClient;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMessageQueue.MQData;

namespace TestMessageQueue.Controllers
{
    [Route("KafkaAsyncTest")]
    [ApiController]
    public class KafkaAsyncTestController : ControllerBase
    {
        public static readonly string KafkaAsyncTest = "KafkaAsyncTest";
        private static IMQConsumer<KafkaMQData> mqConsumer;
        public async Task Get()
        {
            MQContext mqContext = new MQContext(KafkaAsyncTest, new KafkaMQData());
            mqConsumer = MessageQueueFactory.GetKafkaConsumer<KafkaMQData>(KafkaAsyncTest);
            mqConsumer.Subscribe(mqContext);

            Task.Run(async() =>
            {
                await mqConsumer.ConsumeAsync(mqContext, data =>
                {
                    return new Task<bool>(() => { return true; });
                });
            });
          
        }
    }
}
