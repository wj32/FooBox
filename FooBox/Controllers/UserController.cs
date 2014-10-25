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

namespace FooBox.Controllers
{
    [Authorize]
    public class UserController : Controller
    {
        private UserManager um = new UserManager();

        private bool IsAuthorized()
        {
            return um.IsUserAdmin(User.Identity.GetUserId());
        }

        // GET: User/Index
        public ActionResult Index()
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            return View(um.Context.Users.ToList());
        }

        /*
        // GET: User/UserDetails/5
        public ActionResult UserDetails(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = db.Identities.Find(id);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }
        */

        // GET: User/UserCreate
        public ActionResult UserCreate()
        {
            return View();
        }

        // POST: User/UserCreate
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserCreate(AdminNewUserViewModel model)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            User template;
            if (ModelState.IsValid)
            {
                template = new User
                {
                    Name = model.Name,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    QuotaLimit = model.QuotaLimit * (1024 * 1024)
                };
                um.CreateUser(template, model.Password);
                DisplaySuccessMessage("User created");
                return RedirectToAction("Index");
            }

            DisplayErrorMessage();
            return View();
        }

        // GET: User/UserEdit/5
        public ActionResult UserEdit(long? id)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = um.FindUser(id.Value);
            var model = new AdminEditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Name = user.Name,
                QuotaLimit = user.QuotaLimit / (1024 * 1024)
            };
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        // POST: UserUser/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserEdit(AdminEditUserViewModel model)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (ModelState.IsValid)
            {
                User u = um.FindUser(model.Id);
                u.Name = model.Name;
                u.FirstName = model.FirstName;
                u.LastName = model.LastName;
                u.QuotaLimit = model.QuotaLimit * 1024 * 1024;
              
                try
                {
                    um.Context.SaveChanges();
                }
                catch
                {
                    DisplayErrorMessage();
                    return View(model);
                }
                DisplaySuccessMessage("User details updated");
                return RedirectToAction("Index");
            }
            DisplayErrorMessage();
            return View(model);
        }

        // POST: User/UserDelete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserDelete(long? id)
        {
            if (!IsAuthorized())
                return new HttpStatusCodeResult(HttpStatusCode.Forbidden);
            if (!id.HasValue)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            um.DeleteUser(id.Value);

            DisplaySuccessMessage("User deleted");
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                um.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
