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
        private static IMQConsumer<RabbitMQData> mqConsumer;
        private static MQContext mqContext;

        ISet<string> sendGuids;
        ISet<string> recieveGuids;

        public RabbitMQAsycnTestController()
        {
            sendGuids = new HashSet<string>();
            recieveGuids = new HashSet<string>();
        }

        public async Task Get()
        {
            var productor = MessageQueueFactory.GetRabbitMQProducer<RabbitMQData>(ExChangeTypeEnum.Direct);
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey = RabbitMQAsycnTestConsts.RoutingKey };
            MQContext context1 = new MQContext(RabbitMQAsycnTestConsts.RabbitMQAsycnTest, context);


            for (int i = 0; i < 100; i++)
            {
                var data = new RabbitMQData();
                sendGuids.Add(data.MyGuid);
                await productor.ProduceAsync(context1, data);
            }

            mqContext = new MQContext(RabbitMQAsycnTestConsts.RabbitMQAsycnTest, context);
            mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<RabbitMQData>(ExChangeTypeEnum.Direct);
            mqConsumer.Subscribe(mqContext);

            await mqConsumer.ConsumeAsync(mqContext, data =>
            {
                recieveGuids.Add(data.MyGuid);
                return Task.FromResult(true);
            });

            await Task.Delay(15000);
            sendGuids.ExceptWith(recieveGuids);
            Console.WriteLine(sendGuids.Count == 0);
        }
    }
}
