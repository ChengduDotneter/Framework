using Common.MessageQueueClient;
using Microsoft.AspNetCore.Mvc;
using NPOI.HPSF;
using Orleans.Runtime;
using System;

namespace TestWebAPI.Controllers
{
    public class datamodel : IMQData
    {
        public DateTime CreateTime { get; set; }

        public string Name { get; set; }
    }

    [ApiController]
    [Route("producer")]
    public class ProducerApi : ControllerBase
    {
        [HttpGet]
        public void Produce()
        {
            IMQProducer<datamodel> mQProducer = MessageQueueFactory.GetKafkaProducer<datamodel>();
            mQProducer.Produce(new MQContext("tt", null), new datamodel { CreateTime = DateTime.Now, Name = Guid.NewGuid().ToString() });
        }

    }

    [ApiController]
    [Route("consumer")]
    public class ConsumerApi : ControllerBase
    {
        [HttpGet]
        public datamodel Consume()
        {
            datamodel datamodel = null;
            using (IMQConsumer<datamodel> mQProducer = MessageQueueFactory.GetKafkaConsumer<datamodel>("tt1"))
            {
                mQProducer.Subscribe(new MQContext("tt", null));
                mQProducer.Consume(new MQContext("tt", null), (data) => { datamodel = data; return true; });
                return datamodel;
            }
        }
    }
}
