using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
namespace ToolHelper
{
    public class Class1
    {

        public void set(string value)
        {



            //step-1: 设置Redis链接
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            //ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("server1:6379,server2:6379");
            string config = redis.Configuration;

            //step-2: Accessing a redis database 连接到Redis数据库
            IDatabase db = redis.GetDatabase();

            //step-3: 通过db使用Redis API （http://redis.io/commands）
            db.StringSet("mykey", "myvalue", new TimeSpan(0, 10, 0), When.Always, CommandFlags.None);
            string value1 = string.Empty;
            if (db.KeyExists("mykey"))
            {
                value1 = db.StringGet("mykey");
            }


        }

        private void test2()
        {
            //step-1: 设置Redis链接
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            //step-2: 创建连接到特定服务的 PUB/SUB 连接
            ISubscriber sub = redis.GetSubscriber();
            //step-3: 订阅频道，并处于监听状态，接受消息并处理
            string result = string.Empty;
            sub.Subscribe("messages", (channel, message) => {
                result = string.Format("Channel:{0} ; Message:{1} .", channel.ToString(), message);
            });
        }
        private void test3()
        {
            //step-1: 设置Redis链接
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost");
            //step-2: 创建连接到特定服务的 PUB/SUB 连接
            ISubscriber sub = redis.GetSubscriber();
            //step-3: 在另一个进程或是机器上，发布消息
            sub.Publish("messages", "hello");
        }

    }
}
