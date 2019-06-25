using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YC.Ftp
{
    public class Constants
    {
        /// <summary>
        /// get or set request timeout
        /// </summary>
        public static int TimeOut { get; set; } = 100_000;

        /// <summary>
        /// get or set read/write to stream timeout
        /// </summary>
        public static int ReadWriteTimeout { get; set; } = 300_000;
    }
}
