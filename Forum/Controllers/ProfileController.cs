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
        private IProfileFunctions _profileFunctions;
        public DbHelpers _dbhelpers = new DbHelpers(); 

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager   _userManager;

        public ProfileController()
        {
            _profileFunctions = new ProfileFunctions(new ForumDbContext(), new ApplicationDbContext());
        }

        public ProfileController(IProfileFunctions profileFunctions, DbHelpers dbhelpers, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            _dbhelpers        = dbhelpers;
            _profileFunctions = profileFunctions;
            UserManager       = userManager;
            SignInManager     = signInManager;
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
            var userMail = _profileFunctions.GetUserMalilByUserName(Uzver);
            
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
            var userMail = _profileFunctions.GetUserMalilByUserName(Uzver);

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

        #region comments
        [ChildActionOnly]
        public ActionResult Comments(ProfileViewModel model, string ProfileUrl) //Request.RawUrl
        {
            var UserId = _profileFunctions.GetUserIdByUserName(model.UserName);
            var profileComments = _profileFunctions.GetProfileComments(UserId).OrderByDescending(d => d.DateTime).ToList();
            //var profiletReplies = _profileFunctions.GetProfileReplies(UserId);

            foreach (var comment in profileComments)
            {
                if (comment.CommentWallReplies != null) comment.CommentWallReplies.Clear();
                List<CommentWallViewModel> replies = _profileFunctions.GetProfileParentReplies(comment);
                foreach (var reply in replies)
                {
                    var rep = _profileFunctions.GetProfileReplyById(reply.Id);
                    comment.CommentWallReplies.Add(rep);
                }
            }
            //ProfileUrl.Replace("/Profile/FullProfile", "");
            model.ProfileUrl = ProfileUrl;
            model.CommentWall = profileComments;
            return PartialView(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewComment(string commentBody, string profileUrl, string profUserName, string comUserName)
        {
            var profileId = _profileFunctions.GetUserIdByUserName(profUserName);
            var commentWall = new CommentWall()
            {
                ProfileId = profileId,
                DateTime = DateTime.Now,
                UserName = comUserName,
                Body     = commentBody,
            };
            _profileFunctions.AddNewWallComment(commentWall);
            //return RedirectToAction("FullProfile", new { slug = slug });
            return RedirectToAction(profileUrl);
        }

        public PartialViewResult Replies()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewParentReply(string replyBody, int commentid, string ProfileUrl, string profUserName, string comUserName)
        {
            var profileId = _profileFunctions.GetUserIdByUserName(profUserName);
            var commentWallreply = new CommentWallReply()
            {
                CommentId     = commentid,
                ProfileId     = profileId,
                ParentReplyId = null,
                DateTime      = DateTime.Now,
                UserName      = comUserName,
                Body          = replyBody,

            };
            _profileFunctions.AddNewWallReply(commentWallreply);
            //return RedirectToAction("FullProfile", new { slug = slug });
            return RedirectToAction(ProfileUrl);
        }

        public PartialViewResult ChildReplies()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewChildReply(int preplyid, string comUserName, string replyBody, string ProfileUrl)
        {
            var preply = _profileFunctions.GetProfileReplyById(preplyid);

            var commentWallreply = new CommentWallReply()
            {
                ProfileId     = preply.ProfileId,
                CommentId     = preply.CommentId,
                ParentReplyId = preply.Id,
                DateTime      = DateTime.Now,
                UserName      = comUserName,
                Body          = replyBody,
            };

            _profileFunctions.AddNewWallReply(commentWallreply);
            //Think how we gat full profile here
            //return RedirectToAction("FullProfile", new { slug = _profileFunctions.GetPosts().Where(x => x.Id == preply.PostId).FirstOrDefault().UrlSeo });
            //return RedirectToAction( _profileFunctions.GetUsers().Where(x => x.Id == preply.ProfileId).FirstOrDefault().ProfileUrl );
            return RedirectToAction(ProfileUrl);
        }
        #endregion

        #region Helpers
        public static string TimePassed(DateTime postDate)
        {
            string date = null;
            double dateDiff = 0.0;
            var timeDiff = DateTime.Now - postDate;
            var yearPassed = timeDiff.TotalDays / 365;
            var monthPassed = timeDiff.TotalDays / 30;
            var dayPassed = timeDiff.TotalDays;
            var hourPassed = timeDiff.TotalHours;
            var minutePassed = timeDiff.TotalMinutes;
            var secondPassed = timeDiff.TotalSeconds;
            if (Math.Floor(yearPassed) > 0)
            {
                dateDiff = Math.Floor(yearPassed);
                date = dateDiff == 1 ? dateDiff + " year ago" : dateDiff + " years ago";
            }
            else
            {
                if (Math.Floor(monthPassed) > 0)
                {
                    dateDiff = Math.Floor(monthPassed);
                    date = dateDiff == 1 ? dateDiff + " month ago" : dateDiff + " months ago";
                }
                else
                {
                    if (Math.Floor(dayPassed) > 0)
                    {
                        dateDiff = Math.Floor(dayPassed);
                        date = dateDiff == 1 ? dateDiff + " day ago" : dateDiff + " days ago";
                    }
                    else
                    {
                        if (Math.Floor(hourPassed) > 0)
                        {
                            dateDiff = Math.Floor(hourPassed);
                            date = dateDiff == 1 ? dateDiff + " hour ago" : dateDiff + " hours ago";
                        }
                        else
                        {
                            if (Math.Floor(minutePassed) > 0)
                            {
                                dateDiff = Math.Floor(minutePassed);
                                date = dateDiff == 1 ? dateDiff + " minute ago" : dateDiff + " minutes ago";
                            }
                            else
                            {
                                dateDiff = Math.Floor(secondPassed);
                                date = dateDiff == 1 ? dateDiff + " second ago" : dateDiff + " seconds ago";
                            }
                        }
                    }
                }
            }

            return date;
        }

        public string[] CommentDetails(CommentWall comment)
        {
            string[] commentDetails = new string[13];

            commentDetails[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(comment.UserName);//username            
            var pathToFile = "/Content/images/profile/" + commentDetails[0] + "/" + commentDetails[0] + ".png";
            if (!System.IO.File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(pathToFile)))
            {
                pathToFile = "/Content/images/profile/Default.png";
            }
            commentDetails[1] = pathToFile + "?time=" + DateTime.Now.ToString();
            commentDetails[2] = TimePassed(comment.DateTime);//passed time
            commentDetails[3] = comment.DateTime.ToLongDateString().Replace(comment.DateTime.DayOfWeek.ToString() + ", ", "");//comment date
            commentDetails[4] = "parComment" + comment.Id; //grandparentid
            commentDetails[5] = "mComment" + comment.Id; //maincommentId
            commentDetails[6] = "comReplies" + comment.Id; //repliesId
            commentDetails[7] = "comtExpress" + comment.Id; //commentExpid
            commentDetails[8] = "ctrlExpand" + comment.Id; //ctrlExpId    
            commentDetails[9] = "comtText" + comment.Id; //comText
            commentDetails[10] = "commTextArea" + comment.Id; //comTextdiv
            commentDetails[11] = "comReply" + comment.Id; //Reply
            commentDetails[12] = "commentCtrl" + comment.Id; //commentControl  

            return commentDetails;
        }

        //obtain unique named Id for view elemets in comment replies section
        public string[] ReplyDetails(int replyId)
        {
            string[] replyDetails = new string[13];
            var reply = _profileFunctions.GetProfileReplyById(replyId);

            replyDetails[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(reply.UserName);//username            
            var pathToFile = "/Content/images/profile/" + replyDetails[0] + "/" + replyDetails[0] + ".png";
            if (!System.IO.File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(pathToFile)))
            {
                pathToFile = "/Content/images/profile/Default.png";
            }
            replyDetails[1] = pathToFile + "?time=" + DateTime.Now.ToString();
            replyDetails[2] = TimePassed(reply.DateTime); //passed time
            replyDetails[3] = reply.DateTime.ToLongDateString().Replace(reply.DateTime.DayOfWeek.ToString() + ", ", ""); //reply date
            replyDetails[4] = "parComment" + replyId; //grandparentid
            replyDetails[5] = "parReply" + replyId; //parentreplyId
            replyDetails[6] = "comReplies" + replyId; //repliesId
            replyDetails[7] = "comExpress" + replyId; //commentExpid
            replyDetails[8] = "ctrlExpand" + replyId; //ctrlExpId        
            replyDetails[9] = "comText" + replyId; //comText
            replyDetails[10] = "comTextArea" + replyId; //comTextdiv
            replyDetails[11] = "commReply" + replyId; //Reply
            replyDetails[12] = "commCtrl" + replyId; //commentControl

            return replyDetails;
        }

        public List<CommentWallViewModel> GetProfileChildReplies(CommentWallReply parentReply)
        {
            return _profileFunctions.GetProfileChildReplies(parentReply);
        }

        public int CountProfileComments(string profUserName)
        {
            var profileId = _profileFunctions.GetUserIdByUserName(profUserName);
            var repliesCount = _profileFunctions.GetProfileComments(profileId).Count();
            var commentsCount = _profileFunctions.GetProfileReplies(profileId).Count();

            return commentsCount + repliesCount;
        }
        #endregion
    }
}