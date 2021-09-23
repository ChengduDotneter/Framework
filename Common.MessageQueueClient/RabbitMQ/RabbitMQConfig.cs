namespace Common.MessageQueueClient.RabbitMQ
{
    public class RabbitMQConfig : QueueConfigBase
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public ushort RequestedHeartbeat { get; set; }
        public string RoutingKey { get; set; }
        public ExChangeTypeEnum ExChangeType { get; set; }
    }
    
    /// <summary>
    /// 转换类型枚举
    /// </summary>
    public enum ExChangeTypeEnum
    {
        /// <summary>
        /// 关键字匹配
        /// </summary>
        Direct,

        /// <summary>
        /// 数据分发
        /// </summary>
        Fanout,

        /// <summary>
        /// 关键字模糊匹配
        /// </summary>
        Topic,

        /// <summary>
        /// 键值对匹配
        /// </summary>
        Headers
    }
}