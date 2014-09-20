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
                using (var db = new FooBoxEntities())
                {
                    try
                    {
                        await db.Database.Connection.OpenAsync();

                        // Create the data model tables.

                        var command = db.Database.Connection.CreateCommand();
                        var script = System.IO.File.ReadAllText(Server.MapPath("~/FooBoxModel.edmx.sql"));

                        foreach (var statement in script.Split(new string[] { "\r\nGO\r\n" }, StringSplitOptions.None))
                        {
                            command.CommandText = statement;
                            await command.ExecuteNonQueryAsync();
                        }

                        // Create the admin group.

                        var adminGroup = new Group { Name = "Administrators", Description = "Administrators", IsAdmin = true };
                        var adminUser = new User { Name = model.AdminUserName, PasswordHash = "test", PasswordSalt = "test", FirstName = model.AdminUserName, LastName = "", QuotaLimit = long.MaxValue };
                        adminUser.Groups.Add(adminGroup);
                        db.Identities.AddRange(new Identity[] { adminGroup, adminUser });

                        await db.SaveChangesAsync();

                        return RedirectToAction("Index", "Home");
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", ex);
                    }
                }
            }

            // Error
            return View(model);
        }
	}
}