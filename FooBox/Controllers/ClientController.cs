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
        public ActionResult GetShareLink(long? id, string secret, string relativeFullName)
        {
            var client = FindClient(id, secret);
            if (client == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);

            string key = _fileManager.CreateShareLink(relativeFullName, client.User);
            if (key == null)
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.NotFound);

            return Content(Url.Action("DownloadKey", "File", new { key = key }, Request.Url.Scheme));
        }

        /// <summary>
        /// Logs into the website by using a client ID and secret.
        /// </summary>
        public ActionResult Authenticate(long? id, string secret, string returnUrl)
        {
            var client = FindClient(id, secret);
            if (client == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }

            // Check if we're already logged in.
            if (string.IsNullOrEmpty(User.Identity.Name) || User.Identity.GetUserId() != client.UserId)
            {
                IAuthenticationManager auth = HttpContext.GetOwinContext().Authentication;
                var identity = _userManager.CreateIdentity(client.User, "ApplicationCookie");
                auth.SignIn(new AuthenticationProperties() { IsPersistent = false }, identity);
            }
            return Redirect(returnUrl);
        }

        [HttpGet]
        public ActionResult CheckInvites(long? id, string secret, DateTime since)
        {
            InviteStatus invites = new InviteStatus();
            var client = FindClient(id, secret);
            if (client == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.Forbidden);
            }
            foreach(Invitation i in client.User.Invitations){
                //if invitation is newer than since
                if (i.TimeStamp.CompareTo(since) > 0)
                {
                    invites.New = true;
          
                }
                if (i.AcceptedFolders.Count > 0)
                {
                    invites.Accepted = true;
                }
            }
            return Json(invites);
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