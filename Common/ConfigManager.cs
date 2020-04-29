using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using System;

namespace Common
{
    public static class ConfigManager
    {
        private static bool m_isInit;
        private static IConfiguration m_configuration;

        public static IConfiguration Configuration
        {
            get
            {
                if (!m_isInit)
                    throw new Exception($"请先调用{nameof(Init)}进行初始化。");

                return m_configuration;
            }
        }

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
