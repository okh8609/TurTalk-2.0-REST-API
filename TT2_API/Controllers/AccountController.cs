using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TT2_API.Models;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.IO;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using TT2_API.Services;
using TT2_API.Filters;

namespace TT2_API.Controllers
{
    public class AccountRegData
    {
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        //public HttpPostedFileBase picture_path_client { get; set; }
    }

    public class AccountLoginData
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class ChangePasswdData
    {
        public string oldPasswd;
        public string newPasswd;
    }

    public class ProfileData
    {
        public int uid;
        public string email;
        public string name;
        public string auth_code;
    }

    //此Controller在管理跟帳戶有關的資訊
    [RoutePrefix("api/account")]
    public class AccountController : ApiController
    {
        private ChatDBEntities db = new ChatDBEntities();
        private MailService mailService = new MailService();

        // POST api/account/reg
        [HttpPost]
        [Route("reg")]
        public bool Register([FromBody]AccountRegData regData)
        {
            //宣告新的使用者
            Account acc = null;

            //先檢查信箱是否註冊過
            var r = db.PermanentAccount.Where(a => a.email == regData.email).ToList();

            //建立新的使用者
            if (r.Count == 0) //未註冊過
            {
                acc = new Account();
                acc.PermanentAccount = new PermanentAccount();
            }
            else if (r[0].auth_code == null || r[0].auth_code.Trim() == "") //註冊過，已驗證過
            {
                return false; //信箱重複，拒絕註冊
            }
            else
            {
                acc = r[0].Account;
            }

            acc.type = "p";
            acc.PermanentAccount.email = regData.email;
            acc.PermanentAccount.password = MainHelper.HashPassword(regData.password); //將密碼Hash過
            acc.PermanentAccount.name = regData.name;
            acc.PermanentAccount.picture_path = null; //照片上傳還沒做
            acc.PermanentAccount.ipv4 = System.Web.HttpContext.Current.Request.UserHostAddress;
            acc.PermanentAccount.is_admin = false; //預設皆不為管理員
            acc.PermanentAccount.auth_code = mailService.GetValidateCode();
            try
            {
                //db.PermanentAccount.Add(newAccount.PermanentAccount);

                if (r.Count == 0) //未註冊過，才須新增（否則就只是修改而已）
                    db.Account.Add(acc);

                db.SaveChanges();
            }
            catch
            {
                return false;
            }

            //寄發驗證信:
            //取得寫好的驗證信範本內容
            string TempMail = System.IO.File.ReadAllText(
                System.Web.Hosting.HostingEnvironment.MapPath("~/Views/Shared/RegisterEmailTemplate.html"));
            //宣告Email驗證用的Url
            //string baseUrl = @"https://kaihao.tw:60089/";
            string baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            string[] paths = { "api", "account", "verify", acc.id.ToString(), acc.PermanentAccount.auth_code };
            UriBuilder ValidateUrl = new UriBuilder(baseUrl) { Path = Path.Combine(paths) };
            //藉由Service將使用者資料填入驗證信範本中
            string MailBody =
                mailService.GetRegisterMailBody(TempMail
                    , acc.PermanentAccount.name
                    , ValidateUrl.ToString().Replace("%3F", "?"));
            //呼叫Service寄出驗證信
            mailService.SendRegisterMail(MailBody, acc.PermanentAccount.email);


            return true;
        }

        // POST api/account/login
        // 此處已POST撰寫 可改用HTTP基本認證的方式進行帳號密碼的傳遞
        [HttpPost]
        [Route("login")]
        public HttpResponseMessage Login([FromBody]AccountLoginData data)
        {
            HttpResponseMessage response = null;

            //檢查帳號與密碼是否正確
            var r = db.PermanentAccount.Where(a => a.email == data.email).ToList();

            if (r.Count == 0)
            {
                // 帳號錯誤
                response = this.Request.CreateResponse<APIResult>(HttpStatusCode.Unauthorized, new APIResult()
                {
                    Success = false,
                    Message = $"",
                    Payload = "帳號或密碼不正確"
                });
            }
            else
            {
                if (r[0].auth_code != null && r[0].auth_code.Trim() != "")
                {
                    // 帳號尚未驗證
                    response = this.Request.CreateResponse<APIResult>(HttpStatusCode.Unauthorized, new APIResult()
                    {
                        Success = false,
                        Message = $"",
                        Payload = "帳號尚未通過電子郵件驗證"
                    });
                }
                else if (r[0].password == MainHelper.HashPassword(data.password))
                {
                    // 帳號與密碼比對正確，回傳帳密比對正確

                    #region 產生這次通過身分驗證的存取權杖 Access Token
                    string secretKey = MainHelper.SecretKey;

                    // 設定該存取權杖的有效期限
                    IDateTimeProvider provider = new UtcDateTimeProvider();
                    var expDate = provider.GetNow().AddDays(7); //權杖效期7天
                    var unixEpoch = UnixEpoch.Value; // 1970-01-01 00:00:00 UTC
                    var secondsSinceEpoch = Math.Round((expDate - unixEpoch).TotalSeconds);

                    //產生Token
                    var jwtToken = new JwtBuilder()
                          .WithAlgorithm(new HMACSHA256Algorithm())
                          .WithSecret(secretKey)
                          .AddClaim("iss", r[0].uid.ToString()) //
                          .AddClaim("exp", secondsSinceEpoch)
                          .AddClaim("role", new string[] { "User", "People" })
                          .Build();
                    #endregion

                    response = this.Request.CreateResponse<APIResult>(HttpStatusCode.OK, new APIResult()
                    {
                        Success = true,
                        Message = $"帳號:{r[0].email} / 密碼:{r[0].password}",
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
                        Payload = "帳號或密碼不正確"
                    });
                }
            }

            return response;

        }


        //GET api/account/{uid}/{authcode}
        //電子郵件驗證
        [HttpGet]
        [Route("verify/{uid:int}/{authCode}")]
        public string Verify(int uid, string authCode)
        {
            var r = db.Account.Find(uid);

            if (r != null)
            {
                if (r.PermanentAccount.auth_code == null || r.PermanentAccount.auth_code.Trim() == "")
                {
                    return "已經驗證";
                }
                else if (r.PermanentAccount.auth_code.Trim() == authCode)
                {
                    r.PermanentAccount.auth_code = "";
                    db.SaveChanges();
                    return "驗證成功";
                }
                else
                {
                    return "驗證失敗";
                }
            }
            return "尚未註冊";
        }


        //GET api/account/test
        //測試權杖是否有效
        [HttpGet]
        [JwtAuth]
        [Route("test")]
        public APIResult Test()
        {
            int uid = int.Parse((Request.Properties["user"] as string));

            return new APIResult()
            {
                Success = true,
                Message = $"授權使用者uid為= {uid}",
                Payload = new string[] { "有提供存取權杖!!", "有提供存取權杖~~" }
            };
        }

        //GET api/account/profile
        //取得個人檔案
        [HttpGet]
        [JwtAuth]
        [Route("profile")]
        public ProfileData GetProfile()
        {
            int myUID = int.Parse((Request.Properties["user"] as string));

            var r = db.PermanentAccount.Find(myUID);

            ProfileData a = new ProfileData();
            a.uid = r.uid;
            a.email = r.email;
            a.name = r.name;
            a.auth_code = r.auth_code;

            return a;
        }

        //POST /api/account/change/password/{oldPasswd}/{newPasswd}
        //修改密碼
        [HttpPost]
        [JwtAuth]
        [Route("change/passwd")]
        public bool ChangePasswd([FromBody]ChangePasswdData data)
        {
            int myUID = int.Parse((Request.Properties["user"] as string));

            var r = db.PermanentAccount.Find(myUID);

            if (r.password == MainHelper.HashPassword(data.oldPasswd))
            {
                r.password = MainHelper.HashPassword(data.newPasswd);
                try
                {
                    db.SaveChanges();
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }


        //POST /api/account/change/name/{newname}
        //修改帳戶名稱
        [HttpPost]
        [JwtAuth]
        [Route("change/name")]
        public bool ChangePasswd([FromBody]string newName)
        {
            if (newName == null)
                return false;

            int myUID = int.Parse((Request.Properties["user"] as string));

            var r = db.PermanentAccount.Find(myUID);

            r.name = newName;

            try
            {
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //GET api/account/password/{pwd}
        //電子郵件驗證
        [HttpGet]
        [Route("password/{pwd}")]
        public string HashPassword(string pwd)
        {
            return MainHelper.HashPassword(pwd);
        }

        //缺修改帳戶資料的API
        //POST /api/account/change/picture/{pict_path}

        //缺登出API






        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
