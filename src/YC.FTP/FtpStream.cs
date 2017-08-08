using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YC.Ftp.Enums;

namespace YC.Ftp
{
    public class FtpStream : Stream
    {
        private bool _canRead = true;
        private bool _canSeek = true;
        private bool _canWrite = true;
        private MemoryStream buffer;
        private FtpClient _client;
        private string _fullName;
        private long lastPos = 0;

        internal FtpStream(FtpClient client, string fullName, FileAccess access)
        {
            if ((access & FileAccess.Read) != FileAccess.Read)
            {
                _canRead = false;
            }
            if ((access & FileAccess.Write) != FileAccess.Write)
            {
                _canWrite = false;
            }
            this._client = client;
            this._fullName = fullName;
            if (access.HasFlag(FileAccess.Read))
            {
                this.GetFileStream();
            }
            if (access.HasFlag(FileAccess.Write))
            {
                buffer = buffer ?? new MemoryStream();
            }
        }

        public override bool CanRead => _canRead;

        public override bool CanSeek => _canSeek;

        public override bool CanWrite => _canWrite;

        public override long Length => buffer.Length;

        public override long Position
        {
            get
            {
                return buffer.Position;
            }
            set
            {
                if (!CanSeek)
                {
                    throw new NotSupportedException("The stream does not support seeking");
                }
                buffer.Position = value;
            }
        }

        public override int ReadTimeout
        {
            get
            {
                return buffer.ReadTimeout;
            }
            set
            {
                buffer.ReadTimeout = value;
            }
        }

        public override int WriteTimeout
        {
            get
            {
                return buffer.WriteTimeout;
            }
            set
            {
                buffer.WriteTimeout = value;
            }
        }


        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (this._canRead == false)
            {
                throw new NotSupportedException("Stream does not support reading");
            }
            return this.buffer.BeginRead(buffer, offset, count, callback, state);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            if (this._canWrite == false)
            {
                throw new NotSupportedException("Stream does not support writing");
            }
            return this.buffer.BeginWrite(buffer, offset, count, callback, state);

        }

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (_disposed == false)
            {
                if (disposing)
                {
                    Flush(true);
                    if (buffer != null)
                        buffer.Dispose();
                }
                _disposed = true;
            }
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return buffer.EndRead(asyncResult);
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            buffer.EndWrite(asyncResult);
        }

        public override void Flush()
        {
            Flush(true);
        }

        public void Flush(bool flushToFtp)
        {
            if (this._canWrite && flushToFtp && Position > lastPos)
            {
                long pos = Position;

                try
                {
                    this.buffer.Flush();
                    buffer.Seek(lastPos, SeekOrigin.Begin);
                    var stream = new MemoryStream();
                    var num = 1;
                    var numArray = new byte[1024];
                    while (num > 0)
                    {
                        num = buffer.Read(numArray, 0, numArray.Length);
                        stream.Write(numArray, 0, num);
                    }
                    stream.Flush();
                    stream.Seek(0, SeekOrigin.Begin);
                    var method = lastPos > 0 ? FtpMethod.AppendFile : FtpMethod.UploadFile;
                    this._client.Request(this._fullName, 
                        method, 
                        stream);
                }
                finally
                {
                    buffer.Seek(pos, SeekOrigin.Begin);
                    lastPos = pos;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this._canRead == false)
            {
                throw new NotSupportedException("Stream does not support reading");
            }
            return this.buffer.Read(buffer, offset, count);
        }

        public override int ReadByte()
        {
            if (this._canRead == false)
            {
                throw new NotSupportedException("Stream does not support reading");
            }
            return buffer.ReadByte();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (this._canSeek == false)
            {
                throw new NotSupportedException("Stream does not support seeking");
            }
            return buffer.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            if (this._canWrite == false && this._canSeek == false)
            {
                throw new NotSupportedException();
            }
            buffer.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this._canWrite == false)
            {
                throw new NotSupportedException("Stream does not support writing");
            }
            this.buffer.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            if (this._canWrite == false)
            {
                throw new NotSupportedException("Stream does not support writing");
            }
            buffer.WriteByte(value);
        }

        private void GetFileStream()
        {
            buffer = this._client.Request(this._fullName, FtpMethod.DownloadFile, null) as MemoryStream;
        }
    }
}
