using System;
using Apache.Ignite.Core;

namespace Common
{
    /// <summary>
    /// Apache Ignite管理
    /// </summary>
    public static class IgniteManager
    {
        private static IIgnite m_ignite;
        private static bool m_init;

        /// <summary>
        /// Ignite初始化
        /// </summary>
        /// <param name="igniteConfiguration">初始化配置</param>
        public static void Init(IgniteConfiguration igniteConfiguration)
        {
            if (m_init)
                return;

            m_ignite = Ignition.Start(igniteConfiguration);
            m_init = true;
        }

        /// <summary>
        /// 获取Ignite实例
        /// </summary>
        /// <returns></returns>
        public static IIgnite GetIgnite()
        {
            if (!m_init)
                throw new Exception($"请先调用{nameof(Init)}进行初始化。");

            return m_ignite;
        }
    }
}
