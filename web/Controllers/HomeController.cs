using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using SZSOFT.Redis.Helper;

namespace web.Controllers
{
    public class HomeController : Controller
    {

        public ActionResult Index()
        {
            string key = "teststringkey";
            var redis = RedisHelper.StringService; 
            int testNumber = 10000;
            for (int i = 0; i < testNumber; i++)
            {
                redis.StringSet(key+i.ToString(), new UserModel { Id = 1, UserName = $"wdj{i}号" },TimeSpan.FromMinutes(2));

                //redis.KeyDelete(key + i.ToString());
                //redis.StringSet(key, "wdj");
            }

            var redislist = RedisHelper.ListService;
            key = "List_TestKey";
            
                Task.Factory.StartNew(() =>
                {
                    for (var i = 0; i < testNumber; i++)
                    {
                       if( redislist.KeyExists(key))
                        {
                            UserModel s = redislist.ListLeftPop<UserModel>(key);
                        }                       
                    }
                });                
            
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
    /// <summary>
    /// 测试用户实体模型
    /// </summary>
    public class UserModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
    }
}