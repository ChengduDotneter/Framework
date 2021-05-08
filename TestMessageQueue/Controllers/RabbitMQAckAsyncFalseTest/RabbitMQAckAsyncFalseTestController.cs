﻿using Common.MessageQueueClient;
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
    [Route("RabbitMQAckAsyncFalseTest")]
    [ApiController]
    public class RabbitMQAckAsyncFalseTestController
    {
        private static IMQAckConsumer<RabbitMQData> mqConsumer;
        private static MQContext mqContext;

        ISet<string> sendGuids;
        ISet<string> recieveGuids;

        public RabbitMQAckAsyncFalseTestController()
        {
            sendGuids = new HashSet<string>();
            recieveGuids = new HashSet<string>();
        }

        public async Task Get()
        {
            var productor = MessageQueueFactory.GetRabbitMQProducer<RabbitMQData>(ExChangeTypeEnum.Direct);
            TestRabbitMqContent context = new TestRabbitMqContent() { RoutingKey = RabbitMQAckAsyncFalseTestConsts.RoutingKey };
            MQContext context1 = new MQContext(RabbitMQAckAsyncFalseTestConsts.RabbitMQAckAsyncFalseTest, context);


            for (int i = 0; i < 100; i++)
            {
                var data = new RabbitMQData();
                sendGuids.Add(data.MyGuid);
                await productor.ProduceAsync(context1, data);
            }

            mqContext = new MQContext(RabbitMQAckAsyncFalseTestConsts.RabbitMQAckAsyncFalseTest, context);
            mqConsumer = MessageQueueFactory.GetRabbitMQAckConsumer<RabbitMQData>(ExChangeTypeEnum.Direct);
            mqConsumer.Subscribe(mqContext);

            await mqConsumer.AckConsumeAsync(mqContext, data =>
            {
                recieveGuids.Add(data.Data.MyGuid);
                return Task.FromResult(false);
            });

            await Task.Delay(15000);
            sendGuids.ExceptWith(recieveGuids);
            Console.WriteLine(sendGuids.Count == 0);
        }
    }
}
