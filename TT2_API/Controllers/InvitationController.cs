using JWT;
using JWT.Algorithms;
using JWT.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TT2_API.Filters;
using TT2_API.Models;
using TT2_API.Services;

namespace TT2_API.Controllers
{

    public class TempAcco
    {
        public int uid;
        public string pwd; //隨機八字元
    }

    public class TempLoginData
    {
        public int uid { get; set; }
        public string pwd { get; set; }
    }

    public class TempMsgData1
    {
        public int uid { get; set; } //Recipient UID
        public string message { get; set; }
    }

    public class TempMsgData2
    {
        public DateTime start { get; set; } //取出從什麼時候開始的資料（在app端應該會記錄著上次存取的時間(要從DB獲得，才會一致)）
        public int uid { get; set; }
    }

    public class TempMsgData3
    {
        public DateTime time { get; set; } //每則訊息的時間，原則上將上述之最後存取時間設為最後一筆資料的時間
        public string message { get; set; }
    }


    //處理及時邀請功能的API
    //這個算是體驗功能，為避免被濫用，帳號有效時間為accEffTime分鐘
    //超過即無法在存取任何內容
    //但其對話紀錄仍會保存於資料庫
    //會產生一組臨時的uid和pwd
    [RoutePrefix("api/invite")]
    public class InvitationController : ApiController
    {
        private const int accEffTime = 15; //帳號有效時間

        private ChatDBEntities db = new ChatDBEntities();

        #region 帳號部分
        //GET /api/invite/getacc
        //獲得臨時帳號
        [HttpGet]
        [JwtAuth]
        [Route("getacc")]
        public TempAcco GetTempAccount()
        {
            int myUid = int.Parse((Request.Properties["user"] as string));
            string newPwd = GetPassword();

            Account newAcco = new Account();
            newAcco.type = "t";
            newAcco.TemporaryAccount = new TemporaryAccount();
            newAcco.TemporaryAccount.pwd = MainHelper.HashPassword(newPwd);
            newAcco.TemporaryAccount.ancestor_uid = myUid;
            newAcco.TemporaryAccount.reg_time = DateTime.UtcNow;
            newAcco.TemporaryAccount.ipv4 = System.Web.HttpContext.Current.Request.UserHostAddress;

            try
            {
                db.Account.Add(newAcco);
                db.SaveChanges();
                var r = db.Account.Find(myUid).PermanentAccount.TemporaryAccount.OrderByDescending(a => a.reg_time).FirstOrDefault();
                return new TempAcco { uid = r.uid, pwd = newPwd };
            }
            catch (Exception)
            {
                return null;
            }
        }

        // POST api/invite/login
        // 臨時帳號登入的API
        // 此處以POST撰寫 可改用HTTP基本認證的方式進行帳號密碼的傳遞
        [HttpPost]
        [Route("login")]
        public HttpResponseMessage Login([FromBody]TempLoginData data)
        {
            HttpResponseMessage response = null;

            var r = db.TemporaryAccount.Find(data.uid);

            if (DateTime.Compare(r.reg_time.AddMinutes(accEffTime), DateTime.UtcNow) < 0)
            {
                // 臨時帳號accEffTime分鐘內有效
                response = this.Request.CreateResponse<APIResult>(HttpStatusCode.Unauthorized, new APIResult()
                {
                    Success = false,
                    Message = $"",
                    Payload = $"臨時帳號{accEffTime}分鐘內有效"
                });
            }
            else if (r.pwd == MainHelper.HashPassword(data.pwd))
            {
                // 帳號與密碼比對正確，回傳帳密比對正確

                #region 產生這次通過身分驗證的存取權杖 Access Token
                string secretKey = MainHelper.SecretKey;

                // 設定該存取權杖的有效期限
                IDateTimeProvider provider = new UtcDateTimeProvider();
                var expDate = provider.GetNow().AddMinutes(accEffTime); //權杖效期accEffTime分鐘
                var unixEpoch = UnixEpoch.Value; // 1970-01-01 00:00:00 UTC
                var secondsSinceEpoch = Math.Round((expDate - unixEpoch).TotalSeconds);

                //產生Token
                var jwtToken = new JwtBuilder()
                      .WithAlgorithm(new HMACSHA256Algorithm())
                      .WithSecret(secretKey)
                      .AddClaim("iss", r.uid.ToString()) //
                      .AddClaim("exp", secondsSinceEpoch)
                      .AddClaim("role", new string[] { "User", "People", "Guest" })
                      .Build();
                #endregion

                response = this.Request.CreateResponse<APIResult>(HttpStatusCode.OK, new APIResult()
                {
                    Success = true,
                    Message = $"{r.ancestor_uid}", //回傳祖先的ID 這樣才能用哦~
                    Payload = $"{jwtToken}"
                });
            }
            else
            {
                // 密碼錯誤
                response = this.Request.CreateResponse<APIResult>(HttpStatusCode.Unauthorized, new APIResult()
                {
                    Success = false,
                    Message = $"",
                    Payload = "UID或密碼不正確"
                });
            }

            return response;

        }
        #endregion

        #region 聊天部分
        //POST /api/invite/send
        [HttpPost]
        [JwtAuth]
        [Route("send")]
        public bool Send([FromBody]TempMsgData1 data)
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            ChatMsg_Invitation c = new ChatMsg_Invitation();
            c.uid_from = myUid;
            c.uid_to = data.uid;
            c.msg = data.message;
            c.time = DateTime.UtcNow;
            try
            {
                db.ChatMsg_Invitation.Add(c);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //POST /api/invite/receive
        [HttpPost]
        [JwtAuth]
        [Route("receive")]
        public IEnumerable<TempMsgData3> Receive([FromBody] TempMsgData2 data)
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            var r = db.ChatMsg_Invitation.Where(a => (a.uid_from == data.uid) && (a.uid_to == myUid)//把「給我的」訊息都撈出來
                                                        && (DateTime.Compare(a.time, data.start) >= 0))
                                                        .OrderBy(a => a.time)
                                                        .Select(a => new TempMsgData3 { time = a.time, message = a.msg });
            return r;
        }
        #endregion


        private static string GetPassword()
        {
            return MainHelper.GetRandomStr(8);
        }



        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

    }
}
