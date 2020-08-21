using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;
using System.Linq;
using System.IO;
using System.Net.Security;

namespace YC.Ftp.Test
{
    [TestClass]
    public class UnitTest1
    {
        private const string FTP_BASE_URL = "ftp://localhost";
        private NetworkCredential credential;

        [TestInitialize]
        public void Initial()
        {
            ServicePointManager.ServerCertificateValidationCallback = 
                new RemoteCertificateValidationCallback((sender, certificate, chain, errors) => true);

            this.credential = new NetworkCredential("yc", "yc");
        }

        [TestMethod]
        public void UploadFileTest()
        {
            var root = new FtpDirectory(FTP_BASE_URL, credential);
            var dir = root.GetDirectory("YC.FTP");
            var file = dir.GetFile("FILE") ?? dir.CreateFile("FILE");
            var stream = file.OpenWrite();
            var writer = new StreamWriter(stream);
            writer.WriteLine("Hello World, " + DateTime.Now.ToString());
            writer.WriteLine(string.Join("", Enumerable.Repeat(" ", 2048)));
            writer.Flush();
            writer.Close();
        }

        [TestMethod]
        public void AppendFileTest()
        {
            var root = new FtpDirectory(FTP_BASE_URL, credential);
            var dir = root.GetDirectory("YC.FTP");
            var file = dir.GetFile("FILE") ?? dir.CreateFile("FILE");
            var stream = file.OpenAppend();
            var writer = new StreamWriter(stream);
            writer.WriteLine("Hello World, " + DateTime.Now.ToString());
            writer.WriteLine(string.Join("", Enumerable.Repeat(" ", 2048)));
            writer.Flush();
            writer.Close();
        }

        [TestMethod]
        public void DeleteFileTest()
        {
            var root = new FtpDirectory(FTP_BASE_URL, credential);
            var dir = root.GetDirectory("YC.FTP");
            var file = dir.GetFile("FILE");
            if (file?.Exists == true)
            {
                file.Delete();
            }
        }

        [TestMethod]
        public void CreateFolderTest()
        {
            var root = new FtpDirectory(FTP_BASE_URL, credential);
            var dir = root.GetDirectory("YC.FTP");
            dir.CreateSubdirectory("test");
        }

        [TestMethod]
        public void DeleteFolderTest()
        {
            var dir = new FtpDirectory($"{FTP_BASE_URL}/YC.FTP/test", credential);
            if (dir?.Exists == true)
            {
                dir.Delete(true);
            }
        }

        [TestMethod]
        public void GetItemsTest()
        {
            var dir = new FtpDirectory($"{FTP_BASE_URL}localhost/YC.FTP", credential);
            var items = dir?.GetItems()
                .Select(p => p.FullName);
            Console.WriteLine(string.Join(",", items));
        }

        [TestMethod]
        public void MoveFile()
        {
            var dir = new FtpDirectory($"{FTP_BASE_URL}/YC.FTP", credential);
            var items = dir?.GetFiles()
                .FirstOrDefault()
                ?.MoveTo("/YC.FTP/FILE2");
        }
    }
}
