using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ToolHelper
{
    public class RedisManager
    {
        private static IConnectionMultiplexer _connMultiplexer;
        private static string ConnectionName;
        public static Dictionary<string, RedisConfig> RedisConfigs;

        static RedisManager()
        {
            RedisConfigs = new Dictionary<string, RedisConfig>();
            LoadConfig();
        }
        /// <summary>
        /// 获取连接信息
        /// </summary>
        private static void LoadConfig()
        {
            XmlDocument xml = new XmlDocument();
            xml.Load(AppDomain.CurrentDomain.BaseDirectory + "/Configs/Redis.config");
            XmlNodeList xmlNodes = xml.SelectSingleNode("configuration").SelectSingleNode("redisConfigs").ChildNodes;
            foreach (XmlNode item in xmlNodes)
            {
                RedisConfig config = new RedisConfig()
                {
                    Configname = item.Attributes["configname"].Value,
                    Connection = item.Attributes["connection"].Value,
                    DefaultDatabase = Convert.ToInt32(item.Attributes["defaultDatabase"].Value),
                    InstanceName = item.Attributes["instanceName"].Value
                };
                RedisConfigs.Add(config.Configname, config);
            }
        }

        public static ConfigurationOptions Options
        {
            get
            {
                var options = ConfigurationOptions.Parse(RedisConfigs[ConnectionName].Connection);
                options.AbortOnConnectFail = false;
                options.ConnectTimeout = 1000;//连接操作超时（ms）
                options.SyncTimeout = 1000;//时间（ms）允许进行同步操作
                options.ConnectRetry = 3;//重试连接的次数
                return options;
            }
        }

        /// <summary>
        /// 设置redis中的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        private static string GetKeyForRedis(string key)
        {
            return RedisConfigs[ConnectionName].InstanceName + key;
        }

        /// <summary>
        /// 写入带有过期时间的Redis缓存信息
        /// </summary>
        /// <param name="connName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiresTime"></param>
        /// <returns></returns>
        public static bool Set(string connName, string key, object value, DateTime expiresTime)
        {
            ConnectionName = connName;
            using (var client = ConnectionMultiplexer.Connect(Options))
            {
                TimeSpan time = expiresTime - DateTime.Now;
                if (string.IsNullOrEmpty(key))
                {
                    return false;
                }

                // JsonConvert.DeserializeObject<T>(value);


                // return client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringSet(GetKeyForRedis(key),JsonHelper.ObjectToJson(value), time);


                return client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringSet(GetKeyForRedis(key), JsonConvert.SerializeObject(value), time);

            }
        }

        /// <summary>
        /// 写入不带缓存时间的Redis缓存信息
        /// </summary>
        /// <param name="connName"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Set(string connName, string key, object value)
        {
            ConnectionName = connName;
            using (var client = ConnectionMultiplexer.Connect(Options))
            {
                if (string.IsNullOrEmpty(key))
                {
                    return false;
                }
                //return client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringSet(GetKeyForRedis(key), JsonHelper.ObjectToJson(value));
                return client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringSet(GetKeyForRedis(key), JsonConvert.SerializeObject(value));
            }
        }

        /// <summary>
        /// 获取指定对象的缓存信息
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="connName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T GetValue<T>(string connName, string key) where T : class
        {
            ConnectionName = connName;
            using (var client = ConnectionMultiplexer.Connect(Options))
            {
                if (string.IsNullOrEmpty(key))
                {
                    return default(T);
                }
                string value = client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringGet(GetKeyForRedis(key));
                if (string.IsNullOrEmpty(value))
                {
                    return default(T);
                }
                return JsonConvert.DeserializeObject<T>(value);
            }
        }

        /// <summary>
        /// 获取缓存字符串
        /// </summary>
        /// <param name="connName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetValue(string connName, string key)
        {
            ConnectionName = connName;
            using (var client = ConnectionMultiplexer.Connect(Options))
            {
                if (string.IsNullOrEmpty(key))
                {
                    return string.Empty;
                }
                string value = client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).StringGet(GetKeyForRedis(key));
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }
                return value;
            }
        }

        /// <summary>
        /// 按照key删除指定的缓存信息
        /// </summary>
        /// <param name="connName"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Remove(string connName, string key)
        {
            ConnectionName = connName;
            using (var client = ConnectionMultiplexer.Connect(Options))
            {
                return client.GetDatabase(RedisConfigs[ConnectionName].DefaultDatabase).KeyDelete(GetKeyForRedis(key));
            }
        }


        public static async System.Threading.Tasks.Task RemoveByKeyExAsync(string conName, string keyEx)
        {
            var conn = await ConnectionMultiplexer.ConnectAsync(RedisConfigs[conName].Connection);
            //获取db
            var db = conn.GetDatabase(RedisConfigs[conName].DefaultDatabase);
            //遍历集群内服务器
            foreach (var endPoint in conn.GetEndPoints())
            {
                //获取指定服务器
                var server = conn.GetServer(endPoint);
                //在指定服务器上使用 keys 或者 scan 命令来遍历key
                foreach (var key in server.Keys(RedisConfigs[conName].DefaultDatabase))
                {
                    string startKey = GetKeyForRedis(keyEx);
                    string redisKey = key.ToString();
                    //获取key对于的值
                    if (redisKey.StartsWith(startKey))
                    {
                        db.KeyDelete(redisKey);
                    }

                }
            }

            conn.Dispose();
        }


        public static List<string> GetAllKeysByKeyPrefix(string conName, string keyEx)
        {
            ConnectionName = conName;
            List<string> keysList = new List<string>();

            var conn = ConnectionMultiplexer.Connect(RedisConfigs[ConnectionName].Connection);

            //遍历集群内服务器
            foreach (var endPoint in conn.GetEndPoints())
            {
                //获取指定服务器
                var server = conn.GetServer(endPoint);
                //在指定服务器上使用 keys 或者 scan 命令来遍历key
                foreach (var key in server.Keys(RedisConfigs[ConnectionName].DefaultDatabase))
                {
                    string startKey = GetKeyForRedis(keyEx);
                    string redisKey = key.ToString();
                    //获取key对于的值
                    if (redisKey.StartsWith(startKey))
                    {
                        keysList.Add(redisKey);
                    }

                }
            }
            conn.Dispose();
            return keysList;

        }


        public static string GetKeysByOpenID(string conName, string openid)
        {

            List<string> keysList = new List<string>();

            var conn = ConnectionMultiplexer.Connect(RedisConfigs[conName].Connection);

            //遍历集群内服务器
            foreach (var endPoint in conn.GetEndPoints())
            {
                //获取指定服务器
                var server = conn.GetServer(endPoint);
                //在指定服务器上使用 keys 或者 scan 命令来遍历key
                foreach (var key in server.Keys(RedisConfigs[conName].DefaultDatabase))
                {

                    string redisKey = key.ToString();
                    //获取key对于的值
                    if (redisKey.Contains(openid))
                    {
                        conn.Dispose();
                        return redisKey;
                    }

                }
            }
            conn.Dispose();
            return "";

        }

    }

    /// <summary>
    /// Redis配置
    /// </summary>
    public class RedisConfig
    {
        /// <summary>
        /// 配置名称
        /// </summary>
        public string Configname { get; set; }

        /// <summary>
        /// 连接地址
        /// </summary>
        public string Connection { get; set; }

        /// <summary>
        /// 连接库索引
        /// </summary>
        public int DefaultDatabase { get; set; }

        /// <summary>
        /// 前缀
        /// </summary>
        public string InstanceName { get; set; }
    }
}
