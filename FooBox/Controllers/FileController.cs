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
using FooBox.Common;
using System.Data.Entity.Infrastructure;

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
            model.FullDisplayName = fullDisplayName;
            model.Versions = (
                from version in document.DocumentVersions
                orderby version.TimeStamp descending
                select new VersionHistoryViewModel.VersionEntry
                {
                    Size = version.Blob.Size,
                    TimeStamp = version.TimeStamp,
                    VersionId = version.Id,
                    ClientName = version.Client.Name
                }
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

        // POST
        [HttpPost]
        public ActionResult RevertVersion(string fullName, long versionId)
        {
            long userId = User.Identity.GetUserId();
            string fullDisplayName = null;
            DocumentVersion version = _fileManager.FindDocumentVersion(versionId);
            if (version == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            _fileManager.FindFile(fullName, _fileManager.GetUserRootFolder(userId), out fullDisplayName);
            File file = _fileManager.FindFile(fullName, _fileManager.GetUserRootFolder(userId), out fullDisplayName);
            if (file == null || !(file is Document))
                return RedirectToAction("Browse");

            var internalClient = _fileManager.GetInternalClient(userId);
            ClientSyncData data = new ClientSyncData();

            data.ClientId = internalClient.Id;
            data.BaseChangelistId = _fileManager.GetLastChangelistId();
            data.Changes.Add(new ClientChange
            {
                FullName = "/" + userId.ToString() + "/" + fullDisplayName,
                Type = ChangeType.Add,
                Hash = version.Blob.Hash,
                Size = version.Blob.Size
            });
            _fileManager.SyncClientChanges(data);

            return RedirectToAction("DisplayVersionHistory", new { fullName = fullName });
        }

        

        private bool NameConflicts(Folder parent, string name, bool creatingDocument, bool newVersion)
        {
            File file = parent.Files.AsQueryable().Where(f => f.Name == name).SingleOrDefault();

            if (file == null || file.State == ObjectState.Deleted)
                return false;

            if (creatingDocument && newVersion && file is Document)
                return false;

            return true;
        }

        private bool EnsureAvailableName(ref string destinationDisplayName, Folder parent, bool creatingDocument, bool newVersion = false)
        {
            const int MaxIterations = 10;

            string originalDisplayName = destinationDisplayName;
            string name = destinationDisplayName.ToUpperInvariant();

            if (NameConflicts(parent, name, creatingDocument, newVersion))
            {
                int iteration = 0;

                do
                {
                    if (iteration == MaxIterations)
                    {
                        // Try one last time with a random name.
                        destinationDisplayName = Utilities.GenerateNewName(originalDisplayName, creatingDocument, Utilities.GenerateRandomString("0123456789", 8));
                        name = destinationDisplayName.ToUpperInvariant();
                        iteration++;
                        continue;
                    }
                    else if (iteration == MaxIterations + 1)
                    {
                        return false;
                    }

                    destinationDisplayName = Utilities.GenerateNewName(originalDisplayName, creatingDocument, (iteration + 2).ToString());
                    name = destinationDisplayName.ToUpperInvariant();
                    iteration++;

                    // Don't allow new versions if we already had a name conflict.
                    newVersion = false;
                } while (NameConflicts(parent, name, creatingDocument, newVersion));
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

            if (!Utilities.ValidateFileName(newFileDisplayName))
                return RedirectToAction("Browse", new { path = fromPath });
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

                AddFileRecursive(data.Changes, existingFile, parentFolderFullNameFromRoot + "/" + destinationDisplayName, destinationDisplayName);
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

        [HttpPost]
        public ActionResult Upload(string fromPath, HttpPostedFileBase uploadFile)
        {
            long userId = User.Identity.GetUserId();
            var internalClient = _fileManager.GetInternalClient(userId);
            string fullDisplayName = null;
            File file = _fileManager.FindFile(fromPath ?? "", _fileManager.GetUserRootFolder(userId), out fullDisplayName);

            if (file == null || !(file is Folder) || uploadFile == null || !Utilities.ValidateFileName(uploadFile.FileName))
                return RedirectToAction("Browse");

            Folder folder = (Folder)file;
            string destinationDisplayName = uploadFile.FileName;

            if (!EnsureAvailableName(ref destinationDisplayName, folder, true, true))
                return RedirectToAction("Browse");

            try
            {
                string hash;
                long fileSize;

                ClientController.UploadBlob(_fileManager, internalClient, uploadFile.InputStream, out hash, out fileSize);

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

            if (file == null || !(file is Folder) || !Utilities.ValidateFileName(newFolderName))
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

        public ActionResult GetShareLink(string fullName)
        {
            string key = _fileManager.Context.DocumentLinks.Where(x => x.RelativeFullName == fullName).Select(x => x.Key).FirstOrDefault();

            if (key == null)
            {
                key = Utilities.GenerateRandomString(Utilities.LetterDigitChars, 8);
                long userId = User.Identity.GetUserId();
                var dl = new DocumentLink
                {
                    Key = key,
                    RelativeFullName = fullName,
                    User = _userManager.FindUser(userId)
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
            return PartialView("_DocumentLinkURL", key);
            //return Url.Action("DownloadKey", "File", new { key = key }, Request.Url.Scheme);
        }

        public ActionResult DisplayShareLinks()
        {
            return View(_fileManager.Context.DocumentLinks);
        }

        public ActionResult DeleteShareLink(long? id)
        {
            _fileManager.Context.DocumentLinks.RemoveRange(from linc in _fileManager.Context.DocumentLinks where linc.Id == id select linc);
            _fileManager.Context.SaveChanges();
            return RedirectToAction("DisplayShareLinks");
        }

        public ActionResult DownloadKey(string key)
        {
            if (key == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var doc = _fileManager.FindDocumentFromKey(key);
            if (doc == null || doc.State != ObjectState.Normal)
                return new HttpStatusCodeResult(HttpStatusCode.NotFound);

            return DownloadDocument(doc);   
        }

        public ActionResult FolderEdit(string fullName)
        {
            if (fullName == null)
                return RedirectToAction("Browse");

            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
            string fullDisplayName;
            File file = _fileManager.FindFile(fullName, userRootFolder, out fullDisplayName);

            if (file == null || file is Document)  
            {
                return HttpNotFound();
            }

            Folder folder = (Folder)file;
            List<EntitySelectedViewModel> users = new List<EntitySelectedViewModel>();

            var mod = new EditFolderViewModel();
            mod.Id = folder.Id;
            
            var userList = _userManager.Context.Users.ToList();
            foreach (User u in userList)
            {
                if (u.Name.Equals("__DEFAULT__") || u.State == ObjectState.Deleted) { continue; }
                var a = new EntitySelectedViewModel();
                a.Id = u.Id;
                a.IsSelected = UserHasLink(u, folder); 
                a.Name = u.Name;
                users.Add(a);
            }
            mod.Users = users;

            ViewBag.Subheading = folder.DisplayName;
            return View(mod);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult FolderEdit(EditFolderViewModel model)
        {
            if (ModelState.IsValid)
            {
                Folder f = _fileManager.FindFolder(model.Id);
                foreach (var item in model.Users)
                {
                        FooBox.User u = _userManager.FindUser(item.Id);
                    SetUserHasLink(u, f, item.IsSelected);
        
                }
                return RedirectToAction("Index");
            }
            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileManager.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserHasLink(User user, Folder folder) {
            //var link = 
            //(
            //    from file in user.RootFolder.Files.AsQueryable()
            //    where file.State == ObjectState.Normal && 
            //            (file is Link) && 
            //            ((Link)file).TargetId == folder.Id
            //    select file
            //).SingleOrDefault();
            //return link != null;
            return false;
        }

        private void SetUserHasLink(User user, Folder folder, bool hasPermission) {
            long userId = User.Identity.GetUserId();
            var internalClient = _fileManager.GetInternalClient(userId);

            if (hasPermission && !UserHasLink(user, folder))
            { // add the link

                

            }
            else if (!hasPermission && UserHasLink(user, folder))
            { // remove the link
                
                //var link =
                //(
                //    from file in user.RootFolder.Files.AsQueryable()
                //    where file.State == ObjectState.Normal &&
                //            (file is Link) &&
                //            ((Link)file).TargetId == folder.Id
                //    select file
                //).SingleOrDefault();

                
                //ClientSyncData data = new ClientSyncData();
                //var fullDisplayName = _fileManager.GetFullDisplayName(link);
                //data.ClientId = internalClient.Id;
                //data.BaseChangelistId = _fileManager.GetLastChangelistId();
                //data.Changes.Add(new ClientChange
                //{
                //    FullName = fullDisplayName,
                //    Type = ChangeType.Delete
                //});
                //_fileManager.SyncClientChanges(data);

            } 
        }



    }
}
