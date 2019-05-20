using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Mail;


namespace TT2_API.Services
{
    public class MailService
    {
        private string gmail_account = "turtalk1111@gmail.com"; //Gmail帳號
        private string gmail_password = "Asdf789+"; //Gmail密碼
        //private string gmail_password = "cwxfqaniotcoftew"; //Gmail密碼
        private string gmail_mail = "turtalk1111@gmail.com"; //Gmail信箱

        #region 寄會員驗證信
        //產生驗證碼
        public string GetValidateCode()
        {
            return MainHelper.GetRandomStr(20);
        }

        //將使用者資料填入驗證信範本中
        public string GetRegisterMailBody(string TempString, string UserName, string ValidateUrl)
        {
            TempString = TempString.Replace("{{UserName}}", UserName);
            TempString = TempString.Replace("{{ValidateUrl}}", ValidateUrl);

            return TempString;
        }

        //寄驗證信
        public void SendRegisterMail(string MailBody, string ToEmail)
        {
            //建立寄信用Smtp物件
            SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");
            SmtpServer.Port = 25;//587; 
            SmtpServer.Credentials = new System.Net.NetworkCredential(gmail_account, gmail_password);
            SmtpServer.EnableSsl = true; //開啟SSL            

            //信件
            MailMessage mail = new MailMessage(); 
            mail.From = new MailAddress(gmail_mail); //來源信箱
            mail.To.Add(ToEmail); //收信者
            mail.Subject = "TurTalk-會員註冊確認信"; //主旨
            mail.Body = MailBody; //設定信件內容 
            mail.IsBodyHtml = true; //設定信件內容為HTML格式 

            SmtpServer.Send(mail); //送出信件
        }
        #endregion
    }
}