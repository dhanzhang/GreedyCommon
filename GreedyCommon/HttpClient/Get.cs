using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Security;
using System.Web;
using System.Security.Cryptography;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
namespace HttpClient
{
    public class Get : HttpBase
    {

        protected Get()
            : base()
        {
        }
        /// <summary>
        /// 验证证书
        /// </summary>
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return SslPolicyErrors.None == errors;
        }

        public static ExecuteResult<string> Execute(string addresss, int? timeout, System.Collections.Specialized.NameValueCollection args, CookieCollection cookies)
        {
            ExecuteResult<string> res = null;
            try
            {

                List<string> lst = new List<string>(args.Count); 
                foreach (string key in args.Keys)
                { 
                    lst.Add(string.Format("{0}={1}", key, System.Net.WebUtility.HtmlEncode(args[key]))); 
                }
                var urlAddress = string.Format("{0}?{1}", addresss, string.Join("&", lst)); 
                HttpWebRequest request = WebRequest.Create(urlAddress) as HttpWebRequest;
                if (addresss.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    //对服务端证书进行有效性校验（非第三方权威机构颁发的证书，如自己生成的，不进行验证，这里返回true）
                    ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                    request = WebRequest.Create(urlAddress) as HttpWebRequest;
                    request.ProtocolVersion = HttpVersion.Version10;    //http版本，默认是1.1,这里设置为1.0
                }
                else
                {
                    request = WebRequest.Create(urlAddress) as HttpWebRequest;
                }

                request.Method = "GET";
                request.UserAgent = USER_AGENT;
                if (timeout.HasValue)
                {
                    request.Timeout = timeout.Value;
                }
                if (cookies != null)
                {
                    request.CookieContainer = new CookieContainer();
                    request.CookieContainer.Add(cookies);
                }
                var text = "";
                var response = request.GetResponse() as HttpWebResponse;
                using (var rs = response.GetResponseStream())
                using (var sr = new StreamReader(rs))
                {
                    text = sr.ReadToEnd();
                    sr.Close();
                }
                res = new ExecuteResult<string>(text);

            }
            catch (System.Exception ex)
            {
                res = new ExecuteResult<string>(ex);
            }
            return res;
        }
    }
}
