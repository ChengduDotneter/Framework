namespace Common.Model
{
    /// <summary>
    /// Consul所需服务实体类，则不自动生成controller
    /// </summary>
    public class ConsulServiceEntity
    {
        /// <summary>
        /// ip地址
        /// </summary>
        public string IP { get; set; }

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 服务名称
        /// </summary>
        public string ServiceName { get; set; }

        /// <summary>
        /// Consul的ip
        /// </summary>
        public string ConsulIP { get; set; }

        /// <summary>
        /// consul的端口号
        /// </summary>
        public int ConsulPort { get; set; }

        /// <summary>
        /// 服务启动后多久进行注册
        /// </summary>
        public int DeregisterCriticalServiceAfter { get; set; }

        /// <summary>
        /// 健康检查时间间隔，或者称为心跳间隔
        /// </summary>
        public int Interval { get; set; }

        /// <summary>
        /// 心跳检测地址
        /// </summary>
        public string HealthPath { get; set; }

        /// <summary>
        /// 超时
        /// </summary>
        public int Timeout { get; set; }
    }
}