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
    [Route("RabbitMQSycnTest")]
    [ApiController]
    public class RabbitMQSycnTestController : ControllerBase
    {
        private static IMQConsumer<RabbitMQData> mqConsumer;
        private static MQContext mqContext;
        ISet<string> sendGuids;
        ISet<string> recieveGuids;

        public RabbitMQSycnTestController()
        {
            sendGuids = new HashSet<string>();
            recieveGuids = new HashSet<string>();
        }
        public void Get()
        {
            var productor = MessageQueueFactory.GetRabbitMQProducer<RabbitMQData>(ExChangeTypeEnum.Direct);
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey = RabbitMQAsycnTestConsts.RoutingKey };
            MQContext context1 = new MQContext(RabbitMQAsycnTestConsts.RabbitMQAsycnTest, context);


            for (int i = 0; i < 100; i++)
            {
                var data = new RabbitMQData();
                sendGuids.Add(data.MyGuid);
                productor.Produce(context1, data);
            }

            mqContext = new MQContext(RabbitMQAsycnTestConsts.RabbitMQAsycnTest, context);
            mqConsumer = MessageQueueFactory.GetRabbitMQConsumer<RabbitMQData>(ExChangeTypeEnum.Direct);
            mqConsumer.Subscribe(mqContext);

            mqConsumer.Consume(mqContext, data =>
            {
                recieveGuids.Add(data.MyGuid);
                return true;
            });

            Task.Delay(15000);
            sendGuids.ExceptWith(recieveGuids);
            Console.WriteLine(sendGuids.Count == 0);
        }
    }
}
