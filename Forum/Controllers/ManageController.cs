﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using System.Web.Hosting;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Security;

using Forum.Models;
using Forum.Functionality;
using Forum.Controllers;

namespace Forum.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager   _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager   = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        #region Index
        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."                
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : message == ManageMessageId.PhotoUploadSuccess ? "Your photo has been uploaded."                
                : message == ManageMessageId.PhotoDeleteSuccess ? "Your photo has been deleted"                
                : message == ManageMessageId.UserProfileUpdated ? "Your profile data updated"
                : "";

            ViewBag.ErrorMessage =
                message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.FileExtensionError ? "Only jpg, png and gif file formats are allowed."
                : message == ManageMessageId.NoCustomUserAvatar ? "No custom User Avatar to delete"
                : "";

            var userId = User.Identity.GetUserId();
            ApplicationUser user = await UserManager.FindByNameAsync(User.Identity.Name);   

            var model = new IndexViewModel
            {
                HasPassword       = HasPassword(),
                PhoneNumber       = await UserManager.GetPhoneNumberAsync(userId),
                TwoFactor         = await UserManager.GetTwoFactorEnabledAsync(userId),
                Logins            = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                LastLogin         = user.LastLoginDate                
            };

            ProfileViewModel modeliko = new ProfileViewModel
            {
                UserName = user.UserName,
            };
            var ProfileUrl = Request.RawUrl;
            ProfileController profCtrl = new ProfileController();
            //profCtrl.Comments(modeliko, ProfileUrl);
            //new ProfileController().Comments(modeliko, ProfileUrl);
            return View(model);
        }
        #endregion

        #region UserData
        public async Task<ActionResult> ChangeUserData()
        {
            //supposedly find by mail, coz default in identity user = mail
            ApplicationUser user = await UserManager.FindByNameAsync(User.Identity.Name); 
            if (user != null)
            {
                ChangeUserDataViewModel model = new ChangeUserDataViewModel
                {
                    FirstName           = user.FirstName,
                    LastName            = user.LastName,
                    Country             = user.Country,
                    City                = user.City,
                    Gender              = user.Gender,
                    BirthDate           = user.BirthDate,
                    YourSelfDescription = user.YourSelfDescription
                };
                return View(model);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<ActionResult> ChangeUserData(ChangeUserDataViewModel model)
        {
            ApplicationUser user = await UserManager.FindByNameAsync(User.Identity.Name);
            if (user != null)
            {
                user.FirstName           = model.FirstName;
                user.LastName            = model.LastName;
                user.Country             = model.Country;
                user.City                = model.City;
                user.Gender              = model.Gender;
                user.BirthDate           = model.BirthDate;
                user.YourSelfDescription = model.YourSelfDescription;

                IdentityResult result = await UserManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", new { Message = ManageMessageId.UserProfileUpdated });
                }
                else
                {
                    ModelState.AddModelError("", "Something wrong");
                }
            }
            else
            {
                ModelState.AddModelError("", "User not found");
            }

            return View(model);
        }
        #endregion        

        #region ChangePassword
        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }
        #endregion

        #region Upload Photo
        //
        // GET: /Manage/UploadPhoto
        public ActionResult UploadPhoto()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> UploadPhoto(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                var user = await GetCurrentUserAsync();
                var username = user.UserName;
                var fileExt = Path.GetExtension(file.FileName);
                var fnm = username + ".png";
                var userDirectory = username;
                if (fileExt.ToLower().EndsWith(".png") || fileExt.ToLower().EndsWith(".jpg") || fileExt.ToLower().EndsWith(".gif"))// Important for security if saving in webroot
                {
                    var filePath = HostingEnvironment.MapPath("~/Content/images/profile/" + userDirectory + "/") + fnm;
                    var directory = new DirectoryInfo(HostingEnvironment.MapPath("~/Content/images/profile/" + userDirectory + "/"));
                    if (directory.Exists == false)
                    {
                        directory.Create();
                    }
                    ViewBag.FilePath = filePath.ToString();
                    file.SaveAs(filePath);
                    return RedirectToAction("Index", new { Message = ManageMessageId.PhotoUploadSuccess });
                }
                else
                {
                    return RedirectToAction("Index", new { Message = ManageMessageId.FileExtensionError });
                }
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.Error });// PRG
        }

        #endregion Upload Photo

        #region Delete Avatar
        //
        // GET: /Manage/Delete Avatar
        public ActionResult DeleteAvatar()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> DeleteAvatar(HttpPostedFileBase file)
        {
            var user = await GetCurrentUserAsync();
            var username = user.UserName;
            var fnm = username + ".png";
            //var userDirectory = username;

            var filePath = HostingEnvironment.MapPath("~/Content/images/profile/" + username + "/" + fnm);
            var directory = new DirectoryInfo(HostingEnvironment.MapPath("~/Content/images/profile/" + username + "/"));
            if (directory.Exists == true)
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    return RedirectToAction("Index", new { Message = ManageMessageId.PhotoDeleteSuccess });
                }
                else
                {
                    return RedirectToAction("Index", new { Message = ManageMessageId.NoCustomUserAvatar });
                }
            }

            return RedirectToAction("Index");            
        }
        #endregion

        #region RemoveLogin
        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }
        #endregion

        #region Manage Logins
        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : "";
            ViewBag.ErrorMessage =
                message == ManageMessageId.Error ? "An error has occurred. Or Such provider has been already in use (social page or goole mail)"
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }
        #endregion

        #region Number & TwoFactor
       
        // GET: /Manage/AddPhoneNumber
        public ActionResult AddPhoneNumber()
        {
            return View();
        }

        //
        // POST: /Manage/AddPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            // Generate the token and send it
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), model.Number);
            if (UserManager.SmsService != null)
            {
                var message = new IdentityMessage
                {
                    Destination = model.Number,
                    Body = "Your security code is: " + code
                };
                await UserManager.SmsService.SendAsync(message);
            }
            return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
        }

        //
        // POST: /Manage/EnableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EnableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), true);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // POST: /Manage/DisableTwoFactorAuthentication
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DisableTwoFactorAuthentication()
        {
            await UserManager.SetTwoFactorEnabledAsync(User.Identity.GetUserId(), false);
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", "Manage");
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
        {
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(User.Identity.GetUserId(), phoneNumber);
            // Send an SMS through the SMS provider to verify the phone number
            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }

        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(User.Identity.GetUserId(), model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // POST: /Manage/RemovePhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemovePhoneNumber()
        {
            var result = await UserManager.SetPhoneNumberAsync(User.Identity.GetUserId(), null);
            if (!result.Succeeded)
            {
                return RedirectToAction("Index", new { Message = ManageMessageId.Error });
            }
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user != null)
            {
                await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
            }
            return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
        }
        #endregion

        #region Helpers
        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await UserManager.FindByIdAsync(User.Identity.GetUserId());
        }

        public static IEnumerable<string> GetCountries()
        {
            RegionInfo country = new RegionInfo(new CultureInfo("en-US", false).LCID);

            List<string> countryNames = new List<string>();

            //To get the Country Names from the CultureInfo installed in windows
            foreach (CultureInfo cul in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                country = new RegionInfo(new CultureInfo(cul.Name, false).LCID);
                countryNames.Add(country.DisplayName.ToString());
            }
            countryNames.Add("");

            //Assigning all Country names to IEnumerable
            IEnumerable<string> nameAdded = countryNames.OrderBy(names => names).Distinct();
            return nameAdded;
        }       

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error,
            PhotoUploadSuccess,
            FileExtensionError,
            PhotoDeleteSuccess,
            NoCustomUserAvatar,
            UserProfileUpdated
        }
        #endregion
    }
}