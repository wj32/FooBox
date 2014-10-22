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



       

        public ActionResult Index(string fullName)
        {
            if (fullName == null)
                return RedirectToAction("Browse", "File", null);

            Folder userRootFolder = _fileManager.GetUserRootFolder(User.Identity.GetUserId());
            string fullDisplayName;
            File file = _fileManager.FindFile(fullName, userRootFolder, out fullDisplayName);

            if (file == null || file is Document)
            {
                return HttpNotFound();
            }

            Folder folder = (Folder)file;

            var mod = new EditInvitationsViewModel();
            
            mod.FullName = fullName;
            
            var invitations = _fileManager.Context.Invitations.ToList();
            var usersAlreadyInvited = new HashSet<User>();
            var allUsers = _fileManager.Context.Users.ToList();
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
                    usersAlreadyInvited.Add(i.User);
                }
            }
            var uninvitedUsers = allUsers.Except(usersAlreadyInvited);

            foreach (User u in uninvitedUsers)
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult NewInvitation(EditInvitationsViewModel model)
        {
            HashSet<User> uninvitedUsers = new HashSet<User>();
            if (ModelState.IsValid)
            {
                Folder f = (Folder)_fileManager.FindFile(model.FullName);
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
                            UserId = u.Id
                        }; // ADD TIMESTAMP
                        _userManager.Context.Invitations.Add(invitation);
                        _userManager.Context.SaveChanges();
                    } 
                    else 
                    {
                        uninvitedUsers.Add(_userManager.FindUser(item.Id));
                    }
                }
                foreach (var item in model.GroupsToInvite)
                {
                    if (item.IsSelected)
                    {
                        FooBox.Group g = _userManager.FindGroup(item.Id);
                        foreach (User u in g.Users)
                        {
                            if (uninvitedUsers.Contains(u))
                            {
                                uninvitedUsers.Remove(u);
                                var invitation = new Invitation
                                {
                                    Target = f,
                                    TargetId = f.Id,
                                    User = u,
                                    UserId = u.Id,
                                    TimeStamp = new DateTime()
                                }; // ADD TIMESTAMP
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
        public ActionResult DeleteInvitation(long? id)
        {
            var inv = (from invitation in _userManager.Context.Invitations 
                           where invitation.Id == id select invitation).SingleOrDefault();
            var fullName = _fileManager.GetFullName(inv.Target);
            _userManager.Context.Invitations.Remove(inv);
            _userManager.Context.SaveChanges();
            return RedirectToAction("Index", new { fullName = fullName });
        }


    }
}