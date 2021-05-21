using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Common.Log;
using Common.Model;
using Newtonsoft.Json;

namespace Common.ServiceCommon
{
    /// <summary>
    /// 消息处理器路由特性
    /// </summary>
    public class MessageProcessorRouteAttribute : Attribute
    {
        public string Template { get; set; }

        public MessageProcessorRouteAttribute(string template)
        {
            Template = template;
        }
    }

    /// <summary>
    /// 消息处理器
    /// </summary>
    public abstract class MessageProcessor
    {
        internal BlockingCollection<string> SendDatas { get; }
        protected string Identity { get; }

        internal abstract Task RecieveMessage(object parameter, CancellationToken cancellationToken);//接收消息的

        protected MessageProcessor(string identity)//身份标识
        {
            Identity = identity;
            SendDatas = new BlockingCollection<string>();
        }
    }

    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MessageProcessor<T> : MessageProcessor
    {
        private readonly ILogHelper m_logHelper;//日志

        internal override async Task RecieveMessage(object parameter, CancellationToken cancellationToken)//接收消息的重写
        {
            try
            {
                await RecieveMessage(JsonConvert.DeserializeObject<T>((string)parameter), cancellationToken);//把接收的消息序列化后传递给具体的处理类
            }
            catch (Exception exception)
            {
                string errorMessage = ExceptionHelper.GetMessage(exception);//出错 返回错误信息并记录日志
                SendMessage(errorMessage);
                await m_logHelper.Error(this.GetType().Name, errorMessage);
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task RecieveMessage(T parameter, CancellationToken cancellationToken);//给继承的类实现的消息接收方法

        protected void SendMessage<TMessage>(TMessage message)//发送消息
        {
            SendDatas.Add(JsonConvert.SerializeObject(message));
        }

        protected MessageProcessor(string identity, ILogHelper logHelper) : base(identity)//构造函数
        {
            m_logHelper = logHelper;
        }
    }

    /// <summary>
    /// 数据导出数据结构
    /// </summary>
    public class ExportResult
    {
        public int TotalCount { get; set; }//总行数
        public int ReadCount { get; set; }//读取条数
        public ExportStatusTypeEnum ExportStatus { get; set; }//导出状态
        public string ErrorMessage { get; set; }//错误消息
        public string FilePath { get; set; }//文件路径
    }

    /// <summary>
    /// 数据导入请求
    /// </summary>
    public class ImportRequest
    {
        public string FileName { get; set; }//文件名
    }

    /// <summary>
    /// 分区数据导入请求
    /// </summary>
    public class SystemImportRequest : ImportRequest
    {
        public string SystemID { get; set; }//系统id
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult//导入结果实体
    {
        public int TotalCount { get; set; }//总条数
        public int WriteCount { get; set; }//导入条数
        public ImportStatusTypeEnum ImportStatus { get; set; }//导入结果
        public string ErrorMessage { get; set; }//错误信息
        public int ErrorCount { get; set; }//错误条数
        public string ErrorFilePath { get; set; }//错误文件路径
    }

    /// <summary>
    /// 查询消息处理器
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TSearchData"></typeparam>
    public abstract class SearchMessageProcessor<TRequest, TSearchData> : MessageProcessor<TRequest> where TSearchData : ViewModelBase
    {
        protected virtual Expression<Func<TSearchData, bool>> GetBaseLinq(TRequest queryCondition)//获取查询条件
        {
            if (queryCondition == null)//请求的参数不为空
                return item => true;

            LinqSearchAttribute linqSearchAttribute = typeof(TSearchData).GetCustomAttribute<LinqSearchAttribute>();//是否有LinqSearchAttribute特性

            if (linqSearchAttribute != null && !string.IsNullOrWhiteSpace(linqSearchAttribute.GetLinqFunctionName))
            {//获取查询条件的方法
                MethodInfo method = typeof(TSearchData).GetMethod(linqSearchAttribute.GetLinqFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)//不为null
                {
                    Func<TRequest, Expression<Func<TSearchData, bool>>> predicateLinq = method.Invoke(null, null) as Func<TRequest, Expression<Func<TSearchData, bool>>>;//实例化并转换

                    if (predicateLinq != null)
                        return predicateLinq(queryCondition);//获取linq查询条件
                }
            }

            return item => true;
        }

        protected SearchMessageProcessor(string identity, ILogHelper logHelper) : base(identity, logHelper) { }//构造函数
    }
}