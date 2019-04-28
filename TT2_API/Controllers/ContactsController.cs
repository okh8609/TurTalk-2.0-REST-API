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
    public class ContactsData
    {
        public int uid;
        public string name;
        public string email;
    }

    //此Controller在管理跟聯絡人有關的資訊
    [RoutePrefix("api/contacts")]
    public class ContactsController : ApiController
    {
        private ChatDBEntities db = new ChatDBEntities();

        //POST /api/contacts/add
        //加入聯絡人 
        //將別人的uid加入自己的聯絡人
        //(自己的uid由token中取得)
        [HttpPost]
        [JwtAuth]
        [Route("add")]
        public bool Add([FromBody]int uid)
        {
            //回傳true代表加入成功
            //回傳false代表加入失敗(可能已存在...之類的)

            int myUid = int.Parse((Request.Properties["user"] as string));

            try
            {
                if (db.Contacts.Where(a => (a.uid_from == myUid) && (a.uid_to == uid)).ToList().Count == 0)
                {
                    Contacts cc = new Contacts();

                    cc.uid_from = myUid;
                    cc.uid_to = uid;

                    db.Contacts.Add(cc);
                    db.SaveChanges();

                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {

                return false;
            }
        }

        //刪除聯絡人 POST /api/contacts/del
        //將別人的uid從自己的聯絡人刪除
        //(自己的uid由token中取得)
        [HttpPost]
        [JwtAuth]
        [Route("del")]
        public bool Del([FromBody]int uid)
        {
            //回傳true代表刪除成功
            //回傳false代表刪除失敗

            int myUid = int.Parse((Request.Properties["user"] as string));

            try
            {
                var r = db.Contacts.Where(a => (a.uid_from == myUid) && (a.uid_to == uid));
                if (r.Count() == 0)
                    return false;
                db.Contacts.RemoveRange(r);
                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        //請求聯絡人清單 GET /api/contacts/list/
        //(自己的uid由token中取得)
        [HttpGet]
        [JwtAuth]
        [Route("list")]
        public IEnumerable<ContactsData> List()
        {
            int myUid = int.Parse((Request.Properties["user"] as string));

            try
            {
                var result = (from t1 in db.Contacts
                              where t1.uid_from == myUid
                              join t2 in db.PermanentAccount on t1.uid_to equals t2.uid
                              select new ContactsData { uid = t2.uid, name = t2.name, email = t2.email });
                return result;
            }
            catch
            {
                return null;
            }
        }

        //帳戶搜尋的API
        //GET /api/contacts/search/{keyword}
        [HttpGet]
        [JwtAuth]
        [Route("search/{keyword?}")]
        public IEnumerable<ContactsData> Search(string keyword = "")
        {
            try
            {
                var result = from t in db.PermanentAccount
                              where t.name.Contains(keyword) || t.email.Contains(keyword)
                              select new ContactsData { uid = t.uid, name = t.name, email = t.email };
                return result;
            }
            catch
            {
                return null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }
    }
}
