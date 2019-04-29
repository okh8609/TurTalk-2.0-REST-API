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
    public class ChatMsgData1
    {
        public int uid { get; set; } //Recipient UID
        public string message { get; set; }
    }

    public class ChatMsgData2
    {
        public DateTime start { get; set; } //取出從什麼時候開始的資料（在app端應該會記錄著上次存取的時間(要從DB獲得，才會一致)）
        public int uid { get; set; }
    }

    public class ChatMsgData3
    {
        public DateTime time { get; set; } //每則訊息的時間，原則上將上述之最後存取時間設為最後一筆資料的時間
        public string message { get; set; }
    }

    //此Controller在發送和接收一般的訊息
    [RoutePrefix("api/chat")]
    public class ChatNormalController : ApiController
    {
        private ChatDBEntities db = new ChatDBEntities();


        //POST /api/chat/send/
        [HttpPost]
        [JwtAuth]
        [Route("send")]
        public bool Send([FromBody]ChatMsgData1 data)
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            ChatMsg c = new ChatMsg();
            c.uid_from = myUid;
            c.uid_to = data.uid;
            c.msg = data.message;
            c.time = DateTime.UtcNow;
            try
            {
                db.ChatMsg.Add(c);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //POST /api/chat/receive/
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

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
