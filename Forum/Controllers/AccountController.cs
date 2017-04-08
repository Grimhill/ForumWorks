using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Forum.Models;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Web.Hosting;
using Forum.Functionality;


namespace Forum.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        public DbHelpers _dbhelpers = new DbHelpers(); //sql methods helpers
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager   _userManager;

        public AccountController()
        {
        }

        public AccountController(DbHelpers dbhelpers, ApplicationUserManager userManager, ApplicationSignInManager signInManager )
        {
            _dbhelpers    = dbhelpers;
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

        #region Login
        //
        // GET: /Account/Login
        [AllowAnonymous]
        public ActionResult Login(string returnUrl) //receive redirect url
        {
            //this.Request.UrlReferrer - method in view _loginPartial which allowed us to redirect to the page, where logging
            ViewBag.ReturnUrl = returnUrl;             
            return View();
        }

        //
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            var EmailConform   = _dbhelpers.EmailConfirmation(model.LoginUsername); //check if such mail confirmed
            var LogUserName    = _dbhelpers.FindUserName(model.LoginUsername);
            var userRole       = _dbhelpers.CheckUserRole(LogUserName);
            if (userRole == "Banned")
            {
                ModelState.AddModelError("", "User Banned.");
                return View("Login");
            }

            var result = await SignInManager.PasswordSignInAsync(model.LoginUsername, model.LoginPassword, model.RememberMe, shouldLockout: false);
            
            //check if email verified & user exist you can login
            if (EmailConform == false && LogUserName != null && result.ToString() == "Success")
            {
                AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                //UserEmailConformation = model.LoginUsername;
                return RedirectToAction("EmailConfirmationFailed", "Account");
            }            
            else
            {
                ViewBag.ReturnUrl = returnUrl;
                if (ModelState.IsValid)
                {
                    switch (result)
                    {
                        case SignInStatus.Success:
                            _dbhelpers.UpdateLastLoginDate(model.LoginUsername);//update last login
                            _dbhelpers.UpdateOnlineStatus(model.LoginUsername);                            
                            return RedirectToLocal(returnUrl);
                        case SignInStatus.LockedOut:  //user bloked? user is lockout
                            return View("Lockout");
                        case SignInStatus.RequiresVerification: //two factor
                            return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
                        case SignInStatus.Failure:
                        default:
                            ModelState.AddModelError("", "Invalid login attempt. Or User and password do not match");
                            return View("Login");
                    }
                }
                // If we got this far, something failed, redisplay form
                return View("Login");
            }
        }
        #endregion  Login     

        #region Register
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
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var CheckEmail    = _dbhelpers.FindEmail(model.RegisterEmail); //check if such mail already in db
                var CheckUserName = _dbhelpers.FindUserName(model.RegisterUsername); //check if such username already in db

                var user = new ApplicationUser //create new user using identity
                {                         
                    UserName            = model.RegisterUsername,
                    Email               = model.RegisterEmail,                    
                    FirstName           = model.FirstName,
                    LastName            = model.LastName,
                    Country             = model.Country,
                    City                = model.City,
                    Gender              = model.Gender,
                    BirthDate           = model.BirthDate,
                    YourSelfDescription = model.YourSelfDescription,
                    JoinDate            = DateTime.Now,                    
                    LastLoginDate       = DateTime.Now,                    
                    EmailConfirmed = true         //kapitowka
                };
                //check if there no such mail or username in already in use
                if (CheckEmail == null && CheckUserName == null)
                {
                    var result = await UserManager.CreateAsync(user, model.RegisterPassword);
                    if (result.Succeeded)
                    {
                        UserManager.AddToRole(user.Id, "Member"); //add new user to members group

                        // Send an email with this link (app_start identityconfig)
                        string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
                        var callbackUrl = Url.Action("ConfirmEmail", "Account",
                           new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
                        await UserManager.SendEmailAsync(user.Id,
                           "Confirm your account", "Please confirm your account by clicking <a href=\""
                           + callbackUrl + "\">here</a>");

                        //Message after registration
                        ViewBag.Message = "Check your email and confirm your account, you must be confirmed "
                                        + "before you can log in.";

                        return View("Info");
                    }
                    AddErrors(result);
                }
                else
                {
                    if (CheckEmail != null)
                    {
                        ModelState.AddModelError("", "Email is already registered.");
                    }
                    if (CheckUserName != null)
                    {
                        ModelState.AddModelError("", "Username " + model.RegisterUsername.ToLower() + " is already taken.");
                    }
                }
            }
            // If we got this far, something failed, redisplay form
            return View(model);
        }
        #endregion Register

        #region ExternalLogin
        // POST: /Account/ExternalLogin
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult ExternalLogin(string provider, string returnUrl)
        {
            // Request a redirect to the external login provider
            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl }));
        }

        //
        // GET: /Account/SendCode - code from external login provider
        [AllowAnonymous]
        public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
        {
            var userId = await SignInManager.GetVerifiedUserIdAsync();
            if (userId == null)
            {
                return View("Error");
            }
            var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
            var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
            return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/SendCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SendCode(SendCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            // Generate the token and send it
            if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
            {
                return View("Error");
            }
            return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
        }

        //
        // GET: /Account/ExternalLoginCallback
        [AllowAnonymous]
        public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
        {
            string userid = null;
            string LogUserName = null;
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
            if (loginInfo == null)
            {
                return RedirectToAction("Login");
            }

            //No need to mail conf, because we register under logged provider
            string userprokey = loginInfo.Login.ProviderKey;
            userid  = _dbhelpers.FindUserId(userprokey); //get userId by login provider key
            if (userid != null)
            {
                LogUserName = _dbhelpers.FindUserNameById(userid); //get username
            }
            // Sign in the user with this external login provider if the user already has a login
            var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
            switch (result)
            {
                case SignInStatus.Success:
                    _dbhelpers.UpdateLastLoginDate(LogUserName); //update last login date
                    _dbhelpers.UpdateOnlineStatus(LogUserName);  //update online status
                    return RedirectToLocal(returnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.RequiresVerification:
                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
                case SignInStatus.Failure:
                default:
                    // If the user does not have an account, then prompt the user to create an account
                    ViewBag.ReturnUrl = returnUrl;
                    ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
                    return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
            }
        }

        //
        // POST: /Account/ExternalLoginConfirmation
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Manage");
            }

            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await AuthenticationManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    return View("ExternalLoginFailure");
                }

                var CheckEmail    = _dbhelpers.FindEmail(model.Email); //check if such mail already in db
                var CheckUserName = _dbhelpers.FindUserName(model.ExtUsername); //check if such username already in db
                var user = new ApplicationUser
                {
                    UserName            = model.ExtUsername,
                    Email               = model.Email,
                    FirstName           = model.ExtFirstName,
                    LastName            = model.ExtLastName,
                    Country             = model.ExtCountry,
                    City                = model.ExtCity,
                    Gender              = model.ExtGender,
                    BirthDate           = model.ExtBirthDate,
                    YourSelfDescription = model.ExtYourSelfDescription,
                    JoinDate            = DateTime.Now,
                    LastLoginDate       = DateTime.Now,

                };
                if (CheckEmail == null && CheckUserName == null)
                {
                    //var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                    var result = await UserManager.CreateAsync(user);
                    if (result.Succeeded)
                    {
                        UserManager.AddToRole(user.Id, "Member");
                        result = await UserManager.AddLoginAsync(user.Id, info.Login);
                        if (result.Succeeded)
                        {
                            await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                            return RedirectToLocal(returnUrl);
                        }
                    }
                    AddErrors(result);
                }
                else
                {
                    if (CheckEmail != null)
                    {
                        ModelState.AddModelError("", "Email is already registered.");
                    }
                    if (CheckUserName != null)
                    {
                        ModelState.AddModelError("", "Username " + model.ExtUsername.ToLower() + " is already taken.");
                    }
                }
            }

            ViewBag.ReturnUrl = returnUrl;
            return View("ExternalLoginConfirmation");
            //return View(model);
        }

        //
        // GET: /Account/ExternalLoginFailure
        [AllowAnonymous]
        public ActionResult ExternalLoginFailure()
        {
            return View();
        }
        #endregion ExternalLogin     

        #region ForgotPassword
        //
        // GET: /Account/ForgotPassword
        [AllowAnonymous]
        public ActionResult ForgotPassword()
        {
            return View();
        }

        //using users mail to reset password
        // POST: /Account/ForgotPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await UserManager.FindByEmailAsync(model.Email);  //find user by entered mail
                if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
                {
                    // Don't reveal that the user does not exist or is not confirmed
                    return View("ForgotPasswordFail");
                }
                // Send an email with this link (app_start identityconfig)
                var code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { UserId = user.Id, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(user.Id, "Reset Password",
                    "Please reset your password by clicking here: <a href=\"" + callbackUrl + "\">link</a>");
                return View("ForgotPasswordConfirmation");
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Account/ForgotPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        //
        // GET: /Account/ForgotPasswordFail
        [AllowAnonymous]
        public ActionResult ForgotPasswordFail()
        {
            return View();
        }
        #endregion ForgotPassword

        #region resetPassword
        //
        // GET: /Account/ResetPassword
        [AllowAnonymous]
        public ActionResult ResetPassword(string code)
        {            
            return code == null ? View("Error") : View();
        }

        //
        // POST: /Account/ResetPassword
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            //after confirm link forgot password enter new password
            var user = await UserManager.FindByEmailAsync(model.Email); 
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation", "Account");
            }
            AddErrors(result);
            return View();
        }

        //
        // GET: /Account/ResetPasswordConfirmation
        [AllowAnonymous]
        public ActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        #endregion resetPassword    

        #region VerifyCode for TwoFactorSignIn

        // GET: /Account/VerifyCode
        [AllowAnonymous]
        public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
        {
            // Require that the user has already logged in via username/password or external login
            if (!await SignInManager.HasBeenVerifiedAsync())
            {
                return View("Error");
            }
            return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
        }

        //
        // POST: /Account/VerifyCode
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // The following code protects for brute force attacks against the two factor codes. 
            // If a user enters incorrect codes for a specified amount of time then the user account 
            // will be locked out for a specified amount of time. 
            // You can configure the account lockout settings in IdentityConfig
            var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(model.ReturnUrl);
                case SignInStatus.LockedOut:
                    return View("Lockout");
                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Invalid code.");
                    return View(model);
            }
        }
        #endregion VerifyCode for TwoFactorSignIn

        #region LogOff
        //
        // POST: /Account/LogOff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff(string ReturnUrl, string UserOff)
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            _dbhelpers.UpdateOfflineStatus(UserOff);
            return RedirectToLocal(ReturnUrl);
            //return RedirectToAction("Index", "Home");
        }
        #endregion LogOff      

        #region Helpers
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_userManager != null)
                {
                    _userManager.Dispose();
                    _userManager = null;
                }

                if (_signInManager != null)
                {
                    _signInManager.Dispose();
                    _signInManager = null;
                }
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

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                //do something here in future will be added more values
                if(returnUrl.StartsWith("/Account") || returnUrl.StartsWith("/Manage"))
                { return RedirectToAction("Index", "Home"); }
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        internal class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri   = redirectUri;
                UserId        = userId;
            }

            public string LoginProvider { get; set; }
            public string RedirectUri { get; set; }
            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }        

        //Get from some forum - list of countries in system
        public static IEnumerable<SelectListItem> GetCountries()
        {
            RegionInfo country = new RegionInfo(new CultureInfo("en-US", false).LCID);
            List<SelectListItem> countryNames = new List<SelectListItem>();
            string cult = CultureInfo.CurrentCulture.EnglishName;
            string count = cult.Substring(cult.IndexOf('(') + 1,
                             cult.LastIndexOf(')') - cult.IndexOf('(') - 1);
            //To get the Country Names from the CultureInfo installed in windows
            foreach (CultureInfo cul in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                country = new RegionInfo(new CultureInfo(cul.Name, false).LCID);
                countryNames.Add(new SelectListItem()
                {
                    Text     = country.DisplayName,
                    Value    = country.DisplayName,
                    Selected = count == country.EnglishName
                });                
            }
            countryNames.Add(new SelectListItem()
            {
                Text     = "",
                Value    = "",
                Selected = count == country.EnglishName
            });
            //Assigning all Country names to IEnumerable
            IEnumerable<SelectListItem> nameAdded =
                countryNames.GroupBy(x => x.Text).Select(
                    x => x.FirstOrDefault()).ToList<SelectListItem>()
                    .OrderBy(x => x.Text);
            return nameAdded;
        }
        #endregion Helpers
    }
}