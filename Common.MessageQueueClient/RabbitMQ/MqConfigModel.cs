namespace Common.MessageQueueClient
{
    /// <summary>
    /// 消息队列相关配置的Model。
    /// </summary>
    public class MqConfigModel
    {
        /// <summary>
        /// 消息队列的地址。
        /// </summary>
        public string MqHost { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string MqUserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        public string MqPassword { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int MqPort { get; set; }

        /// <summary>
        /// 心跳超时时间
        /// </summary>
        public ushort RequestedHeartbeat { get; set; }
    }
}