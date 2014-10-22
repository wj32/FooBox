using FooBox.Common;
using FooBox.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using System.Web.Script.Serialization;

namespace FooBox.Controllers
{
    public class ClientController : Controller
    {
        public static void UploadBlob(FileManager fileManager, Client client, Stream stream, out string hash, out long size)
        {
            var clientUploadDirectory = fileManager.AccessClientUploadDirectory(client.Id);
            var randomName = Utilities.GenerateRandomString(Utilities.IdChars, 32);
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
        public ActionResult Sync()
        {
            using (var sr = new StreamReader(Request.InputStream))
            {
                string input = sr.ReadToEnd();
                var serializer = new JavaScriptSerializer();
                var clientSyncPostData = serializer.Deserialize<ClientSyncPostData>(input);

                var client = FindClient(clientSyncPostData.Id, clientSyncPostData.Secret);

                if (client == null)
                    return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

                clientSyncPostData.Data.ClientId = clientSyncPostData.Id;

                var result = _fileManager.SyncClientChanges(clientSyncPostData.Data);

                if (result.State == ClientSyncResultState.Success)
                    _fileManager.CleanClientUploadDirectory(client.Id);

                return Json(result);
            }
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
        public ActionResult Upload(long? id, string secret)
        {
            var client = FindClient(id, secret);

            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            string hash;
            long fileSize;
            ClientController.UploadBlob(_fileManager, client, Request.InputStream, out hash, out fileSize);

            return Json(new { hash = hash, fileSize = fileSize });
        }
        
        [HttpGet]
<<<<<<< HEAD
        public ActionResult GetShareLink(long? id, string secret, string fullName)
=======
        public ActionResult GetShareLink(long? id, string secret, string relativeFullName)
>>>>>>> dc9ae38f04c05ed618cfaa5f2a9d8fa08003a802
        {
            var client = FindClient(id, secret);
            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

<<<<<<< HEAD
            //TODO: set url = public link for file here
            string key = _fileManager.Context.DocumentLinks.Where(x => x.RelativeFullName == fullName).Select(x => x.Key).FirstOrDefault();

            if (key == null)
            {
                key = Utilities.GenerateRandomString(Utilities.LetterDigitChars, 8);

                var dl = new DocumentLink
                {
                    Key = key,
                    RelativeFullName = fullName,
                    User = _userManager.FindUser(client.Id)
                };

                try
                {
                    var context = _fileManager.Context;
                    context.DocumentLinks.Add(dl);
                    context.SaveChanges();
                }
                catch
                {
                }
            }

            return Content(key);
        }



        /*
         * Authenticates the client wihth the server
         */
        public ActionResult Authenticate(long? id, string secret, string returnUrl) {
            var client = FindClient(id, secret);
            if (client == null){
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }
            var user = _userManager.FindUser(id.Value);
            IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
            var identity = _userManager.CreateIdentity(user, "ApplicationCookie");
            auth.SignIn(new AuthenticationProperties() { IsPersistent = true }, identity);
            
            return Redirect(Uri.EscapeDataString(returnUrl));
=======
            string key = _fileManager.CreateShareLink(relativeFullName, client.User);
            if (key == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);

            return Content(Url.Action("DownloadKey", "File", new { key = key }, Request.Url.Scheme));
>>>>>>> dc9ae38f04c05ed618cfaa5f2a9d8fa08003a802
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