using FooBox.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FooBox.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (!FileManager.IsFooBoxSetUp())
                return RedirectToAction("SetUp", "Setup");

            return RedirectToAction("Browse", "File");
        }
    }
}