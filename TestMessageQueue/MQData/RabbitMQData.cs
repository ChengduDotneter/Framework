using Common.MessageQueueClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestMessageQueue.MQData
{
    public class RabbitMQData : IMQData
    {
        public DateTime CreateTime => DateTime.Now;
    }
}
