using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp
{
    public class FtpConfig
    {
        private static FtpConfig _default = null;
        public static FtpConfig Current
        {
            get
            {
                if (_default == null)
                {
                    _default = new FtpConfig
                    {
                        Encoding = Encoding.Default
                    };
                }
                return _default;
            }
        }

        public Encoding Encoding { get; set; }
    }
}
