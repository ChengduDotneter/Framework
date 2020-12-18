namespace Common.MessageQueueClient
{
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