using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YC.Ftp.Enums;

namespace YC.Ftp
{
    public class FtpDirectory
        : FtpItem
    {
        internal FtpDirectory(FtpClient client, string fullName)
            : base(client, fullName)
        {

        }

        public FtpDirectory(string path, ICredentials credential)
            : base(path, credential)
        {

        }

        /// <summary>
        /// 取得所有物件 (資料夾或檔案)
        /// </summary>
        /// <returns></returns>
        public IEnumerable<FtpItem> GetItems()
        {
            var stream = this.Client.Request(this.FullName, FtpMethod.ListDirectoryDetails, null);
            using (var sr = new StreamReader(stream, FtpConfig.Current.Encoding))
            {
                while (sr.Peek() > -1)
                {
                    var str = sr.ReadLine();
                    var pattern = this.TryGetPattern(str);
                    var match = Regex.Match(str, pattern.Key, RegexOptions.IgnoreCase);
                    if (match.Success == false)
                    {
                        yield return null;
                    }
                    var item = pattern.Value(match);
                    item._parent = this;
                    yield return item;
                }
            }
        }

        public IEnumerable<FtpDirectory> GetDirectories()
        {
            return this.GetItems().OfType<FtpDirectory>();
        }

        public FtpDirectory GetDirectory(string directory)
        {
            return this.GetDirectories().FirstOrDefault(dir => dir.Name == directory) ??
                new FtpDirectory(this.Client, this.FullName.TrimEnd('/') + "/" + directory)
                {
                    _parent = this
                };
        }

        public IEnumerable<FtpFile> GetFiles()
        {
            return this.GetItems().OfType<FtpFile>();
        }

        public FtpFile GetFile(string file)
        {
            return this.GetFiles().FirstOrDefault(dir => dir.Name == file) ??
                new FtpFile(this.Client, this.FullName.TrimEnd('/') + "/" + file)
                {
                    _parent = this
                };
        }

        public bool Create()
        {
            try
            {
                this.Client.Request(this.FullName, FtpMethod.MakeDirectory, null);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        public FtpDirectory CreateSubdirectory(string name)
        {
            try
            {
                var fullName = this.FullName.TrimEnd('/') + "/" + name;
                this.Client.Request(fullName, FtpMethod.MakeDirectory, null);
                return new FtpDirectory(this.Client, fullName)
                {
                    _parent = this
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        public FtpFile CreateFile(string name)
        {
            return new FtpFile(this.Client, this.FullName.TrimEnd('/') + "/" + name)
            {
                Exists = false,
                _parent = this
            };
        }

        public bool Delete(bool force)
        {
            if (force == true)
            {
                var items = this.GetItems();
                items.AsParallel()
                    .Where(item => (item is FtpDirectory ? (item as FtpDirectory).Delete(true) : item.Delete()) && false)
                    .ToList();

            }
            return base.Delete();
        }
    }
}
