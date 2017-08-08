using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp.Exceptions
{
    public class FtpPathErrorException
        : Exception
    {
        public FtpPathErrorException(string path)
        {
            this.Path = path;
        }

        public string Path { get; private set; }
    }
}
