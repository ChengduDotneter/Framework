namespace Common.MessageQueueClient.Kafka
{
    public class KafkaConfig : QueueConfigBase
    {
        public string HostName { get; set; }
        public string GroupId { get; set; }
        public bool EnableAutoOffsetStore { get; set; }

        public KafkaConfig()
        {
            EnableAutoOffsetStore = true;
        }
    }
}