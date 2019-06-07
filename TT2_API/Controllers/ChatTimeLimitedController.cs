using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TT2_API.Filters;
using TT2_API.Models;
using System.Data.Entity;

namespace TT2_API.Controllers
{
    public class ChatMsgData1_TimeLimited
    {
        public int uid { get; set; } //Recipient UID
        public string message { get; set; }
        public TimeSpan eff_period { get; set; } //訊息的有效時限
    }

    public class ChatMsgData2_TimeLimited
    {
        public int uid { get; set; } //送出者的UID
        public DateTime LFt { get; set; } // Last fetch time (取處的資料會從該時間之後開始)
    }

    public class ChatMsgData3_TimeLimited
    {
        public DateTime time { get; set; } //訊息送到的時間
        public DateTime exp { get; set; } //訊息到期時間
        public bool isMyMsg { get; set; } //這筆訊息是我送出的
        public string msg { get; set; }
    }

    public class ChatMsgData3_TimeLimited_with_UTCtime
    {
        public DateTime UTCtime { get; set; } //存取資料的時間點
        public IEnumerable<ChatMsgData3_TimeLimited> chatMsgs { get; set; }
    }

    public class ChatMsgData4_TimeLimited //幾封未讀訊息(通知用)
    {
        public int uid { get; set; }
        public string name { get; set; }
        public int count { get; set; }
    }

    //此Controller在發送和接收限時聊天的訊息
    [RoutePrefix("api/chat2")]
    public class ChatTimeLimitedController : ApiController
    {
        private ChatDBEntities db = new ChatDBEntities();

        //POST /api/chat2/send/
        [HttpPost]
        [JwtAuth]
        [Route("send")]
        public bool Send([FromBody]ChatMsgData1_TimeLimited data)
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            ChatMsg_TimeLimited c = new ChatMsg_TimeLimited();
            c.uid_from = myUid;
            c.uid_to = data.uid;
            c.msg = data.message;
            c.time = DateTime.UtcNow;
            c.eff_period = data.eff_period;
            try
            {
                db.ChatMsg_TimeLimited.Add(c);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //POST /api/chat2/receive/
        [HttpPost]
        [JwtAuth]
        [Route("receive")]
        public ChatMsgData3_TimeLimited_with_UTCtime Receive([FromBody] ChatMsgData2_TimeLimited data)
        {
            var nowUTCtime = DateTime.UtcNow;
            int myUid = int.Parse((Request.Properties["user"] as string));

            var r = db.ChatMsg_TimeLimited.Where(a => (DateTime.Compare(a.time, data.LFt) >= 0) &&
                                                      (((a.uid_from == data.uid) && (a.uid_to == myUid)) ||
                                                       ((a.uid_from == myUid) && (a.uid_to == data.uid)))); //把某人的訊息都撈出來

            List<ChatMsgData3_TimeLimited> r2 = new List<ChatMsgData3_TimeLimited>();

            foreach (var i in r)
            {
                var iexp = i.time.Add(i.eff_period);
                if ((DateTime.Compare(iexp, DateTime.UtcNow) >= 0)) // 期限尚有效的資訊
                {
                    ChatMsgData3_TimeLimited t = null;
                    if (i.uid_from == myUid)
                        t = new ChatMsgData3_TimeLimited { exp = iexp, msg = i.msg, time = i.time, isMyMsg = true };
                    else
                        t = new ChatMsgData3_TimeLimited { exp = iexp, msg = i.msg, time = i.time, isMyMsg = false };

                    r2.Add(t);
                }
            }

            r2.Sort((x, y) => { return x.time > y.time ? 1 : -1; }); //依據送出時間由小到大排序

            return new ChatMsgData3_TimeLimited_with_UTCtime { UTCtime = nowUTCtime, chatMsgs = r2 };
        }

        //GET /api/chat2/receive/
        //取得所有資料的數量(通知列表使用)
        [HttpGet]
        [JwtAuth]
        [Route("receive")]
        public IEnumerable<ChatMsgData4_TimeLimited> ReceiveAll()
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            /* var r = from t1 in db.ChatMsg_TimeLimited
                    where t1.uid_to == myUid
                    group t1 by t1.uid_from into g
                    join t2 in db.PermanentAccount on g.Key equals t2.uid
                    select new ChatMsgData4_TimeLimited { name = t2.name, count = g.Count() }; */

            var r = db.ChatMsg_TimeLimited.Where(a => (a.uid_to == myUid)).ToList(); //把「給我的」訊息都撈出來
            List<ChatMsg_TimeLimited> r2 = new List<ChatMsg_TimeLimited>();

            foreach (var i in r)
            {
                var iexp = i.time.Add(i.eff_period);
                if ((DateTime.Compare(iexp, DateTime.UtcNow) >= 0)) // 期限尚有效的資訊
                    r2.Add(i);
            }

            var r3 = from t1 in r2
                     group t1 by t1.uid_from into g
                     join t2 in db.PermanentAccount on g.Key equals t2.uid
                     orderby t2.name
                     select new ChatMsgData4_TimeLimited { name = t2.name, count = g.Count(), uid = g.Key };

            return r3;
        }

        //GET /api/chat2/clear/
        //清除所有過期的訊息
        //應該寫在伺服器的腳本中去呼叫
        //回傳刪除的筆數，-1代表失敗
        [HttpGet]
        [Route("clear")]
        public int Clear()
        {
            try
            {
                //會出錯的查詢
                /*var r = db.ChatMsg_TimeLimited.Where(
                               a => (DateTime.Compare(
                                    (DateTime)DbFunctions.AddSeconds(a.time, (int)(a.eff_period.TotalSeconds)),
                                    DateTime.UtcNow) < 0));*/
                //最後決定讓是否到期的判斷讓應用程式去做

                var r = db.ChatMsg_TimeLimited;
                int count = 0;
                foreach (var i in r)
                {
                    /*
                    var a = i.time.Add(i.eff_period);
                    var b = DateTime.UtcNow;
                    var c = DateTime.UtcNow.ToLocalTime();
                    var d = DateTime.Now; //c和d是一樣的
                    */
                    if ((DateTime.Compare(i.time.Add(i.eff_period), DateTime.UtcNow) < 0))
                    {
                        ++count;
                        db.ChatMsg_TimeLimited.Remove(i);
                    }
                }
                db.SaveChanges();
                return count;
            }
            catch (Exception)
            {
                return -1; // error
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
