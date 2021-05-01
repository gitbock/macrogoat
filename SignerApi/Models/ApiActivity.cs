using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SignerApi.Models
{
    /// <summary>
    /// Object holding API result and also used for logging in DB
    /// Properties are filterd by annotations of JSON Serializer so
    /// that not all Properties are returned to user as web result.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ApiActivity
    {
        //statics
        public static class ApiOperation
        {
            public static string Sign { get { return "sign"; } }
            public static string RequestSigning { get { return "request_sign"; } }
            public static string Verify { get { return "verify"; } }
            public static string Status { get { return "status"; } }
            public static string Download { get { return "download"; } }
        }
        
        public static class ApiStatus
        {
            public static string Success { get { return "success"; } }
            public static string Error { get { return "error"; } }
            public static string InProgress { get { return "in progress"; } }
            public static string Queued { get { return "queued for analysis"; } }
        }


        [Key]
        public int Key { get; set; }

        [JsonProperty]
        public DateTime Timestamp { get; set; }

        [JsonProperty]
        public string Operation { get; set; }

        [JsonProperty]
        //end result of operation
        public string Status { get; set; }

        //Random Key (e.g. for Download mapping to SystemFilename)
        public string UniqueKey { get; set; }

        //Filename in Filesystem saved with unique filename
        public string SystemOfficeFilename { get; set; }

        public string SystemCertFilename { get; set; }

        [JsonProperty]
        public string Message { get; set; }

        [JsonProperty]
        public string FileHash { get; set; }

        [JsonProperty]
        public string CertHash { get; set; }
        
        [JsonProperty]
        public string CertIssuedTo { get; set; }

        [JsonProperty]
        public string CertIssuedBy { get; set; }

        [JsonProperty]
        public string CertExpire { get; set; }

        public string ClientIPAddress { get; set; }

        //Raw filenames as supplied by user 
        public string UserOfficeFilename { get; set; }
        public string UserCertFilename { get; set; }

        [JsonProperty]
        public string DownloadUrl { get; set; }

        [JsonProperty]
        public string StatusUrl { get; set; }

        public string EncCertPw { get; set; }


        public ApiActivity()
        {
            this.Timestamp = DateTime.Now;
            this.UniqueKey = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Gets a secure subset of Properties to be shown to user.
        /// subset of properties is controlled by json annotators
        /// </summary>
        /// <returns>string with properties of Activity</returns>
        public string getWebresult()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder();
            s.Append($"result={Status} | ");
            s.Append($"Message={Message} | ");
            s.Append($"Operation={Operation} | ");
            s.Append($"UniqueKey={UniqueKey} | ");
            s.Append($"UserOfficeFilename={UserOfficeFilename} | ");
            s.Append($"SystemOfficeFilename={SystemOfficeFilename} | ");
            s.Append($"UserCertFilename={UserCertFilename} | ");
            s.Append($"SystemCertFilename={SystemCertFilename}.");
            return s.ToString();
        }

    }

    
}
