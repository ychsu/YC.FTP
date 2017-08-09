using System;
using System.IO;
using System.Net;
using YC.Ftp.Enums;

namespace YC.Ftp
{
    public class FtpClient
    {
        static FtpClient()
        {
            WebRequest.RegisterPrefix("ftps", new FtpsWebRequestCreator());
        }

        internal FtpClient(string basePath, ICredentials credential)
        {
            if ((WebRequest.Create(basePath) is FtpWebRequest) == false)
            {
                throw new NotSupportedException("the URI prefix is not supported.");
            }

            this.BasePath = basePath.TrimEnd('/');
            this.Credentials = credential;
        }

        public string BasePath { get; private set; }

        public ICredentials Credentials { get; set; }

        internal Stream Request(string path, FtpMethod method, Stream requestStream)
        {
            var requestUri = new Uri($"{BasePath}{path}");
            FtpWebRequest request = WebRequest.Create(requestUri) as FtpWebRequest;
            request.Method = typeof(WebRequestMethods.Ftp).GetField(method.ToString()).GetValue(null).ToString();
            if (this.Credentials != null)
            {
                request.Credentials = this.Credentials;
            }
            if (requestStream != null)
            {
                Stream stream = request.GetRequestStream();
                requestStream.CopyTo(stream);
                stream.Close();
            }
            FtpWebResponse response = request.GetResponse() as FtpWebResponse;
            MemoryStream memoryStream = this.ConvertToMemoryStream(response.GetResponseStream());
            response.Close();
            return memoryStream;
        }

        /// <summary>
        /// 轉換Stream為MemoryStream
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        private MemoryStream ConvertToMemoryStream(Stream stream)
        {
            byte[] numArray = new byte[1024];
            int num = 1;
            MemoryStream memoryStream = new MemoryStream();
            while (num > 0)
            {
                num = stream.Read(numArray, 0, (int)numArray.Length);
                memoryStream.Write(numArray, 0, num);
            }
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream;
        }
    }
}
