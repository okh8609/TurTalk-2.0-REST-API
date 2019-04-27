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

namespace TT2_API.Controllers
{
    public class AccountRegData
    {
        public string email { get; set; }
        public string password { get; set; }
        public string name { get; set; }
        //public HttpPostedFileBase picture_path_client { get; set; }
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
            //先檢查信箱是否註冊過
            if (AccountCheck(regData.email))
                return false;

            //建立新的使用者
            Account newAccount = new Account();
            newAccount.type = "p";
            newAccount.PermanentAccount = new PermanentAccount();
            newAccount.PermanentAccount.email = regData.email;
            newAccount.PermanentAccount.password = HashPassword(regData.password); //將密碼Hash過
            newAccount.PermanentAccount.name = regData.name;
            newAccount.PermanentAccount.picture_path = null; //照片上傳還沒做
            newAccount.PermanentAccount.ipv4 = System.Web.HttpContext.Current.Request.UserHostAddress;
            newAccount.PermanentAccount.is_admin = false; //預設皆不為管理員
            newAccount.PermanentAccount.auth_code = mailService.GetValidateCode();
            try
            {
                //db.PermanentAccount.Add(newAccount.PermanentAccount);
                db.Account.Add(newAccount);
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
            string[] paths = { "api", "account", newAccount.id.ToString(), newAccount.PermanentAccount.auth_code };
            UriBuilder ValidateUrl = new UriBuilder(baseUrl) { Path = Path.Combine(paths) };
            //藉由Service將使用者資料填入驗證信範本中
            string MailBody =
                mailService.GetRegisterMailBody(TempMail
                    , newAccount.PermanentAccount.name
                    , ValidateUrl.ToString().Replace("%3F", "?"));
            //呼叫Service寄出驗證信
            mailService.SendRegisterMail(MailBody, newAccount.PermanentAccount.email);


            return true;
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

        #region 帳號註冊重複確認
        public bool AccountCheck(string mail)
        {
            var Search = db.PermanentAccount.Where(a => a.email == mail);
            return Search.Count() != 0; //true代表已經註冊過
        }
        #endregion

        // GET api/values
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET api/values/5
        public string Get(int id)
        {
            return "value";
        }

        // POST api/values
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        public void Delete(int id)
        {
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
