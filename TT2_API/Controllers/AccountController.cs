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
using MyForum.Services;
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

    //API
    [RoutePrefix("api/account")]
    public class AccountController : ApiController
    {
        private ChatDBEntities2 db = new ChatDBEntities2();
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
            acc.PermanentAccount.password = HashPassword(regData.password); //將密碼Hash過
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
            string baseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority);
            string[] paths = { "api", "account", acc.id.ToString(), acc.PermanentAccount.auth_code };
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
                else if (r[0].password == HashPassword(data.password))
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




        //GET api/account/test
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


        #region Hash密碼
        private string HashPassword(string Password)
        {
            //加鹽
            string salt = "AEBG4UeghhFFc7ApFaHQa593N4hwvv5f";
            string saltAndPassword = String.Concat(salt, Password);

            //SHA1
            SHA1CryptoServiceProvider sha1Hasher = new SHA1CryptoServiceProvider();

            byte[] PasswordData = Encoding.Default.GetBytes(saltAndPassword);//換成byte資料
            byte[] HashDate = sha1Hasher.ComputeHash(PasswordData);

            string Hashresult = ""; //換回string資料
            for (int i = 0; i < HashDate.Length; i++)
            {
                Hashresult += HashDate[i].ToString("x2");
            }

            return Hashresult;
        }
        #endregion


        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
