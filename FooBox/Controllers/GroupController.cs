using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    [Authorize]
    public class GroupController : Controller
    {
        private UserManager um = new UserManager();

        private bool IsAuthorized()
        {
            return um.IsUserAdmin(User.Identity.GetUserId());
        }

        // GET: Group/Index
        public ActionResult Index()
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            return View(um.Context.Groups);
        }

        // GET: Group/GroupCreate
        public ActionResult GroupCreate()
        {
            var mod = new AdminNewGroupViewModel();
            List<EntitySelectedViewModel> users = new List<EntitySelectedViewModel>();


            foreach (User u in um.Context.Users) 
            {
                if (u.Name.Equals("__DEFAULT__") || u.State == ObjectState.Deleted) { continue; }
                var a = new EntitySelectedViewModel();
                a.Id = u.Id;
                a.IsSelected = false;
                a.Name = u.Name;
                users.Add(a);
            }
            mod.Users = users;
            return View(mod);
        }

        // POST: Group/GroupCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GroupCreate(AdminNewGroupViewModel model)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            Group template = null;
            if (ModelState.IsValid)
            {
                template = new Group
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsAdmin = false
                };
                var actual = um.CreateGroup(template);
                actual.Users.Add(um.GetDefaultUser());
                foreach (var item in model.Users) 
                {
                    if (item.IsSelected)
                    {
                        FooBox.User u = um.FindUser(item.Id);
                        if (u != null) actual.Users.Add(u);
                        um.Context.SaveChanges();
                    }
                }           

                DisplaySuccessMessage("User created");
                return RedirectToAction("Index");
            }
         

            DisplayErrorMessage();
            return View(model);
        }




        // GET: Group/GroupEdit/5
        public ActionResult GroupEdit(long? id)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Group grp = um.FindGroup(id.Value);

            if (grp == null)
            {
                return HttpNotFound();
            }

            List<EntitySelectedViewModel> users = new List<EntitySelectedViewModel>();

            var mod = new AdminEditGroupViewModel();   
            mod.Id = grp.Id;
            mod.Name = grp.Name;
            mod.Description = grp.Description;
            var userList = um.Context.Users.ToList();
            foreach (User u in userList)
            {
                if (u.Name.Equals("__DEFAULT__") || u.State == ObjectState.Deleted) { continue; }
                var a = new EntitySelectedViewModel();
                a.Id = u.Id;
                a.IsSelected = grp.Users.Contains(u);
                a.Name = u.Name;
                users.Add(a);
            }
            mod.Users = users;
            return View(mod);
        }

        // POST: Group/GroupEdit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GroupEdit(AdminEditGroupViewModel model)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (ModelState.IsValid)
            {
                Group g = um.FindGroup(model.Id);
                g.Name = model.Name;
                g.Users.Clear();
                g.Users.Add(um.GetDefaultUser());
                foreach (var item in model.Users)
                {
                    if (item.IsSelected)
                    {
                        FooBox.User u = um.FindUser(item.Id);
                        if (u != null) g.Users.Add(u);
                    }
                } 
                g.Description = model.Description;
                try
                {
                    um.Context.SaveChanges();
                }
                catch
                {
                    DisplayErrorMessage();
                    return View(model);
                }
                DisplaySuccessMessage("Group edited");
                return RedirectToAction("Index");
            }


           
            DisplayErrorMessage();
            return View(model);
        }



        // GET: Group/GroupDelete/5
        public ActionResult GroupDelete(long? id)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Group group = um.FindGroup(id.Value);
            if (group == null)
            {
                return HttpNotFound();
            }
            return View(group);
        }

        // POST: Group/GroupDelete/5
        [HttpPost, ActionName("GroupDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult GroupDeleteConfirmed(long id)
        {

            um.DeleteGroup(id);

            DisplaySuccessMessage("Group deleted");
            return RedirectToAction("Index");
        }

        private void DisplaySuccessMessage(string msgText)
        {
            TempData["SuccessMessage"] = msgText;
        }

        private void DisplayErrorMessage()
        {
            TempData["ErrorMessage"] = "Save change was unsuccessful.";
        }

    }
}