using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TT2_API.Filters;
using TT2_API.Models;

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
        public DateTime start { get; set; } //取出從什麼時候開始的資料（在app端應該會記錄著上次存取的時間(要從DB獲得，才會一致)）
        public int uid { get; set; } //送出者的UID
    }

    public class ChatMsgData3_TimeLimited
    {
        public DateTime time { get; set; } //每則訊息的時間，原則上將上述之最後存取時間設為最後一筆資料的時間
        public string message { get; set; }
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
            c.time = DateTime.Now;
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
        public IEnumerable<ChatMsgData3> Receive([FromBody] ChatMsgData2 data)
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            var r = db.ChatMsg.Where(a => (a.uid_from == data.uid) && (a.uid_to == myUid)//把「給我的」訊息都撈出來
                                            && (DateTime.Compare(a.time, data.start) >= 0))
                                            .OrderBy(a => a.time)
                                            .Select(a => new ChatMsgData3 { time = a.time, message = a.msg });
            return r;
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
                var r = db.ChatMsg_TimeLimited.Where(a => (DateTime.Compare(a.time.Add(a.eff_period), DateTime.Now) < 0));
                db.ChatMsg_TimeLimited.RemoveRange(r);
                db.SaveChanges();
                return r.Count();
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
