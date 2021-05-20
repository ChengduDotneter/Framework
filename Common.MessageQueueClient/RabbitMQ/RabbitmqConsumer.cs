﻿using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.MessageQueueClient.RabbitMQ
{
    /// <summary>
    /// RabbitMq消费者操作类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RabbitmqConsumer<T> : IMQConsumer<T>, IMQBatchConsumer<T> where T : class, IMQData, new()
    {
        private static IConnectionFactory m_connectionFactory;
        private IConnection m_connection;
        private IModel m_channel;
        private string m_routingKey;
        private ISet<string> m_queueNames;
        private EventingBasicConsumer m_consumer;

        private readonly ExChangeTypeEnum m_exChangeTypeEnum;

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

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="exChangeTypeEnum">数据分发模式</param>
        public RabbitmqConsumer(ExChangeTypeEnum exChangeTypeEnum)
        {
            m_exChangeTypeEnum = exChangeTypeEnum;
            RabbitMqConsumerInit();
        }

        private void Consume(MQContext mQContext, EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            if (m_queueNames.Contains(mQContext.MessageQueueName))
            {
                //接收事件
                m_consumer.Received += eventHandler;
                //开启监听
                m_channel.BasicConsume(queue: mQContext.MessageQueueName, autoAck: false, consumer: m_consumer);
            }
        }

        /// <summary>
        /// 消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消费回调</param>
        public void Consume(MQContext mQContext, Func<T, bool> callback)
        {
            Consume(mQContext, (eventSender, args) =>
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
                    throw;
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
                    Log4netCreater.CreateLog("RabbitmqConsumer").Error($"{exception.InnerException},{exception.StackTrace} ");
                    throw;
                }
            });
        }

        /// <summary>
        /// 异步消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消息回调</param>
        public Task ConsumeAsync(MQContext mQContext, Func<T, Task<bool>> callback)
        {
            Consume(mQContext, async (eventSender, args) =>
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
                    throw;
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
                    Log4netCreater.CreateLog("RabbitmqConsumer").Error($"{exception.InnerException},{exception.StackTrace} ");
                    throw;
                }
            });

            return Task.CompletedTask;
        }

        /// <summary>
        /// 同步批量消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消息回调</param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        public void Consume(MQContext mQContext, Func<IEnumerable<T>, bool> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            if (m_queueNames.Contains(mQContext.MessageQueueName))
            {
                _ = Task.Factory.StartNew(async () =>
                {
                    IList<BatchData> batchDatas = new List<BatchData>();

                    while (true)
                    {
                        while (true)
                        {
                            BasicGetResult basicGetResult = m_channel.BasicGet(mQContext.MessageQueueName, false);

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
                                throw;
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
                                    for (int i = 0; i < batchDatas.Count; i++)
                                        m_channel.BasicReject(batchDatas[i].DeliveryTag, true);
                                }
                            }
                            catch (Exception exception)
                            {
                                //处理逻辑失败时，该消息扔回消息队列
                                for (int i = 0; i < batchDatas.Count; i++)
                                    m_channel.BasicReject(batchDatas[i].DeliveryTag, true);
                                
                                Log4netCreater.CreateLog("RabbitmqConsumer").Error($"{exception.InnerException},{exception.StackTrace} ");
                                throw;
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
        }

        /// <summary>
        /// 异步批量消费
        /// </summary>
        /// <param name="mQContext">MQ上下文</param>
        /// <param name="callback">消息回调</param>
        /// <param name="pullingTimeSpan">拉取数据时间间隔</param>
        /// <param name="pullingCount">拉取数据数据包大小分割</param>
        public Task ConsumeAsync(MQContext mQContext, Func<IEnumerable<T>, Task<bool>> callback, TimeSpan pullingTimeSpan, int pullingCount)
        {
            if (m_queueNames.Contains(mQContext.MessageQueueName))
            {
                _ = Task.Factory.StartNew(async () =>
                {
                    IList<BatchData> batchDatas = new List<BatchData>();

                    while (true)
                    {
                        while (true)
                        {
                            BasicGetResult basicGetResult = m_channel.BasicGet(mQContext.MessageQueueName, false);

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
                                throw;
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
                                
                                Log4netCreater.CreateLog("RabbitmqConsumer").Error($"{exception.InnerException},{exception.StackTrace} ");
                                throw;
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

            return Task.CompletedTask;
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        public void DeSubscribe()
        {
            Dispose();
        }

        private void RabbitMqConsumerInit()
        {
            m_queueNames = new HashSet<string>();

            if (m_connectionFactory == null)
                m_connectionFactory = RabbitmqHelper.CreateConnectionFactory();

            if (m_connection == null)
                m_connection = m_connectionFactory.CreateConnection();

            if (m_channel == null)
                m_channel = m_connection.CreateModel();

            //申明是否手动确认
            m_channel.BasicQos(0, 1, false);

            m_consumer = new EventingBasicConsumer(m_channel);
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
        /// <param name="mQContext"></param>
        public void Subscribe(MQContext mQContext)
        {
            if (mQContext.Context is RabbitMqContent rabbitMqContent)
            {
                if (string.IsNullOrWhiteSpace(m_routingKey) && !string.IsNullOrWhiteSpace(rabbitMqContent.RoutingKey))
                    m_routingKey = rabbitMqContent.RoutingKey;

                if (string.IsNullOrWhiteSpace(m_routingKey))
                    throw new Exception("路由关键字出错。");

                if (!m_queueNames.Contains(mQContext.MessageQueueName))
                {
                    m_queueNames.Add(mQContext.MessageQueueName);

                    RabbitmqHelper.BindingQueues(mQContext.MessageQueueName, m_exChangeTypeEnum, m_channel, m_routingKey, m_queueNames);
                }
            }
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

                Log4netCreater.CreateLog("RabbitmqConsumer").Error(errorMessage);
                throw new Exception(errorMessage);
            }
        }
    }
}