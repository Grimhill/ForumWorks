using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System.IO;
using PagedList;
using Forum.Models;
using Forum.Functionality;
using System.Data.SqlClient;
using System.Configuration;
using System.Text.RegularExpressions;

namespace Forum.Controllers
{
    public class ProfileController : Controller
    {
        private IForumFunctions _forumFunctions;
        public DbHelpers _dbhelpers = new DbHelpers(); 

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager   _userManager;

        public ProfileController()
        {
            _forumFunctions = new ForumFunctions(new ForumDbContext(), new ApplicationDbContext());
        }

        public ProfileController(IForumFunctions forumFunctions, DbHelpers dbhelpers, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            _dbhelpers      = dbhelpers;
            _forumFunctions = forumFunctions;
            UserManager     = userManager;
            SignInManager   = signInManager;
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

        //[HttpPost]  
        //public ActionResult UpdateStatus(string Username, string status)
        //{
        //    if(Username == null)
        //    { return View(); }
        //    if(status == "true")
        //    { _dbhelpers.UpdateOnlineStatus(Username);} 
        //    else { _dbhelpers.UpdateOfflineStatus(Username); }          
        //    return View();
        //}  

        [HttpPost]
        public ActionResult UpdateStatus(string Username, string status)
        {
            string responce = "";
            if (Username == null)            {
                responce = "Unlogged"; 
                return Json(responce);
            }
            if (status == "true")
            {
                responce = "Status online";
                _dbhelpers.UpdateOnlineStatus(Username);
                return Json(responce);
            }
            else {
                responce = "Status offline";
                _dbhelpers.UpdateOfflineStatus(Username);
                return Json(responce);
            }
            //return Json(responce);
        }

        [HttpGet]
        public ActionResult ViewProfile(string Uzver)
        {
            //var userId   = _forumFunctions.GetUserIdByUserName(Author);
            var userMail = _forumFunctions.GetUserMalilByUserName(Uzver);
            
            ApplicationUser  user   = UserManager.FindByEmail(userMail);
            ProfileViewModel model = new ProfileViewModel
            {
                UserName            = user.UserName,
                UserRole            = _dbhelpers.CheckUserRole(user.UserName),// GetUserRole(user.UserName),                
                YourSelfDescription = user.YourSelfDescription,
                Registered          = user.JoinDate,
                LastLogin           = user.LastLoginDate,
                OnlineStatus        = user.OnlineStatus
            };
            return PartialView(model);
        }

        [HttpGet]
        public ActionResult FullProfile(string Uzver)
        {
            var userMail = _forumFunctions.GetUserMalilByUserName(Uzver);

            ApplicationUser user = UserManager.FindByEmail(userMail);
            ProfileViewModel model = new ProfileViewModel
            {
                UserName            = user.UserName,
                UserRole            = _dbhelpers.CheckUserRole(user.UserName), //   GetUserRole(user.UserName),
                FirstName           = user.FirstName,
                LastName            = user.LastName,
                Country             = user.Country,
                City                = user.City,
                Gender              = user.Gender,
                BirthDate           = user.BirthDate,
                YourSelfDescription = user.YourSelfDescription,
                Registered          = user.JoinDate,
                LastLogin           = user.LastLoginDate,
                OnlineStatus        = user.OnlineStatus
            };
            return View(model);
        }

        #region Helpers        

        #endregion
    }
}