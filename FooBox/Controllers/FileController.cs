using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Web;
using System.Web.Mvc;
using FooBox;
using FooBox.Models;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace FooBox.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private FileManager _fileManager;
        private UserManager _userManager;

        public FileController()
        {
            _fileManager = new FileManager();
            _userManager = new UserManager(_fileManager.Context);
        }

        public ActionResult Index()
        {
            return RedirectToAction("Browse");
        }

        private FileBrowseViewModel CreateBrowseModelForFolder(Folder folder, string fullDisplayName)
        {
            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());

            if (folder == null)
            {
                folder = userRootFolder;
                fullDisplayName = "";
            }

            FileBrowseViewModel model = new FileBrowseViewModel();

            model.FullDisplayName = fullDisplayName;
            model.DisplayName = folder == userRootFolder ? "Home" : folder.DisplayName;
            model.Files = (
                from file in folder.Files.AsQueryable()
                where file.State == ObjectState.Normal
                orderby !(file is Folder) ascending, file.Name ascending
                let latestVersion = (file is Document) ? (from version in ((Document)file).DocumentVersions.AsQueryable()
                                                          orderby version.TimeStamp descending
                                                          select version).FirstOrDefault()
                                                         : null
                select new FileBrowseViewModel.FileEntry
                {
                    Id = file.Id,
                    FullDisplayName = fullDisplayName + "/" + file.DisplayName,
                    DisplayName = file.DisplayName,
                    IsFolder = file is Folder,
                    Size = latestVersion != null ? latestVersion.Blob.Size : 0,
                    TimeStamp = latestVersion != null ? latestVersion.TimeStamp : DateTime.UtcNow
                }
                ).ToList();
            model.Parents = new List<Tuple<string, string>>();

            // Construct the parent list for the breadcrumb.

            List<Folder> parentFolders = new List<Folder>();

            if (folder != userRootFolder)
            {
                Folder parentFolder = folder.ParentFolder;

                while (parentFolder != userRootFolder)
                {
                    parentFolders.Add(parentFolder);
                    parentFolder = parentFolder.ParentFolder;
                }
            }

            parentFolders.Reverse();

            if (folder != userRootFolder)
                model.Parents.Add(new Tuple<string, string>("Home", ""));

            StringBuilder sb = new StringBuilder();

            foreach (var f in parentFolders)
            {
                sb.Append('/');
                sb.Append(f.DisplayName);

                model.Parents.Add(new Tuple<string, string>(f.DisplayName, sb.ToString()));
            }

            return model;
        }

        private ActionResult DownloadDocument(Document document, DocumentVersion version = null)
        {
            if (version == null)
            {
                // Get latest version
                version = _fileManager.GetLastDocumentVersion(document);
            }
            string blobFileName = _fileManager.GetBlobFileName(version.Blob.Key);
            return File(blobFileName, MimeMapping.GetMimeMapping(document.DisplayName), document.DisplayName);
        }

        public ActionResult Browse()
        {
            string path = (string)RouteData.Values["path"] ?? "";
            string fullDisplayName = null;
            File file = _fileManager.FindFile(path, _fileManager.GetUserRootFolder(User.Identity.GetUserId()), out fullDisplayName);

            if (file == null)
            {
                ModelState.AddModelError("", "The path '" + path + "' is invalid.");
                return View(CreateBrowseModelForFolder(null, null));
            }

            if (file is Document)
            {
                return DownloadDocument((Document)file);
            }
            else
            {
                return View(CreateBrowseModelForFolder((Folder)file, fullDisplayName));
            }
        }

        public ActionResult DisplayVersionHistory(string fullName)
        {
            if (fullName == null)
                return RedirectToAction("Browse");

            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
            string fullDisplayName;
            File file = _fileManager.FindFile(fullName, userRootFolder, out fullDisplayName);

            if (file == null || !(file is Document))
                return RedirectToAction("Browse");

            Document document = (Document)file;
            VersionHistoryViewModel model = new VersionHistoryViewModel();

            model.DisplayName = document.DisplayName;
            model.Versions = (
                from version in document.DocumentVersions
                orderby version.TimeStamp descending
                select new VersionHistoryViewModel.VersionEntry { TimeStamp = version.TimeStamp, VersionId = version.Id }
                ).ToList();

            return View(model);
            }

        public ActionResult DownloadVersion(long? id)
        {
            if (!id.HasValue)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DocumentVersion version = _fileManager.FindDocumentVersion((long)id);
            if (version == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            return DownloadDocument(version.Document, version);
        }

        private bool NameConflicts(Folder parent, string name, bool creatingDocument)
        {
            File file = parent.Files.AsQueryable().Where(f => f.Name == name).SingleOrDefault();

            if (file == null || file.State == ObjectState.Deleted)
                return false;

            if (creatingDocument)
            {
                if (file is Folder)
                    return true;

                return false;
            }
            else
            {
                return true;
            }
        }

        private string GenerateNewName(string originalDisplayName, bool creatingDocument, string key)
        {
            if (creatingDocument)
            {
                int indexOfLastDot = originalDisplayName.LastIndexOf('.');

                if (indexOfLastDot != -1 && indexOfLastDot != originalDisplayName.Length - 1)
                {
                    string firstPart = originalDisplayName.Substring(0, indexOfLastDot);
                    string secondPart = originalDisplayName.Substring(indexOfLastDot + 1, originalDisplayName.Length - (indexOfLastDot + 1));

                    return firstPart + " (" + key + ")." + secondPart;
                }
            }

            return originalDisplayName + " (" + key + ")";
        }

        private bool EnsureAvailableName(ref string destinationDisplayName, Folder parent, bool creatingDocument)
        {
            const int MaxIterations = 10;

            string originalDisplayName = destinationDisplayName;
            string name = destinationDisplayName.ToUpperInvariant();

            if (NameConflicts(parent, name, creatingDocument))
            {
                int iteration = 0;

                do
                {
                    if (iteration == MaxIterations)
                    {
                        // Try one last time with a random name.
                        destinationDisplayName = GenerateNewName(originalDisplayName, creatingDocument, Utilities.GenerateRandomString("0123456789", 8));
                        name = destinationDisplayName.ToUpperInvariant();
                        iteration++;
                        continue;
                    }
                    else if (iteration == MaxIterations + 1)
                    {
                        return false;
                    }

                    destinationDisplayName = GenerateNewName(originalDisplayName, creatingDocument, (iteration + 2).ToString());
                    name = destinationDisplayName.ToUpperInvariant();
                    iteration++;
                } while (NameConflicts(parent, name, creatingDocument));
            }

            return true;
        }

        // POST
        [HttpPost]
        public ActionResult Delete(string fromPath, string fileDisplayName)
        {
            long userId = User.Identity.GetUserId();
            string fullDisplayName = null;
            string fromPathString = fromPath == null ? "" : fromPath + "/";
            File file = _fileManager.FindFile(fromPathString + fileDisplayName, _fileManager.GetUserRootFolder(userId), out fullDisplayName);

            if (file == null || file.State != ObjectState.Normal)
                return RedirectToAction("Browse");

            var internalClient = _fileManager.GetInternalClient(userId);
            ClientSyncData data = new ClientSyncData();

            data.ClientId = internalClient.Id;
            data.BaseChangelistId = _fileManager.GetLastChangelistId();
            data.Changes.Add(new ClientChange
            {
                FullName = "/" + userId.ToString() + "/" + fullDisplayName,
                Type = ChangeType.Delete
            });
            _fileManager.SyncClientChanges(data);

            return RedirectToAction("Browse", new { path = fromPath });
        }

        [HttpPost]
        public ActionResult Rename(string fromPath, string oldFileDisplayName, string newFileDisplayName)
        {
            long userId = User.Identity.GetUserId();
            var internalClient = _fileManager.GetInternalClient(userId);
            string parentFolderFullName = null;
            File parentFolderFile = _fileManager.FindFile(fromPath ?? "", _fileManager.GetUserRootFolder(userId), out parentFolderFullName);

            if (parentFolderFile == null || !(parentFolderFile is Folder) || oldFileDisplayName == newFileDisplayName)
                return RedirectToAction("Browse", new { path = fromPath });

            Folder parentFolder = (Folder)parentFolderFile;

            // Check if the old file exists.

            string oldFileName = oldFileDisplayName.ToUpperInvariant();
            File existingFile = parentFolder.Files.AsQueryable().Where(f => f.Name == oldFileName).SingleOrDefault();

            if (existingFile == null || existingFile.State != ObjectState.Normal)
                return RedirectToAction("Browse", new { path = fromPath });

            ClientSyncData data = new ClientSyncData();
            data.ClientId = internalClient.Id;
            data.BaseChangelistId = _fileManager.GetLastChangelistId();

            // Check if the user is just trying to change the case.

            string parentFolderFullNameFromRoot = "/" + userId.ToString() + "/" + parentFolderFullName;

            if (string.Equals(oldFileDisplayName, newFileDisplayName, StringComparison.InvariantCultureIgnoreCase))
            {
                data.Changes.Add(new ClientChange
                {
                    FullName = parentFolderFullNameFromRoot + "/" + oldFileDisplayName,
                    Type = ChangeType.SetDisplayName,
                    DisplayName = newFileDisplayName
                });
            }
            else
            {
                string destinationDisplayName = newFileDisplayName;

                if (!EnsureAvailableName(ref destinationDisplayName, parentFolder, !(existingFile is Folder)))
                    return RedirectToAction("Browse", new { path = fromPath });

                data.Changes.Add(new ClientChange
                {
                    FullName = parentFolderFullNameFromRoot + "/" + oldFileDisplayName,
                    Type = ChangeType.Delete
                });

                AddFileRecursive(data.Changes, existingFile, parentFolderFullNameFromRoot + "/" + newFileDisplayName, newFileDisplayName);
            }

            _fileManager.SyncClientChanges(data);

            return RedirectToAction("Browse", new { path = fromPath });
        }

        private void AddFileRecursive(ICollection<ClientChange> changes, File existingFile, string newFullName, string newDisplayName = null)
        {
            long size = 0;
            string hash = null;

            if (existingFile is Document)
            {
                Blob blob = _fileManager.GetLastDocumentVersion((Document)existingFile).Blob;
                size = blob.Size;
                hash = blob.Hash;
            }

            changes.Add(new ClientChange
            {
                FullName = newFullName,
                Type = ChangeType.Add,
                IsFolder = existingFile is Folder,
                DisplayName = newDisplayName ?? existingFile.DisplayName,
                Size = size,
                Hash = hash
            });

            if (existingFile is Folder)
            {
                foreach (File file in ((Folder)existingFile).Files)
                {
                    if (file.State != ObjectState.Normal)
                        continue;

                    AddFileRecursive(changes, file, newFullName + "/" + file.Name);
                }
            }
        }

        private void UploadBlob(Client client, Stream stream, out string hash, out long size)
        {
            var clientUploadDirectory = _fileManager.AccessClientUploadDirectory(client.Id);
            var randomName = Utilities.GenerateRandomString(FileManager.IdChars, 32);
            var tempUploadFileName = clientUploadDirectory.FullName + "\\" + randomName;

            byte[] buffer = new byte[4096 * 4];
            int bytesRead;
            long totalBytesRead = 0;

            // Simultaneously hash the file and write it out to a temporary file.

            using (var hashAlgorithm = _fileManager.CreateBlobHashAlgorithm())
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

        [HttpPost]
        public ActionResult Upload(string fromPath, HttpPostedFileBase uploadFile)
        {
            long userId = User.Identity.GetUserId();
            var internalClient = _fileManager.GetInternalClient(userId);
            string fullDisplayName = null;
            File file = _fileManager.FindFile(fromPath ?? "", _fileManager.GetUserRootFolder(userId), out fullDisplayName);

            if (file == null || !(file is Folder) || uploadFile == null)
                return RedirectToAction("Browse");

            Folder folder = (Folder)file;
            string destinationDisplayName = uploadFile.FileName;

            if (!EnsureAvailableName(ref destinationDisplayName, folder, true))
                return RedirectToAction("Browse");

            try
            {
                string hash;
                long fileSize;

                UploadBlob(internalClient, uploadFile.InputStream, out hash, out fileSize);

                ClientSyncData data = new ClientSyncData();

                data.ClientId = internalClient.Id;
                data.BaseChangelistId = _fileManager.GetLastChangelistId();
                data.Changes.Add(new ClientChange
                {
                    FullName = "/" + userId.ToString() + "/" + fullDisplayName + "/" + destinationDisplayName,
                    Type = ChangeType.Add,
                    IsFolder = false,
                    Size = fileSize,
                    Hash = hash,
                    DisplayName = destinationDisplayName
                });

                _fileManager.SyncClientChanges(data);
            }
            finally
            {
                _fileManager.CleanClientUploadDirectory(internalClient.Id);
            }

            return RedirectToAction("Browse", new { path = fromPath });
        }

        [HttpPost]
        public ActionResult NewFolder(string fromPath, string newFolderName)
        {
            long userId = User.Identity.GetUserId();
            var internalClient = _fileManager.GetInternalClient(userId);
            string fullDisplayName = null;
            File file = _fileManager.FindFile(fromPath ?? "", _fileManager.GetUserRootFolder(userId), out fullDisplayName);

            if (file == null || !(file is Folder))
                return RedirectToAction("Browse");

            Folder folder = (Folder)file;
            string destinationDisplayName = newFolderName;

            if (!EnsureAvailableName(ref destinationDisplayName, folder, false))
                return RedirectToAction("Browse");

            ClientSyncData data = new ClientSyncData();

            data.ClientId = internalClient.Id;
            data.BaseChangelistId = _fileManager.GetLastChangelistId();
            data.Changes.Add(new ClientChange
            {
                FullName = "/" + userId + "/" + fromPath + "/" + destinationDisplayName,
                Type = ChangeType.Add,
                IsFolder = true,
                DisplayName = destinationDisplayName
            });

            _fileManager.SyncClientChanges(data);

            return RedirectToAction("Browse", new { path = fromPath });
        }

        /*For now, create an action that takes in a client ID and secret 
         * and returns the entire contents of the client's user's root folder */
        [HttpPost]
        [AllowAnonymous]
        public String ClientRoot(string id, string secret)
        {
            FileManager f = new FileManager();
            Client c;
            long ID;
            if (long.TryParse(id, out ID)){
                c = f.FindClient(ID);
                if (c.Secret != secret)
                {
                    return "fail";
                }
            } else {
                return "fail";
            }
            UserManager m = new UserManager();
            User u = m.FindUser(c.UserId);
            XmlSerializer xmlSerializer = new XmlSerializer(u.RootFolder.GetType());
            StringWriter textWriter = new StringWriter();

            xmlSerializer.Serialize(textWriter, u.RootFolder);
            return textWriter.ToString();
        
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
