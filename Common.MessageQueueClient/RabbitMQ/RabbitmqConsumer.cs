using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq消费者操作类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RabbitmqConsumer<T> : IMQConsumer<T>, IMQBatchConsumer<T> where T : class, IMQData, new()
    {
        private readonly static ILog m_log;
        private readonly IConnection m_connection; //连接管道
        private readonly IModel m_channel; //会话模型
        private readonly EventingBasicConsumer m_consumer;
        private readonly RabbitMQConfig m_rabbitMqConfig;

        private class BatchData
        {
            public ulong DeliveryTag { get; }
            public T Data { get; }

            public BatchData(ulong deliveryTag, T data)
            {
                DeliveryTag = deliveryTag;
                Data = data;
            }
        }

        static RabbitmqConsumer()
        {
            m_log = Log4netCreater.CreateLog("RabbitmqConsumer");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public RabbitmqConsumer(RabbitMQConfig rabbitMqConfig)
        {
            m_rabbitMqConfig = rabbitMqConfig;

            ConnectionFactory connectionFactory = new ConnectionFactory()
            {
                HostName = m_rabbitMqConfig.HostName,
                Port = m_rabbitMqConfig.Port,
                UserName = m_rabbitMqConfig.UserName,
                Password = m_rabbitMqConfig.Password,
                RequestedHeartbeat = m_rabbitMqConfig.RequestedHeartbeat,
                AutomaticRecoveryEnabled = true //自动重连
            };

            m_connection = connectionFactory.CreateConnection();
            m_channel = m_connection.CreateModel();
            m_channel.BasicQos(0, 1, false);
            m_consumer = new EventingBasicConsumer(m_channel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventHandler">事件</param>
        private void Consume(EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            //接收事件
            m_consumer.Received += eventHandler; //接收消息触发的事件
            //开启监听
            m_channel.BasicConsume(queue: m_rabbitMqConfig.QueueName, autoAck: false, consumer: m_consumer);
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="callback">消费回调</param>
        public void Consume(Func<T, bool> callback)
        {
            Consume((eventSender, args) =>
            {
                byte[] message = args.Body; //接收到的消息
                T data = null;

                try
                {
                    data = ConvertMessageToData(Encoding.UTF8.GetString(message));
                }
                catch
                {
                    //数据convert失败时，直接删除该数据
                    m_channel.BasicReject(args.DeliveryTag, false);
                }

                try
                {
                    if (callback?.Invoke(data) ?? false)
                        //返回消息确认
                        m_channel.BasicAck(args.DeliveryTag, true);
                    else
                        m_channel.BasicReject(args.DeliveryTag, true);
                }
                catch (Exception exception)
                {
                    //处理逻辑失败时，该消息扔回消息队列
                    m_channel.BasicReject(args.DeliveryTag, true);
                    m_log.Error($"{exception.InnerException},{exception.StackTrace} ");
                }
            });
        }

        /// <summary>
        /// 异步消费
        /// </summary>
        /// <param name="callback">消息回调</param>
        public Task ConsumeAsync(Func<T, Task<bool>> callback)
        {
            async void EventHandler(object eventSender, BasicDeliverEventArgs args)
            {
                byte[] message = args.Body; //接收到的消息
                T data = null;

                try
                {
                    data = ConvertMessageToData(Encoding.UTF8.GetString(message));
                }
                catch
                {
                    //数据convert失败时，直接删除该数据
                    m_channel.BasicReject(args.DeliveryTag, false);
                }

                try
                {
                    if (callback == null ? false : await callback.Invoke(data))
                        //返回消息确认
                        m_channel.BasicAck(args.DeliveryTag, true);
                    else
                        m_channel.BasicReject(args.DeliveryTag, true);
                }
                catch (Exception exception)
                {
                    //处理逻辑失败时，该消息扔回消息队列
                    m_channel.BasicReject(args.DeliveryTag, true);
                    m_log.Error($"{exception.InnerException},{exception.StackTrace} ");
                }
            }

            Consume(EventHandler);
            return Task.CompletedTask;
        }

        /// <summary>
        /// 同步批量消费
        /// </summary>
        /// <param name="callback">消息回调</param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        public void Consume(Func<IEnumerable<T>, bool> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                IList<BatchData> batchDatas = new List<BatchData>();

                while (true)
                {
                    while (true)
                    {
                        BasicGetResult basicGetResult = m_channel.BasicGet(m_rabbitMqConfig.QueueName, false);

                        if (basicGetResult == null)
                            break;

                        T data = null;

                        try
                        {
                            data = ConvertMessageToData(Encoding.UTF8.GetString(basicGetResult.Body));
                        }
                        catch
                        {
                            //数据convert失败时，直接删除该数据
                            m_channel.BasicReject(basicGetResult.DeliveryTag, false);
                        }

                        batchDatas.Add(new BatchData(basicGetResult.DeliveryTag, data));

                        if (batchDatas.Count >= pullingCount)
                            break;
                    }

                    if (batchDatas.Count > 0)
                    {
                        try
                        {
                            bool result = false;

                            if (callback != null)
                                result = callback.Invoke(batchDatas.Select(item => item.Data));

                            if (result)
                            {
                                //返回消息确认
                                m_channel.BasicAck(batchDatas.Last().DeliveryTag, true);
                            }
                            else
                            {
                                //该消息扔回消息队列
                                for (int i = 0; i < batchDatas.Count; i++)
                                    m_channel.BasicReject(batchDatas[i].DeliveryTag, true);
                            }
                        }
                        catch (Exception exception)
                        {
                            //处理逻辑失败时，该消息扔回消息队列
                            for (int i = 0; i < batchDatas.Count; i++)
                                m_channel.BasicReject(batchDatas[i].DeliveryTag, true);

                            m_log.Error($"{exception.InnerException},{exception.StackTrace} ");
                        }
                        finally
                        {
                            batchDatas.Clear();
                        }
                    }

                    await Task.Delay(pullingTimeSpan);
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// 异步批量消费
        /// </summary>
        /// <param name="callback">消息回调</param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        public Task ConsumeAsync(Func<IEnumerable<T>, Task<bool>> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            _ = Task.Factory.StartNew(async () =>
            {
                IList<BatchData> batchDatas = new List<BatchData>();

                while (true)
                {
                    while (true)
                    {
                        BasicGetResult basicGetResult = m_channel.BasicGet(m_rabbitMqConfig.QueueName, false);

                        if (basicGetResult == null)
                            break;

                        T data = null;

                        try
                        {
                            data = ConvertMessageToData(Encoding.UTF8.GetString(basicGetResult.Body));
                        }
                        catch
                        {
                            //数据convert失败时，直接删除该数据
                            m_channel.BasicReject(basicGetResult.DeliveryTag, false);
                        }

                        batchDatas.Add(new BatchData(basicGetResult.DeliveryTag, data));

                        if (batchDatas.Count >= pullingCount)
                            break;
                    }

                    if (batchDatas.Count > 0)
                    {
                        try
                        {
                            bool result = false;

                            if (callback != null)
                                result = await callback.Invoke(batchDatas.Select(item => item.Data));

                            if (result)
                            {
                                //返回消息确认
                                m_channel.BasicAck(batchDatas.Last().DeliveryTag, true);
                            }
                            else
                            {
                                for (int i = 0; i < batchDatas.Count; i++)
                                    m_channel.BasicReject(batchDatas[i].DeliveryTag, true);
                            }
                        }
                        catch (Exception exception)
                        {
                            //处理逻辑失败时，该消息扔回消息队列
                            for (int i = 0; i < batchDatas.Count; i++)
                                m_channel.BasicReject(batchDatas[i].DeliveryTag, true);

                            m_log.Error($"{exception.InnerException},{exception.StackTrace} ");
                        }
                        finally
                        {
                            batchDatas.Clear();
                        }
                    }

                    await Task.Delay(pullingTimeSpan);
                }
            }, TaskCreationOptions.LongRunning);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            Dispose();
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            m_channel?.Dispose();
            m_connection?.Dispose();
        }

        /// <summary>
        /// 订阅
        /// </summary>
        public void Subscribe()
        {
            m_channel.ExchangeDeclare(m_rabbitMqConfig.QueueName, type: m_rabbitMqConfig.ExChangeType.ToString().ToLower(), durable: true, autoDelete: false, null); //设置交换器类型
            m_channel.QueueDeclare(m_rabbitMqConfig.QueueName, true, false, false, null);
            m_channel.QueueBind(m_rabbitMqConfig.QueueName, m_rabbitMqConfig.QueueName, m_rabbitMqConfig.RoutingKey); // 设置路由关键字即为队列的名称
        }

        /// <summary>
        /// 根据泛型将Message反序列化为相应对象
        /// </summary>
        /// <param name="message">数据</param>
        /// <returns></returns>
        private T ConvertMessageToData(string message)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(message);
            }
            catch
            {
                string errorMessage = $"RabbitMQConsum序列化失败：{message}。";

                m_log.Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}