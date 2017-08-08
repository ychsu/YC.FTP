using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp.Exceptions
{
    public class FtpFileNotFoundException : Exception
    {
        public FtpFileNotFoundException(string fullName)
        {
            this.FullName = fullName;
        }

        public string FullName { get; private set; }
    }
}
