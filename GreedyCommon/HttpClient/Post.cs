using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
namespace HttpClient
{
    public class Post : HttpBase
    {

        protected Post()
            : base()
        {
        }


        protected static void Settings(HttpWebRequest request)
        {
            request.Method = WebRequestMethods.Http.Post;
            request.ContentType = string.Format("multipart/form-data; boundary={0}", BOUNDARY);
            request.KeepAlive = true;
            request.ProtocolVersion = HttpVersion.Version10;

        }

        protected static void WriteData(HttpWebRequest request, System.Collections.Specialized.NameValueCollection values, IEnumerable<UploadFile> files)
        {
            var boundary = "--" + BOUNDARY;
            using (var postDataStream = new MemoryStream())
            {
                if (values != null)
                {
                    foreach (string name in values.Keys)
                    {
                        var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                        postDataStream.Write(buffer, 0, buffer.Length);
                        buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"{0}\"{1}{1}", name, Environment.NewLine));
                        postDataStream.Write(buffer, 0, buffer.Length);
                        buffer = Encoding.UTF8.GetBytes(values[name] + Environment.NewLine);
                        postDataStream.Write(buffer, 0, buffer.Length);
                    }
                }
                if (files != null && files.Count() > 0)
                {
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

        }

        protected static ExecuteResult<string> ReadData(HttpWebRequest request)
        {
            ExecuteResult<string> res = null;
            using (var response = (HttpWebResponse)request.GetResponse())

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using (var sr = new StreamReader(response.GetResponseStream()))
                    {

                        res = new ExecuteResult<string>(sr.ReadToEnd());
                        sr.Close();

                    }
                }
                else
                {

                    res = new ExecuteResult<string>()
                    {
                        IsOk = false,
                        Message = response.StatusDescription
                    };
                }

            return res;
        }

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
                HttpWebRequest requestToServerEndpoint = (HttpWebRequest)WebRequest.Create(url);
                Settings(requestToServerEndpoint);
                WriteData(requestToServerEndpoint, nv, null);

                return ReadData(requestToServerEndpoint);



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
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(address);
                Settings(request);
                WriteData(request, values, files);
                return ReadData(request);

            }
            catch (System.Exception ex)
            {
                return new ExecuteResult<string>(ex);
            }

        }
    }
}
