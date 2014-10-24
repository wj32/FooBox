using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    public class InvitationController : Controller
    {
        private FileManager _fileManager;
        private UserManager _userManager;

        public InvitationController()
        {
            _fileManager = new FileManager();
            _userManager = new UserManager(_fileManager.Context);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fileManager.Dispose();
            }
            base.Dispose(disposing);
        }
       

        public ActionResult Index(string fullName)
        {
            if (fullName == null)
                return RedirectToAction("Browse", "File", null);

            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
            User currentUser = _userManager.FindUser(User.Identity.GetUserId());
            string fullDisplayName;
            File file = _fileManager.FindFile(fullName, userRootFolder, out fullDisplayName);

            if (file == null || file is Document)
            {
                return HttpNotFound();
            }

            Folder folder = (Folder)file;

            var mod = new EditInvitationsViewModel();
            
            mod.FullName = fullName;
            mod.FromPath = _fileManager.GetFullName(file.ParentFolder, userRootFolder);
            
            var invitations = _fileManager.Context.Invitations.ToList();
            foreach (Invitation i in invitations)
            {
                if (i.TargetId == folder.Id)
                {
                    var ivm = new InvitationVM
                    {
                        Id = i.Id,
                        UserName = i.User.Name,
                        Timestamp = i.TimeStamp,
                        Accepted = i.AcceptedFolders.Any()
                    };
                    mod.FolderInvitations.Add(ivm);
                }
            }

            var uninvited = uninvitedUsers(fullName);
            foreach (User u in uninvited)
            {
                if (u == _userManager.GetDefaultUser() || 
                    u.State == ObjectState.Deleted || 
                    u == currentUser) continue;
                var e = new EntitySelectedViewModel
                {
                    Id = u.Id,
                    IsSelected = false,
                    Name = u.Name
                };
                mod.UsersToInvite.Add(e);
            }

            foreach (Group g in _userManager.Context.Groups)
            {
                if (g.State == ObjectState.Deleted) continue;
                var e = new EntitySelectedViewModel
                {
                    Id = g.Id,
                    IsSelected = false,
                    Name = g.Name
                };
                mod.GroupsToInvite.Add(e);
            }

            ViewBag.Subheading = folder.DisplayName;
            return View(mod);
        }

        public ActionResult AllInvitations()
        {
           
            User currentUser = _userManager.FindUser(User.Identity.GetUserId());
            AllInvitationsViewModel mod = new AllInvitationsViewModel();
            var invitations = _fileManager.Context.Invitations.ToList();
            foreach (Invitation i in invitations)
            {
                if (i.User == currentUser)
                {
                    var ivm = new InvitationVM
                    {
                        Id = i.Id,
                        FolderName = i.Target.DisplayName,
                        UserName = i.Target.Owner.Name,
                        Timestamp = i.TimeStamp,
                        Accepted = i.AcceptedFolders.Any()
                    };
                    mod.Incoming.Add(ivm);
                }
                if (i.Target.Owner == currentUser)
                {
                    var ivm = new InvitationVM
                    {
                        Id = i.Id,
                        FolderName = i.Target.DisplayName,
                        UserName = i.User.Name,
                        Timestamp = i.TimeStamp,
                        Accepted = i.AcceptedFolders.Any()
                    };
                    mod.Outgoing.Add(ivm);
                }
            }

            return View(mod);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewInvitation(EditInvitationsViewModel model)
        {
            List<User> uninvited = new List<User>(uninvitedUsers(model.FullName));
            if (ModelState.IsValid)
            {
                Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
                User currentUser = _userManager.FindUser(User.Identity.GetUserId());
                string fullDisplayName;
                Folder f = (Folder)_fileManager.FindFile(model.FullName, userRootFolder, out fullDisplayName);
                foreach (var item in model.UsersToInvite)
                {
                    if (item.IsSelected)
                    {
                        FooBox.User u = _userManager.FindUser(item.Id);
                        var invitation = new Invitation
                        {
                            Target = f,
                            TargetId = f.Id,
                            User = u,
                            UserId = u.Id,
                            TimeStamp = DateTime.UtcNow
                        };
                        _userManager.Context.Invitations.Add(invitation);
                        _userManager.Context.SaveChanges();
                    } 
                }
                foreach (var item in model.GroupsToInvite)
                {
                    if (item.IsSelected)
                    {
                        FooBox.Group g = _userManager.FindGroup(item.Id);
                        foreach (User u in g.Users)
                        {
                            if (u == _userManager.GetDefaultUser() || 
                                u.State == ObjectState.Deleted ||
                                u == currentUser) continue;
                            if (uninvited.Contains(u))
                            {
                                uninvited.Remove(u);
                                var invitation = new Invitation
                                {
                                    Target = f,
                                    TargetId = f.Id,
                                    User = u,
                                    UserId = u.Id,
                                    TimeStamp = DateTime.UtcNow
                                }; 
                                _userManager.Context.Invitations.Add(invitation);
                                _userManager.Context.SaveChanges();
                            }
                        }
                    }
                }
                return RedirectToAction("Index", new { fullName = model.FullName });
            }
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteInvitation(EditInvitationsViewModel model, long? id)
        {
            var inv = (from invitation in _userManager.Context.Invitations 
                           where invitation.Id == id select invitation).SingleOrDefault();
            _userManager.Context.Invitations.Remove(inv);
            // TODO gotta remove the folder in invitation.AcceptedFolders
            _userManager.Context.SaveChanges();
            return RedirectToAction("Index", new { fullName = model.FullName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteInvitationIncoming(long? id)
        {
            var inv = (from invitation in _userManager.Context.Invitations
                       where invitation.Id == id
                       select invitation).SingleOrDefault();
            _userManager.Context.Invitations.Remove(inv);
            // TODO gotta remove the folder in invitation.AcceptedFolders
            _userManager.Context.SaveChanges();
            return RedirectToAction("AllInvitations");
        }

        [HttpPost]
        // Returns new folder name
        public ActionResult EditStatus(long id, string submit)
        {
            var inv = (from invitation in _userManager.Context.Invitations
                       where invitation.Id == id select invitation).SingleOrDefault();
            if (submit.Equals("Accept"))
            {
                return RedirectToAction("AddSharedFolder", "File", new { invitationId = id });
            }
            else if (submit.Equals("Decline"))
            {
                _userManager.Context.Invitations.Remove(inv);
                _userManager.Context.SaveChanges();
                return null;
            }
            else
            {
                return null;
            }
        }

        private IEnumerable<User> uninvitedUsers(string fullName) 
        {
            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
            string fullDisplayName;
            File file = _fileManager.FindFile(fullName, userRootFolder, out fullDisplayName);

            if (file == null || file is Document)
            {
                return null;
            }

            Folder folder = (Folder)file;
            var invitations = _fileManager.Context.Invitations.ToList();
            var usersAlreadyInvited = new HashSet<User>();
            var allUsers = _fileManager.Context.Users.ToList();
            foreach (Invitation i in invitations)
            {
                if (i.TargetId == folder.Id)
                {
                    usersAlreadyInvited.Add(i.User);
                }
            }
            return allUsers.Except(usersAlreadyInvited);
        }



    }
}