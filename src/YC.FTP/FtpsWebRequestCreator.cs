using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp
{
    internal class FtpsWebRequestCreator : IWebRequestCreate
    {
        public WebRequest Create(Uri uri)
        {
            var absoluteUri = uri.AbsoluteUri.Remove(3, 1);
            var request = WebRequest.Create(absoluteUri) as FtpWebRequest;
            request.EnableSsl = true;

            return request;
        }
    }
}
