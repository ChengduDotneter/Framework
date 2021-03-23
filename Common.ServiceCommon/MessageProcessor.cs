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

        internal abstract Task RecieveMessage(object parameter, CancellationToken cancellationToken);

        protected MessageProcessor(string identity)
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
        private readonly ILogHelper m_logHelper;

        internal override Task RecieveMessage(object parameter, CancellationToken cancellationToken)
        {
            try
            {
                return RecieveMessage(JsonConvert.DeserializeObject<T>((string)parameter), cancellationToken);
            }
            catch (Exception exception)
            {
                string errorMessage = ExceptionHelper.GetMessage(exception);
                SendMessage(errorMessage);
                m_logHelper.Error(this.GetType().Name, errorMessage);
                return Task.CompletedTask;
            }
        }

        /// <summary>
        /// 接收消息
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task RecieveMessage(T parameter, CancellationToken cancellationToken);

        protected void SendMessage<TMessage>(TMessage message)
        {
            SendDatas.Add(JsonConvert.SerializeObject(message));
        }

        protected MessageProcessor(string identity, ILogHelper logHelper) : base(identity)
        {
            m_logHelper = logHelper;
        }
    }

    /// <summary>
    /// 数据导出数据结构
    /// </summary>
    public class ExportResult
    {
        public int TotalCount { get; set; }
        public int ReadCount { get; set; }
        public ExportStatusTypeEnum ExportStatus { get; set; }
        public string ErrorMessage { get; set; }
        public string FilePath { get; set; }
    }

    /// <summary>
    /// 数据导入请求
    /// </summary>
    public class ImportRequest
    {
        public string FileName { get; set; }
    }

    /// <summary>
    /// 分区数据导入请求
    /// </summary>
    public class SystemImportRequest : ImportRequest
    {
        public string SystemID { get; set; }
    }

    /// <summary>
    /// 导入结果
    /// </summary>
    public class ImportResult
    {
        public int TotalCount { get; set; }
        public int WriteCount { get; set; }
        public ImportStatusTypeEnum ImportStatus { get; set; }
        public string ErrorMessage { get; set; }
        public int ErrorCount { get; set; }
        public string ErrorFilePath { get; set; }
    }

    /// <summary>
    /// 查询消息处理器
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TSearchData"></typeparam>
    public abstract class SearchMessageProcessor<TRequest, TSearchData> : MessageProcessor<TRequest> where TSearchData : ViewModelBase
    {
        protected virtual Expression<Func<TSearchData, bool>> GetBaseLinq(TRequest queryCondition)
        {
            if (queryCondition == null)
                return item => true;

            LinqSearchAttribute linqSearchAttribute = typeof(TSearchData).GetCustomAttribute<LinqSearchAttribute>();

            if (linqSearchAttribute != null && !string.IsNullOrWhiteSpace(linqSearchAttribute.GetLinqFunctionName))
            {
                MethodInfo method = typeof(TSearchData).GetMethod(linqSearchAttribute.GetLinqFunctionName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (method != null)
                {
                    Func<TRequest, Expression<Func<TSearchData, bool>>> predicateLinq = method.Invoke(null, null) as Func<TRequest, Expression<Func<TSearchData, bool>>>;

                    if (predicateLinq != null)
                        return predicateLinq(queryCondition);
                }
            }

            return item => true;
        }

        protected SearchMessageProcessor(string identity, ILogHelper logHelper) : base(identity, logHelper) { }
    }
}