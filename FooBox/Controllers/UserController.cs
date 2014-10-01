using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using FooBox;

namespace FooBox.Controllers
{
    public class UserController : Controller
    {
        private FooBoxContext db = new FooBoxContext();

        // GET: User/Index
        public ActionResult Index()
        {
 
            return View(db.Users.ToList());
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
                db.Identities.Add(user);
                db.SaveChanges();
                DisplaySuccessMessage("Has append a User record");
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
            User user = (User) db.Identities.Find(id);
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
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
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
            User user = (User) db.Identities.Find(id);
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
            if (ModelState.IsValid)
            {
                Identity user = db.Identities.Find(id);
                db.Entry(user).State = EntityState.Modified;
                
                user.State = ObjectState.Deleted;
                db.SaveChanges();
            }
            
           
            
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
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
