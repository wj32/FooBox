using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    public class SetupController : Controller
    {
        //
        // GET: /Setup/
        public ActionResult Index()
        {
            return RedirectToAction("SetUp");
        }

        //
        // GET: /Setup/SetUp
        [AllowAnonymous]
        public ActionResult SetUp()
        {
            if (FileManager.IsFooBoxSetUp())
                return RedirectToAction("Index", "Home");

            return View();
        }

        //
        // POST: /Setup/SetUp
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult> SetUp(SetupViewModel model)
        {
            if (ModelState.IsValid)
            {
                using (var db = new FooBoxContext())
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        using (var fileManager = new FileManager(db))
                        using (var userManager = new UserManager(db))
                        {
                            userManager.InitialSetup();
                            fileManager.InitialSetup();

                            // Create the admin user.
                            userManager.CreateUser(new User { Name = model.AdminUserName, QuotaLimit = long.MaxValue }, model.AdminPassword);
                            userManager.FindUser(model.AdminUserName).Groups.Add(userManager.FindGroup(UserManager.AdministratorsGroupName));
                            await db.SaveChangesAsync();
                        }

                        transaction.Commit();

                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Error setting up the database: " + ex.Message);
                    }
                }
            }

            // Error
            return View(model);
        }
    }
}