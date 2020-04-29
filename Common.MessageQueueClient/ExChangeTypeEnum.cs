namespace Common.MessageQueueClient
{
    public enum ExChangeTypeEnum
    {
        /// <summary>
        /// 关键字匹配
        /// </summary>
        direct,

        /// <summary>
        /// 数据分发
        /// </summary>
        fanout,

        /// <summary>
        /// 关键字模糊匹配
        /// </summary>
        topic,

        /// <summary>
        /// 键值对匹配
        /// </summary>
        headers
    }
}
