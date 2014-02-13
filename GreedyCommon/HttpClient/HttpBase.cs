using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
namespace HttpClient
{
    public abstract class HttpBase
    {
        protected HttpBase()
        {
        }

        #region Async Request
  
        public static Task<string> MakeAsyncHttpGetRequest(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            //request.ContentType = ContentTypeXml;
            request.Method = "GET";

            Task<WebResponse> task = Task.Factory.FromAsync(
                request.BeginGetResponse,
                asyncResult => request.EndGetResponse(asyncResult),
                (object)null);

            return task.ContinueWith(t => ReadStreamFromResponse(t.Result));
        }

        /// <summary>
        /// Read the Stream from Response.
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private static string ReadStreamFromResponse(WebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            using (var sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();
                return strContent;
            }
        }
        #endregion

        protected const string BOUNDARY = "----WebKitFormBoundarymwt82GuALqhz9B6K";
        protected const string USER_AGENT = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
    }
}
