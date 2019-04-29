using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace TT2_API.Services
{
    public class MainHelper
    {
        // 雜湊簽章會用到的金鑰
        public const string SecretKey = "GXiwDg5MbeJ1XTOYx0NHjPOuuF0QDstcKsVvVBrk";


        //產生隨機亂髮
        //用於 email認證
        //及 邀請密碼產生
        public static string GetRandomStr(int len)
        {
            //設定驗證碼字元的陣列
            string[] Code ={ "A", "B", "C", "D", "E", "F", "G", "H", "I", "J", "K", "L","M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                             "a", "b", "c", "d","e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
                             "1", "2", "3", "4", "5", "6", "7", "8", "9"};
            //驗證碼字串
            string str = string.Empty;

            Random rd = new Random();

            for (int i = 0; i != 8; ++i)
                str += Code[rd.Next(Code.Count())];

            return str;
        }


        #region Hash密碼
        public static string HashPassword(string Password)
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
    }
}