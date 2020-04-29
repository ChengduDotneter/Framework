using System;
using System.Threading;

namespace Common
{
    public static class IDGenerator
    {
        /// <summary>
        /// 节点类型
        /// </summary>
        private static readonly uint nodeType;

        /// <summary>
        /// 节点编号
        /// </summary>
        private static readonly uint node;

        /// <summary>
        /// 线程锁
        /// </summary>
        private static readonly object lockThis = new object();

        /// <summary>
        /// 进程同步锁名称格式化字符串
        /// </summary>
        private const string MUTEX_NAME_STRING_FORMAT = "{0}_{1}";

        /// <summary>
        /// 最小时间
        /// </summary>
        private static readonly DateTime MIN_DATE_TIME = new DateTime(1970, 1, 1);

        /// <summary>
        /// 开始时间截 (2018-01-01)
        /// </summary>
        private const long TWEPOCH = 1546300800000L;

        /// <summary>
        /// 机器id所占的位数，节点ID范围在0-255之间
        /// </summary>
        private const int NODE_BITS = 8;

        /// <summary>
        /// 机器id最小值
        /// </summary>
        private const int MIN_NODE = 0;

        /// <summary>
        /// 机器id最大值
        /// </summary>
        private const int MAX_NODE = (-1 ^ (-1 << NODE_BITS));

        /// <summary>
        /// 序列在id中占的位数
        /// </summary>
        private const int SEQUENCE_BITS = 12;

        /// <summary>
        /// 毫秒级别时间截占的位数
        /// </summary>
        private const int TIME_STAMP_BITS = 41;

        /// <summary>
        /// 节点类型所占位数
        /// </summary>
        private const int NODE_TYPE_BITS = 2;

        /// <summary>
        /// 节点类型的最小值
        /// </summary>
        private const int MIN_NODE_TYPE = 0;

        /// <summary>
        /// 节点类型最大值
        /// </summary>
        private const int MAX_NODE_TYPE = (-1 ^ (-1 << NODE_TYPE_BITS));

        /// <summary>
        /// 机器id向左移2位
        /// </summary>
        private const int NODE_SHIFT = NODE_TYPE_BITS;

        /// <summary>
        /// 生成序列向左移10位(8+2)
        /// </summary>
        private const int SEQUENCE_SHIFT = NODE_BITS + NODE_SHIFT;

        /// <summary>
        /// 时间截向左移22位(12+8+2)
        /// </summary>
        private const int TIME_STAMP_SHIFT = SEQUENCE_BITS + SEQUENCE_SHIFT;

        /// <summary>
        /// 生成序列的掩码，这里为4095 (0b111111111111=0xfff=4095)
        /// </summary>
        private const int SEQUENCE_MASK = -1 ^ (-1 << SEQUENCE_BITS);

        /// <summary>
        /// 上一次生成ID的总时间毫秒数
        /// </summary>
        private static long lastTimestamp;

        /// <summary>
        /// 毫秒内序列
        /// </summary>
        private static long sequence;

        /// <summary>
        /// 根据节点类型和节点编号生成新ID
        /// </summary>
        /// <returns>新生成的ID</returns>
        public static long NextID()
        {
            using (Mutex mutex = new Mutex(false, string.Format(MUTEX_NAME_STRING_FORMAT, nodeType, node)))
            {
                mutex.WaitOne();

                try
                {
                    return CreateNewID(nodeType, node);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        /// <summary>
        /// 获取从1970-01-01至今的毫秒数
        /// </summary>
        private static long GetTicks()
        {
            return (long)(DateTime.Now - MIN_DATE_TIME).TotalMilliseconds;
        }

        /// <summary>
        /// 阻塞线程直到下一毫秒
        /// </summary>
        /// <returns>阻塞之后的总时间毫秒数</returns>
        private static long BlockUntilNextMillis()
        {
            long timestamp = GetTicks();

            while (timestamp <= lastTimestamp)
                timestamp = GetTicks();

            return timestamp;
        }

        private static long CreateNewID(uint nodeType, uint node)
        {
            if (nodeType > MAX_NODE_TYPE || nodeType < MIN_NODE_TYPE)
                throw new Exception("节点类型错误。");

            if (node > MAX_NODE || node < MIN_NODE)
                throw new Exception("节点编号异常。");

            long timestamp;

            lock (lockThis)
            {
                timestamp = GetTicks();

                if (timestamp < lastTimestamp) //当前时间小于上一次ID生成的时间戳
                    throw new Exception("系统时间异常。");

                if (timestamp == lastTimestamp) //如果是同一时间生成的，则进行毫秒内序列
                {
                    sequence = (sequence + 1) & SEQUENCE_MASK;

                    if (sequence == 0) //毫秒内序列溢出
                        timestamp = BlockUntilNextMillis(); //阻塞到下一个毫秒,获得新的时间戳
                }
                else //时间戳改变，毫秒内序列重置
                {
                    sequence = 0L;
                }

                lastTimestamp = timestamp; //上次生成ID的时间截
            }

            return ((timestamp - TWEPOCH) << TIME_STAMP_SHIFT) | // 时间差占用41位，最多69年，左移22位
                   (sequence << SEQUENCE_SHIFT) | // 毫秒内序列，取值范围0-4095，左移10位，ANDROID下为0，左移22位
                   (node << NODE_SHIFT) | // 工作机器，取值范围0-255，左移2位，ANDROID下为0-1048575
                   nodeType; // 生成方式占用2位
        }

        /// <summary>
        /// 从ID获取生成ID的时间
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>生成ID的时间</returns>
        public static DateTime GetTimeFromID(long id)
        {
            return MIN_DATE_TIME.AddMilliseconds((id >> TIME_STAMP_SHIFT) + TWEPOCH);
        }

        /// <summary>
        /// 从ID获取节点编号
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>节点编号</returns>
        public static int GetMachineNodeFromID(long id)
        {
            return (int)(id >> NODE_SHIFT) & MAX_NODE;
        }

        /// <summary>
        /// 从ID获取节点类型
        /// </summary>
        /// <param name="id">ID</param>
        /// <returns>节点类型</returns>
        public static long GetMethodFromID(long id)
        {
            return id & MAX_NODE_TYPE;
        }

        static IDGenerator()
        {
            nodeType = Convert.ToUInt32(ConfigManager.Configuration.GetSection("NodeType").Value);
            node = Convert.ToUInt32(ConfigManager.Configuration.GetSection("Node").Value);
        }
    }
}
