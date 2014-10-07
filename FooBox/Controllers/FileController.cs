using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FooBox;
using FooBox.Models;
using System.Text;

namespace FooBox.Controllers
{
    [Authorize]
    public class FileController : Controller
    {
        private FileManager _fileManager = new FileManager();

        public ActionResult Index()
        {
            return RedirectToAction("Browse");
        }

        private FileBrowseViewModel CreateBrowseModelForFolder(Folder folder, string fullDisplayName)
        {
            if (folder == null)
            {
                folder = _fileManager.GetRootFolder();
                fullDisplayName = "";
            }

            FileBrowseViewModel model = new FileBrowseViewModel();

            model.FullDisplayName = fullDisplayName;
            model.DisplayName = folder.DisplayName.Length == 0 ? "Home" : folder.DisplayName;
            model.Files = (
                from file in folder.Files.AsQueryable()
                where file.State == ObjectState.Normal
                let latestVersion = (file is Document) ? (from version in ((Document)file).DocumentVersions.AsQueryable()
                                                          orderby version.TimeStamp descending
                                                          select version).SingleOrDefault()
                                                         : null
                select new FileBrowseViewModel.FileEntry
                {
                    FullDisplayName = fullDisplayName + "/" + file.DisplayName,
                    DisplayName = file.DisplayName,
                    IsFolder = file is Folder,
                    Size = latestVersion != null ? latestVersion.Blob.Size : 0,
                    TimeStamp = latestVersion != null ? latestVersion.TimeStamp : DateTime.UtcNow
                }
                ).ToList();
            model.Parents = new List<Tuple<string, string>>();

            Folder parentFolder = folder.ParentFolder;
            List<Folder> parentFolders = new List<Folder>();

            while (parentFolder != null)
            {
                if (parentFolder.Name.Length != 0)
                    parentFolders.Add(parentFolder);

                parentFolder = parentFolder.ParentFolder;
            }

            parentFolders.Reverse();

            if (folder.ParentFolder != null)
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

        public ActionResult Browse()
        {
            string path = (string)RouteData.Values["path"];
            string fullDisplayName = null;
            File file = path != null ? _fileManager.FindFile(path, out fullDisplayName) : null;

            if (file == null || !(file is Folder))
            {
                ModelState.AddModelError("", "The path '" + path + "' is invalid.");
                return View(CreateBrowseModelForFolder(null, null));
            }

            return View(CreateBrowseModelForFolder((Folder)file, fullDisplayName));
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
