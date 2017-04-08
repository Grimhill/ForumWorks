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
    public class CKEditorUploadController : Controller
    {
        //forumfunk and identity for future
        private IForumFunctions _forumFunctions;

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;


        public CKEditorUploadController()
        {
            _forumFunctions = new ForumFunctions(new ForumDbContext(), new ApplicationDbContext());
        }

        public CKEditorUploadController(IForumFunctions forumFunctions, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
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

        public ActionResult uploadPartial()
        {
            var Data = Server.MapPath("~/Content/images/postimages/");
            //view file, it`s name and upload date
            var images = Directory.GetFiles(Data).Select(x => new imagesviewmodel
            {
                Url  = Url.Content("~/Content/images/postimages/" + Path.GetFileName(x)),
                Name = Url.Content(Path.GetFileName(x)),
                Date = System.IO.File.GetCreationTime(Data + Path.GetFileName(x))
            });
            return PartialView(images);
        }

        public void uploadnow(HttpPostedFileWrapper upload)
        {
            if (upload != null)
            {
                string FileName = upload.FileName;
                string filepath = System.IO.Path.Combine(Server.MapPath("~/Content/"), FileName);

                if (System.IO.File.Exists(filepath)) 
                {
                    string folder    = Path.GetDirectoryName(filepath);
                    string filename  = Path.GetFileNameWithoutExtension(filepath);
                    string extension = Path.GetExtension(filepath);
                    int number = 0;

                    //if we have file, with name file (2).png etc
                    Match regex = Regex.Match(filepath, @"(.+) \((\d+)\)\.\w+");

                    if (regex.Success)
                    {
                        filename = regex.Groups[1].Value;
                        number = int.Parse(regex.Groups[2].Value);
                    }

                    do
                    {
                        number++;
                        filepath = Path.Combine(folder, string.Format("{0} ({1}){2}", filename, number, extension));
                    }
                    while (System.IO.File.Exists(filepath));
                    upload.SaveAs(filepath);
                }
                else
                {
                    upload.SaveAs(filepath);
                }
            }
        }
    }
}