using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    public class GroupsController : Controller
    {
        private UserManager um = new UserManager();

        // GET: Groups/Index
        public ActionResult Index()
        {
            return View(um.Context.Groups);
        }

        // GET: Groups/GroupCreate
        public ActionResult GroupCreate()
        {
            var mod = new AdminNewGroupViewModel
            {
                Items = new MultiSelectList(um.Context.Users, "Id", "Name", null)
            };
            return View(mod);
        }

        // POST: Groups/GroupCreate
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
            return View(template);
        }

        // GET: Groups/GroupDelete/5
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