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
                {
                    try
                    {
                        // Create the admin group and admin user.

                        var adminGroup = new Group { Name = "Administrators", Description = "Administrators", IsAdmin = true };
                        db.Groups.Add(adminGroup);

                        using (var userManager = new UserManager(db))
                        {
                            await userManager.CreateAsync(new User { Name = model.AdminUserName, QuotaLimit = long.MaxValue }, model.AdminPassword);
                            (await userManager.FindAsync(model.AdminUserName)).Groups.Add(adminGroup);
                        }

                        await db.SaveChangesAsync();

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