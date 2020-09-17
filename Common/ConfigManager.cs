using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;

namespace Common
{
    /// <summary>
    /// 配置文件操作类
    /// </summary>
    public static class ConfigManager
    {
        private static bool m_isInit;
        private static IConfiguration m_configuration;

        /// <summary>
        /// 配置文件操作对象
        /// </summary>
        public static IConfiguration Configuration
        {
            get
            {
                if (!m_isInit)
                    throw new Exception($"请先调用{nameof(Init)}进行初始化。");

                return m_configuration;
            }
        }

        /// <summary>
        /// 配置文件初始化
        /// </summary>
        /// <param name="enviroment"></param>
        public static void Init(string enviroment)
        {
            m_configuration = new ConfigurationBuilder()
                .Add(new JsonConfigurationSource
                {
                    Path = "appsettings.json",
                    ReloadOnChange = true
                })
                .Add(new JsonConfigurationSource
                {
                    Path = $"appsettings.{enviroment}.json",
                    ReloadOnChange = true
                }).Build();

            m_isInit = true;
        }
    }
}