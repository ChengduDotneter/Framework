using System;

namespace Common.MessageQueueClient
{
    internal class MqConfigFactory
    {
        /// <summary>
        /// 创建MqConfigModel一个实例。
        /// </summary>
        /// <returns>MqConfigModel</returns>
        internal static MqConfigModel CreateConfigDomInstance()
        {
            return GetConfigFormAppStting();
        }

        /// <summary>
        /// 获取配置文件中的配置项目。
        /// </summary>
        /// <returns></returns>
        private static MqConfigModel GetConfigFormAppStting()
        {
            MqConfigModel config = new MqConfigModel();

            string mqHost = ConfigManager.Configuration["RabbitServer:Host"];
            if (string.IsNullOrWhiteSpace(mqHost))
                throw new Exception("RabbitMQ地址配置错误");
            config.MqHost = mqHost;

            string mqUserName = ConfigManager.Configuration["RabbitServer:UserName"];
            if (string.IsNullOrWhiteSpace(mqUserName))
                throw new Exception("RabbitMQ用户名不能为NULL");

            config.MqUserName = mqUserName;

            string mqPassword = ConfigManager.Configuration["RabbitServer:Password"];
            if (string.IsNullOrWhiteSpace(mqPassword))
                throw new Exception("RabbitMQ密码不能为NULL");

            config.MqPassword = mqPassword;
            config.MqPort = Convert.ToInt32(ConfigManager.Configuration["RabbitServer:Port"]);

            string mqRequestedHeartbeat = ConfigManager.Configuration["RabbitServer:RequestedHeartbeat"];
            if (string.IsNullOrWhiteSpace(mqRequestedHeartbeat))
                throw new Exception("设置的心跳超时时间不能为NULL");

            config.RequestedHeartbeat = (ushort)Convert.ToInt32(mqRequestedHeartbeat);

            return config;
        }
    }
}