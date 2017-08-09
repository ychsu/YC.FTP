using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YC.Ftp
{
    using Exceptions;

    public class FtpItem
    {
        private static string pattern = @"(?<basePath>ftps?://([\w-]+\.)*\w+(:\d+)?)(?<path>/.+)?";
        private static Regex regex = new Regex(pattern);
        private KeyValuePair<string, Func<Match, FtpItem>> _pattern;

        private string _permissions;
        private string _owner;
        private string _group;
        private int _fileSize;
        private DateTime _modifiedOn;
        private bool? _exists;
        internal FtpDirectory _parent;

        internal FtpItem(FtpClient client, string fullName)
        {
            this.Client = client;
            this.FullName = fullName;
        }

        public FtpItem(string path, ICredentials credential)
        {
            var match = regex.Match(path);
            if (match.Success == false)
            {
                throw new FtpPathErrorException(path);
            }

            var basePath = match.Groups["basePath"].Value;
            this.Client = new FtpClient(basePath, credential);

            this.FullName = match.Groups["path"].Success ? match.Groups["path"].Value : "/";
        }

        public FtpClient Client { get; private set; }

        /// <summary>
        /// 取得檔案完整路徑
        /// </summary>
        public string FullName { get; internal set; }

        /// <summary>
        /// 取得檔案名稱
        /// </summary>
        public string Name
        {
            get
            {
                return this.FullName.Split('/').LastOrDefault();
            }
        }

        /// <summary>
        /// 取得檔案權限 (Unix Only)
        /// </summary>
        /// <returns></returns>
        public string Permissions
        {
            get
            {
                CheckFileExists();
                return this._permissions;
            }
            internal set
            {
                this._permissions = value;
            }
        }

        /// <summary>
        /// 取得擁有者 (Unix Ftp Only)
        /// </summary>
        /// <returns></returns>
        public string Owner
        {
            get
            {
                CheckFileExists();
                return _owner;
            }
            internal set
            {
                this._owner = value;
            }
        }

        /// <summary>
        /// 取得擁有群組 (Unix Ftp Only)
        /// </summary>
        /// <returns></returns>
        public string Group
        {
            get
            {
                CheckFileExists();
                return this._group;
            }
            internal set
            {
                this._group = value;
            }
        }

        /// <summary>
        /// 取得檔案大小
        /// </summary>
        /// <returns></returns>
        public int FileSize
        {
            get
            {
                CheckFileExists();
                return this._fileSize;
            }
            internal set
            {
                this._fileSize = value;
            }
        }

        /// <summary>
        /// 取得檔案修改時間
        /// </summary>
        public DateTime ModifiedOn
        {
            get
            {
                CheckFileExists();
                return this._modifiedOn;
            }
            internal set
            {
                this._modifiedOn = value;
            }
        }

        public bool Exists
        {
            get
            {
                if (this.IsRoot == false && this._exists.HasValue == false)
                {
                    this.LoadFileItem();
                }
                return this._exists ?? false;
            }
            internal set
            {
                this._exists = value;
            }
        }

        private void CheckFileExists()
        {
            if (this.Exists == false)
            {
                throw new FtpFileNotFoundException(this.FullName);
            }
        }

        private void LoadFileItem()
        {
            var item = this.GetParent()?.GetItems()
                .FirstOrDefault(p => p.Name ==  this.Name);
            this.Exists = item != null;
            if (this.Exists)
            {
                this.FileSize = item.FileSize;
                this.Group = item.Group;
                this.ModifiedOn = item.ModifiedOn;
                this.Owner = item.Owner;
                this.Permissions = item.Permissions;
            }
        }

        protected bool IsRoot
        {
            get
            {
                return this.FullName == "/";
            }
        }

        public FtpDirectory GetRoot()
        {
            if (this.IsRoot == true)
            {
                return null;
            }
            return new FtpDirectory(this.Client, "/");
        }

        public FtpDirectory GetParent()
        {
            if (this.IsRoot == true)
            {
                return null;
            }
            var splits = this.FullName
                .Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            return this._parent ?? new FtpDirectory(this.Client,
                "/" + string.Join("/", splits.Take(splits.Length - 1)));
        }

        public bool Delete()
        {
            try
            {
                var method = this is FtpDirectory ? Enums.FtpMethod.RemoveDirectory : Enums.FtpMethod.DeleteFile;
                this.Client.Request(this.FullName, method, null);
            }
            catch (Exception)
            {
                return false;
            }
            return true;
        }

        protected KeyValuePair<string, Func<Match, FtpItem>> TryGetPattern(string value)
        {
            var patterns = new Dictionary<string, Func<Match, FtpItem>>
            {
			    // Unix
			    { @"(?<Permissions>\S+)\s+(?<ObjectCount>\d+)\s+(?<Owner>\S+)\s+(?<Group>\S+)\s+(?<FileSize>\d+)\s+(?<ModifiedOn>\w+\s+\d+\s+\d+:\d+|\w+\s+\d+\s+\d+)\s(?<FileName>.*)$", m => UnixParser(m) },
			    // Dos
			    { @"(?<ModifiedOn>\d+-\d+-\d+\s+\d+:\d+\w+)\s+((<DIR>)|(?<FileSize>\d+)\s+)(?<FileName>.*)$", m => DosParser(m) }
            };
            if (string.IsNullOrWhiteSpace(_pattern.Key) == true)
            {
                foreach (var pattern in patterns)
                {
                    var reg = new Regex(pattern.Key);
                    var m = reg.Match(value);
                    if (m.Success == true)
                    {
                        _pattern = pattern;
                    }
                }
            }
            if (string.IsNullOrWhiteSpace(_pattern.Key) == true)
            {
                throw new FormatException("not supported");
            }
            return _pattern;
        }

        private FtpItem UnixParser(Match m)
        {
            var permissions = m.Groups["Permissions"].Value.ToLower().Trim();
            var fullName = FullName.TrimEnd('/');
            var fileName = m.Groups["FileName"].Value.Trim();
            FtpItem item = permissions[0] == 'd' ?
                new FtpDirectory(this.Client, fullName + "/" + fileName) as FtpItem :
                new FtpFile(Client, fullName + "/" + fileName) as FtpItem;
            item.Exists = true;
            item.Permissions = permissions;
            item.Owner = m.Groups[nameof(item.Owner)].Value.Trim();
            item.Group = m.Groups[nameof(item.Group)].Value.Trim();
            if (m.Groups[nameof(item.FileSize)].Success == true)
            {
                int i;
                if (int.TryParse(m.Groups[nameof(item.FileSize)].Value.Trim(), out i) == true)
                {
                    item.FileSize = i;
                }
            }
            if (m.Groups[nameof(item.ModifiedOn)].Success == true)
            {
                var formats = new string[]
                {
                "MMM dd HH:mm",
                "MMM dd yyyy"
                };
                var provider = System.Globalization.CultureInfo.GetCultureInfo("en-US");
                DateTime i;
                if (DateTime.TryParseExact(m.Groups[nameof(item.ModifiedOn)].Value.Trim(), formats, provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out i) == true)
                {
                    item.ModifiedOn = i;
                }
            }
            return item;
        }

        private FtpItem DosParser(Match m)
        {
            var isDir = m.Groups[2].Success;
            var fullName = FullName.TrimEnd('/');
            var fileName = m.Groups["FileName"].Value.Trim();
            FtpItem item = isDir ?
                new FtpDirectory(Client, fullName + "/" + fileName) as FtpItem :
                new FtpFile(Client, fullName + "/" + fileName) as FtpItem;
            item.Exists = true;
            if (m.Groups[nameof(item.FileSize)].Success == true)
            {
                int size;
                if (int.TryParse(m.Groups[nameof(item.FileSize)].Value.Trim(), out size) == true)
                {
                    item.FileSize = size;
                }
            }
            if (m.Groups[nameof(item.ModifiedOn)].Success == true)
            {
                DateTime i;
                var str = m.Groups[nameof(item.ModifiedOn)].Value.Trim();
                var provider = System.Globalization.CultureInfo.GetCultureInfo("en-US");
                if (DateTime.TryParseExact(str, "MM-dd-yy hh:mmtt", provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out i) == true)
                {
                    item.ModifiedOn = i;
                }
            }
            return item;
        }

    }
}
