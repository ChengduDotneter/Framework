namespace Common.Model
{/// <summary>
/// 导入状态类型枚举
/// </summary>
    public enum ImportStatusTypeEnum
    {
        Downloading,//正在下载
        Writting,//正在写入
        Error,//错误
        End//结束
    }
}