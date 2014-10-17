using FooBox.Common;
using FooBox.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace FooBox.Controllers
{
    public class ClientController : Controller
    {
        public static void UploadBlob(FileManager fileManager, Client client, Stream stream, out string hash, out long size)
        {
            var clientUploadDirectory = fileManager.AccessClientUploadDirectory(client.Id);
            var randomName = Utilities.GenerateRandomString(FileManager.IdChars, 32);
            var tempUploadFileName = clientUploadDirectory.FullName + "\\" + randomName;

            byte[] buffer = new byte[4096 * 4];
            int bytesRead;
            long totalBytesRead = 0;

            // Simultaneously hash the file and write it out to a temporary file.

            using (var hashAlgorithm = fileManager.CreateBlobHashAlgorithm())
            {
                using (var fileStream = new FileStream(tempUploadFileName, FileMode.Create))
                {
                    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        hashAlgorithm.TransformBlock(buffer, 0, bytesRead, null, 0);
                        fileStream.Write(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }
                }

                hashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
                hash = (new SoapHexBinary(hashAlgorithm.Hash)).ToString();

                try
                {
                    System.IO.File.Move(tempUploadFileName, clientUploadDirectory.FullName + "\\" + hash);
                }
                catch
                {
                    // We're going to assume that the file with hash as its name already exists.
                    // This means that someone has already uploaded an identical file.
                    System.IO.File.Delete(tempUploadFileName);
                }
            }

            size = totalBytesRead;
        }

        private FileManager _fileManager;
        private UserManager _userManager;

        public ClientController()
        {
            _fileManager = new FileManager();
            _userManager = new UserManager(_fileManager.Context);
        }

        public ActionResult Sync(long? id, string secret, long? baseChangelistId)
        {
            var client = FindClient(id, secret);

            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            return Json(_fileManager.SyncClientChanges(
                new ClientSyncData
                {
                    ClientId = client.Id,
                    BaseChangelistId = baseChangelistId ?? 0
                }),
                JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult Sync(long? id, string secret)
        {
            var client = FindClient(id, secret);

            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            StreamReader sr = new StreamReader(Request.InputStream);
            string input = sr.ReadToEnd();

            var serializer = new JavaScriptSerializer();
            var clientSyncData = serializer.Deserialize<ClientSyncData>(input);

            return Json(_fileManager.SyncClientChanges(clientSyncData));
        }

        public ActionResult Download(long? id, string secret, string hash)
        {
            var client = FindClient(id, secret);

            if (client == null || hash == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            hash = hash.ToUpper();
            string key = (from blob in _fileManager.Context.Blobs where blob.Hash == hash select blob.Key).FirstOrDefault();

            if (key == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);

            return File(_fileManager.GetBlobFileName(key), "application/octet-stream");
        }

        [HttpPost]
        public ActionResult Upload(long? id, string secret, HttpPostedFileBase uploadFile)
        {
            var client = FindClient(id, secret);

            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            if (uploadFile == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);

            string hash;
            long fileSize;
            ClientController.UploadBlob(_fileManager, client, uploadFile.InputStream, out hash, out fileSize);

            return Json(new { hash = hash, fileSize = fileSize });
        }

        private Client FindClient(long? id, string secret)
        {
            if (!id.HasValue)
                return null;

            var client = _fileManager.FindClient(id.Value);
            if (client == null)
                return null;
            if (client.Secret != secret)
                return null;

            return client;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileManager.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}