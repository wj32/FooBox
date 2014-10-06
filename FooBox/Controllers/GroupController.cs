using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    public class GroupController : Controller
    {
        private UserManager um = new UserManager();

        // GET: Group/Index
        public ActionResult Index()
        {
            return View(um.Context.Groups);
        }

        // GET: Group/GroupCreate
        public ActionResult GroupCreate()
        {
            var mod = new AdminNewGroupViewModel
            {
                Items = new MultiSelectList(um.Context.Users, "Id", "Name", null)
            };
            return View(mod);
        }

        // POST: Group/GroupCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GroupCreate(AdminNewGroupViewModel model)
        {
            Group template = null;
            if (ModelState.IsValid)
            {
                template = new Group
                {
                    Name = model.Name,
                    Description = model.Description,
                    IsAdmin = model.IsAdmin
                };
                um.CreateGroup(template);
                DisplaySuccessMessage("User created");
                return RedirectToAction("Index");
            }
         

            DisplayErrorMessage();
            return View(model);
        }




        // GET: Group/GroupEdit/5
        public ActionResult GroupEdit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Group grp = um.FindGroup(id.Value);

            if (grp == null)
            {
                return HttpNotFound();
            }
            var mod = new AdminEditGroupViewModel
            {
                Id = grp.Id,
                Name = grp.Name,
                Description = grp.Description,
                IsAdmin = grp.IsAdmin,
                Items = new MultiSelectList(um.Context.Users, "Id", "Name", grp.Users)
            };

            return View(mod);
        }

        // POST: Group/GroupEdit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult GroupEdit(AdminEditGroupViewModel model)
        {
            if (ModelState.IsValid)
            {
                Group g = um.FindGroup(model.Id);
                g.Name = model.Name;
                g.IsAdmin = model.IsAdmin;
                if (model.Users != null) {
                    g.Users = model.Users.ToList();
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