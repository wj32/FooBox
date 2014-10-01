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
    public class UserController : Controller
    {
        private UserManager um = new UserManager();

        
        // GET: User/Index
        public ActionResult Index()
        {
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
        public ActionResult UserCreate([Bind(Include = "Id,Name,State,PasswordHash,PasswordSalt,FirstName,LastName,QuotaLimit,QuotaCharged,Clients,Groups,RootFolder")] User user)
        {
            if (ModelState.IsValid)
            {
                um.CreateUser(user, "");
                DisplaySuccessMessage("User created");
                return RedirectToAction("Index");
            }

            DisplayErrorMessage();
            return View(user);
        }

        // GET: User/UserEdit/5
        public ActionResult UserEdit(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = um.FindUser(id.Value);

            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: UserUser/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UserEdit([Bind(Include = "Id,Name,FirstName,LastName,QuotaLimit,QuotaCharged")] User user)
        {
            if (ModelState.IsValid)
            {
                um.Context.Entry(user).State = EntityState.Modified;
                um.Context.SaveChanges();
                DisplaySuccessMessage("User details updated");
                return RedirectToAction("Index");
            }
            DisplayErrorMessage();
            return View(user);
        }

        // GET: User/UserDelete/5
        public ActionResult UserDelete(long? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            User user = um.FindUser(id.Value);
            if (user == null)
            {
                return HttpNotFound();
            }
            return View(user);
        }

        // POST: User/UserDelete/5
        [HttpPost, ActionName("UserDelete")]
        [ValidateAntiForgeryToken]
        public ActionResult UserDeleteConfirmed(long id)
        {
            
            um.DeleteUser(id);

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
