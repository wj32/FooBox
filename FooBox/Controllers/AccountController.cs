using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.Owin.Security;
using FooBox.Models;
using System.Web.Security;

namespace FooBox.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public AccountController()
            : this(new UserManager())
        {
        }

        public AccountController(UserManager userManager)
        {
            UserManager = userManager;
        }

        public UserManager UserManager { get; private set; }

        //
        // POST  /ClientLogin
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ClientLogin(string username, string password)
        {
            var user = UserManager.FindUser(username, password);
            if (user != null) { 
               //Return json and secret
            }
            return View();
        }

        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

       
        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var user = UserManager.FindUser(model.UserName, model.Password);
                if (user != null)
                {
                    SignIn(user, model.RememberMe);
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Register
        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var template = new User { Name = model.UserName };
                var user = UserManager.CreateUser(template, model.Password);
                if (user != null)
                {
                    SignIn(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Unable to create the user. The user name may already be in use.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/Manage
        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            ViewBag.ReturnUrl = Url.Action("Manage");
            return View();
        }

        //
        // POST: /Account/Manage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(ManageUserViewModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");
            if (ModelState.IsValid)
            {
                bool result = UserManager.ChangeUserPassword(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
                if (result)
                {
                    return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });
                }
                else
                {
                    ModelState.AddModelError("", "The old password is incorrect.");
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut();
            return RedirectToAction("Index", "Home");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && UserManager != null)
            {
                UserManager.Dispose();
                UserManager = null;
            }
            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void SignIn(User user, bool isPersistent)
        {
            var identity = UserManager.CreateIdentity(user, "ApplicationCookie");
            AuthenticationManager.SignIn(new AuthenticationProperties() { IsPersistent = isPersistent }, identity);
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            Error
        }

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion
    }
}