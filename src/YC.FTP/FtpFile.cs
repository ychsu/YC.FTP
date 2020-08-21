using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp
{
    public class FtpFile : FtpItem
    {
        internal FtpFile(FtpClient client, string fullName, Encoding encoding)
            : base(client, fullName, encoding)
        {

        }

        public FtpFile(string path, ICredentials credential)
            : base(path, credential)
        {

        }

        public Stream OpenRead()
        {
            return new FtpStream(base.Client, this.FullName, FileAccess.Read);
        }

        public Stream OpenWrite()
        {
            return new FtpStream(base.Client, this.FullName, FileAccess.Write);
        }

        public Stream OpenAppend()
        {
            var stream = new FtpStream(base.Client, this.FullName, FileAccess.ReadWrite);
            stream.Seek(stream.Length, SeekOrigin.Begin);
            return stream;
        }
    }
}
