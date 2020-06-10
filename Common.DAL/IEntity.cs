namespace Common.DAL
{
    /// <summary>
    /// 实体模型接口
    /// </summary>
    public interface IEntity
    {
        /// <summary>
        /// 全局唯一ID，64位长整型
        /// </summary>
        long ID { get; }
    }
}