using System;
using System.Collections.Generic;
using System.Text;

namespace Common.MessageQueueClient.RabbitMQ
{
    public class RabbitmqConsumer<T> : IMQConsumer<T> where T : class, IMQData, new()
    {
        public void Consume(MQContext mQContext, Func<T, bool> callback)
        {
            throw new NotImplementedException();
        }

        public void DeSubscribe()
        {
            throw new NotImplementedException();
        }

        public void Subscribe(MQContext mQContext)
        {
            throw new NotImplementedException();
        }
    }
}
