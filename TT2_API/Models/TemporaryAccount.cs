//------------------------------------------------------------------------------
// <auto-generated>
//     這個程式碼是由範本產生。
//
//     對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//     如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace TT2_API.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class TemporaryAccount
    {
        public int uid { get; set; }
        public string pwd { get; set; }
        public int ancestor_uid { get; set; }
        public System.DateTime reg_time { get; set; }
        public string ipv4 { get; set; }
    
        public virtual Account Account { get; set; }
        public virtual PermanentAccount PermanentAccount { get; set; }
    }
}
