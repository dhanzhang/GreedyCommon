using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
namespace HttpClient
{
    public abstract class Post
    {
        const string BOUNDARY = "----WebKitFormBoundarymwt82GuALqhz9B6K";
        const string NEW_LINE = "\r\n";
        /// <summary>
        /// Http.POST 模拟器,不能上传文件
        /// </summary>
        /// <param name="url">地址</param>
        /// <param name="nv">键值对</param>
        /// <returns></returns>
        public static ExecuteResult<string> Execute(string url, System.Collections.Specialized.NameValueCollection nv)
        {
            ExecuteResult<String> er = null;
            try
            {
                // Create a http request to the server endpoint that will pick up the
                // file and file description.
                HttpWebRequest requestToServerEndpoint = (HttpWebRequest)WebRequest.Create(url);
                requestToServerEndpoint.Method = WebRequestMethods.Http.Post;
                requestToServerEndpoint.ContentType = "multipart/form-data; boundary=" + BOUNDARY;
                requestToServerEndpoint.KeepAlive = true;
                requestToServerEndpoint.Credentials = System.Net.CredentialCache.DefaultCredentials;
                using (var postDataStream = new MemoryStream())
                using (var sw = new StreamWriter(postDataStream))
                {

                    foreach (string name in nv.Keys)
                    {
                        sw.Write(string.Format("--{0}{1}", BOUNDARY, NEW_LINE));
                        sw.Write(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}", name, NEW_LINE));
                        sw.Write(NEW_LINE);
                        sw.Write(string.Format("{0}{1}", nv[name], NEW_LINE));
                    }
                    sw.Write(string.Format("--{0}--", BOUNDARY));
                    sw.Flush();
                    requestToServerEndpoint.ContentLength = postDataStream.Length;
                    using (Stream s = requestToServerEndpoint.GetRequestStream())
                    {
                        postDataStream.WriteTo(s);
                    }
                    postDataStream.Close();
                }
                var res = string.Empty;
                var respons = (HttpWebResponse)requestToServerEndpoint.GetResponse();
                using (var sr = new StreamReader(respons.GetResponseStream()))
                {
                    res = sr.ReadToEnd();
                    sr.Close();
                }

                return new ExecuteResult<string>(res);

            }
            catch (System.Exception ex)
            {
                er = new ExecuteResult<string>(ex);
            }
            return er;
        }
        /// <summary>
        /// Http.POST 模拟器,上传文件
        /// </summary>
        /// <param name="address">地址</param>
        /// <param name="values">键值对</param>
        /// <param name="files">待上传文件</param>
        /// <returns></returns>
        public static ExecuteResult<string> Execute(string address, System.Collections.Specialized.NameValueCollection values, IEnumerable<UploadFile> files)
        {
            var request = WebRequest.Create(address);
            request.Method = "POST";

            request.ContentType = "multipart/form-data; boundary=" + BOUNDARY;
            var boundary = "--" + BOUNDARY;
            using (var postDataStream = new MemoryStream())
            {
                // Write the values
                foreach (string name in values.Keys)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    postDataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                    postDataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                    postDataStream.Write(buffer, 0, buffer.Length);
                }
                // Write the files
                foreach (var file in files)
                {
                    var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                    postDataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"{2}", file.Name, file.Filename, Environment.NewLine));
                    postDataStream.Write(buffer, 0, buffer.Length);
                    buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", file.ContentType, Environment.NewLine));
                    postDataStream.Write(buffer, 0, buffer.Length);
                    file.Stream.CopyTo(postDataStream);
                    buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                    postDataStream.Write(buffer, 0, buffer.Length);
                }
                var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                postDataStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
                request.ContentLength = postDataStream.Length;
                using (Stream s = request.GetRequestStream())
                {
                    postDataStream.WriteTo(s);
                }
                postDataStream.Close();

            }
            var res = string.Empty;

            using (var response = request.GetResponse())
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                res = sr.ReadToEnd();
                sr.Close();
            }
            return new ExecuteResult<string>(res);


        }
    }
}
