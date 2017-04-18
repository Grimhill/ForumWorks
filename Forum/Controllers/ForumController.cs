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

namespace Forum.Controllers
{
    public class ForumController : Controller
    {
        private IForumFunctions _forumFunctions;
        public DbHelpers _dbhelpers = new DbHelpers();
        
        public static List<ForumViewModel> postList             = new List<ForumViewModel>();
        public static List<AllPostsViewModel> allPostsList      = new List<AllPostsViewModel>();
        public static List<AllPostsViewModel> checkCategoryList = new List<AllPostsViewModel>();
        public static List<AllPostsViewModel> checkTagList      = new List<AllPostsViewModel>();

        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager   _userManager;

        
        public ForumController()
        {
            _forumFunctions = new ForumFunctions(new ForumDbContext(), new ApplicationDbContext());
        }

        public ForumController(IForumFunctions forumFunctions, DbHelpers dbhelpers, ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            _forumFunctions = forumFunctions;
            _dbhelpers      = dbhelpers;
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

        #region Index

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Index(int? page, string sortOrder, string searchString, string[] searchCategory, string[] searchTag)
        {
            checkCategoryList.Clear();
            checkTagList.Clear();
            CreateCatAndTagList();

            Posts(page, sortOrder, searchString, searchCategory, searchTag);
            return View();
        }
        #endregion Index

        #region Posts/AllPosts

        [ChildActionOnly]
        public ActionResult Posts(int? page, string sortOrder, string searchString, string[] searchCategory, string[] searchTag)
        {
            postList.Clear();

            ViewBag.CurrentSort           = sortOrder;
            ViewBag.CurrentSearchString   = searchString;
            ViewBag.CurrentSearchCategory = searchCategory;
            ViewBag.CurrentSearchTag      = searchTag;
            ViewBag.DateSortParm          = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.BestSortParm          = sortOrder == "Best" ? "All" : "Best";


            var posts = _forumFunctions.GetPosts();
            foreach (var post in posts)
            {
                var postCategories = GetPostCategories(post.Id);
                var postTags = GetPostTags(post.Id);
                var likes    = _forumFunctions.LikeDislikeCount("postlike", post.Id);
                var dislikes = _forumFunctions.LikeDislikeCount("postdislike", post.Id);
                postList.Add(new ForumViewModel()
                {
                    Post             = post,
                    Modified         = post.Modified,
                    Title            = post.Title,
                    ShortDescription = post.ShortDescription,
                    PostedOn         = post.PostedOn,
                    Id               = post.Id,
                    Author           = post.Author,
                    PostModifedBy    = post.PostModifedBy,
                    PostLikes        = likes,
                    PostDislikes     = dislikes,
                    LikeCount        = post.LikeCount,
                    PostCategories   = postCategories,
                    PostTags         = postTags,
                    UrlSlug          = post.UrlSeo });
            }

            if (searchString != null)
            {
                postList = postList.Where(x => x.Title.ToLower().Contains(searchString.ToLower())).ToList();
            }

            //filter by category search on main page
            if (searchCategory != null)
            {
                List<ForumViewModel> newlist = new List<ForumViewModel>();
                foreach (var catName in searchCategory)
                {
                    foreach (var item in postList)
                    {
                        if (item.PostCategories.Where(x => x.Name == catName).Any())
                        {
                            newlist.Add(item);
                        }
                    }
                    foreach (var item in checkCategoryList)
                    {
                        if (item.Category.Name == catName)
                        {
                            item.Checked = true;
                        }
                    }
                }
                postList = postList.Intersect(newlist).ToList();
            }

            //filter by tag search on main page
            if (searchTag != null)
            {
                List<ForumViewModel> newlist = new List<ForumViewModel>();
                foreach (var tagName in searchTag)
                {
                    foreach (var item in postList)
                    {
                        if (item.PostTags.Where(x => x.Name == tagName).Any())
                        {
                            newlist.Add(item);
                        }
                    }
                    foreach (var item in checkTagList)
                    {
                        if (item.Tag.Name == tagName)
                        {
                            item.Checked = true;
                        }
                    }
                }
                postList = postList.Intersect(newlist).ToList();
            }

            switch (sortOrder)
            {
                case "date_desc":
                    postList = postList.OrderByDescending(x => x.PostedOn).ToList();
                    break;
                case "Best":
                    postList = postList.Where(x => x.LikeCount >= 2).OrderByDescending(x => x.PostedOn).ToList();
                    break;
                case "All":
                    postList = postList.OrderBy(x => x.PostedOn).ToList();
                    break;
                default:
                    postList = postList.OrderBy(x => x.PostedOn).ToList();
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);

            return PartialView("Posts", postList.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult AllPosts(int? page, string sortOrder, string searchString, string[] searchCategory, string[] searchTag)
        {
            allPostsList.Clear();
            checkCategoryList.Clear();
            checkTagList.Clear();

            ViewBag.CurrentSort           = sortOrder;
            ViewBag.CurrentSearchString   = searchString;
            ViewBag.CurrentSearchCategory = searchCategory;
            ViewBag.CurrentSearchTag      = searchTag;
            ViewBag.DateSortParm          = string.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewBag.TitleSortParm         = sortOrder == "Title" ? "title_desc" : "Title";

            var posts = _forumFunctions.GetPosts();
            foreach (var post in posts)
            {
                var postCategories = GetPostCategories(post.Id);
                var postTags       = GetPostTags(post.Id);
                allPostsList.Add(new AllPostsViewModel()
                {
                    PostId         = post.Id,
                    Date           = post.PostedOn,
                    Description    = post.ShortDescription,
                    Title          = post.Title,
                    PostCategories = postCategories,
                    Meta           = post.Meta,
                    PostTags       = postTags,
                    Author         = post.Author,
                    Modified       = post.Modified,
                    PostModifedBy  = post.PostModifedBy,
                    isPublished    = post.isPublished,
                    UrlSlug        = post.UrlSeo });
            }

            if (searchString != null)
            {
                allPostsList = allPostsList.Where(x => x.Title.ToLower().Contains(searchString.ToLower())).ToList();
            }

            CreateCatAndTagList();

            //filter by category search in posts
            if (searchCategory != null)
            {
                List<AllPostsViewModel> newlist = new List<AllPostsViewModel>();
                foreach (var catName in searchCategory)
                {
                    foreach (var item in allPostsList)
                    {
                        if (item.PostCategories.Where(x => x.Name == catName).Any())
                        {
                            newlist.Add(item);
                        }
                    }
                    foreach (var item in checkCategoryList)
                    {
                        if (item.Category.Name == catName)
                        {
                            item.Checked = true;
                        }
                    }
                }
                allPostsList = allPostsList.Intersect(newlist).ToList();
            }

            //filter by tag search in posts
            if (searchTag != null)
            {
                List<AllPostsViewModel> newlist = new List<AllPostsViewModel>();
                foreach (var tagName in searchTag)
                {
                    foreach (var item in allPostsList)
                    {
                        if (item.PostTags.Where(x => x.Name == tagName).Any())
                        {
                            newlist.Add(item);
                        }
                    }
                    foreach (var item in checkTagList)
                    {
                        if (item.Tag.Name == tagName)
                        {
                            item.Checked = true;
                        }
                    }
                }
                allPostsList = allPostsList.Intersect(newlist).ToList();
            }

            switch (sortOrder)
            {
                case "date_desc":
                    allPostsList = allPostsList.OrderByDescending(x => x.Date).ToList();
                    break;
                case "Title":
                    allPostsList = allPostsList.OrderBy(x => x.Title).ToList();
                    break;
                case "title_desc":
                    allPostsList = allPostsList.OrderByDescending(x => x.Title).ToList();
                    break;
                default:
                    allPostsList = allPostsList.OrderBy(x => x.Date).ToList();
                    break;
            }

            int pageSize = 10;
            int pageNumber = (page ?? 1);
            return View("AllPosts", allPostsList.ToPagedList(pageNumber, pageSize));

        }
        #endregion Posts/AllPosts

        #region Post

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Post(string sortOrder, string slug)
        {
            PostViewModel model = new PostViewModel();            
            var postid = _forumFunctions.GetPostIdBySlug(slug);
            var post   = _forumFunctions.GetPostById(postid);
           
            model.Id            = post.Id;            
            model.UrlSeo        = post.UrlSeo;            
            model.Title         = post.Title;
            model.Body          = post.Body;
            model.Author        = post.Author;
            model.PostedOn      = post.PostedOn;
            model.Modified      = post.Modified;
            model.PostModifedBy = post.PostModifedBy;
            model.PostLikes     = _forumFunctions.LikeDislikeCount("postlike", post.Id);
            model.PostDislikes  = _forumFunctions.LikeDislikeCount("postdislike", post.Id);
            Comments(model, post, sortOrder);
            return View(model);
        }

        //[Authorize(Roles = "Admin, Moderator")] //also roles in view blog\index        
        [HttpGet]
        public ActionResult AddNewPost()
        {            
            return View();
        }
        
        //[Authorize(Roles = "Admin, Moderator")]        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult AddNewPost(PostViewModel model)
        {
            Random rndId = new Random(); 
            var strCurrentUserId = User.Identity.GetUserId();
            var post = new Post
            {                
                Body             = model.Body,
                Meta             = model.Meta,
                PostedOn         = DateTime.Now,
                isPublished      = false,  //published after moderation
                ShortDescription = model.ShortDescription,
                Title            = model.Title,
                Author           = _forumFunctions.GetUserById(strCurrentUserId),
                //Author           = User.Identity.GetUserName(), //another variant
                UrlSeo           = model.Title.Replace(" ", "-") + + rndId.Next(100) //unique urlseo for slug                
            };
            _forumFunctions.AddNewPost(post);
            return RedirectToAction("EditPost", "Forum", new { slug = post.UrlSeo });            
        }
               
        //[Authorize(Roles = "Admin, Moderator")]
        [HttpGet]
        public ActionResult EditPost(string slug)
        {
            var model = CreatePostViewModel(slug);
            return View(model);
        }
        
        //[Authorize(Roles = "Admin, Moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditPost(PostViewModel model)
        {
            var strCurrentUserId  = User.Identity.GetUserId(); //get current user id
            var postId = _forumFunctions.GetPostIdBySlug(model.UrlSeo);
            var post = _forumFunctions.GetPostById(postId);
            
            post.Body             = model.Body;
            post.Title            = model.Title;
            post.Meta             = model.Meta;
            post.UrlSeo           = model.UrlSeo;
            post.ShortDescription = model.ShortDescription;
            post.Modified         = DateTime.Now;
            post.PostModifedBy    = _forumFunctions.GetUserById(strCurrentUserId);
            post.isPublished      = model.isPublished;
            _forumFunctions.Save();

            return RedirectToAction("Post", new { slug = model.UrlSeo });
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult DeletePost(PostViewModel model, int postid)
        {
            model.Id = postid;
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePost(int postId)
        {
            _forumFunctions.DeletePost(postId);
            return RedirectToAction("Index", "Forum");
        }

        public ActionResult UpdatePostLike(int postid, string slug, string username, string likeordislike)
        {
            _forumFunctions.UpdatePostLike(postid, username, likeordislike);
            return RedirectToAction("Post", new { slug = slug });
        }
        #endregion Post

        #region Categories and Tags
        //[Authorize(Roles = "Admin, Moderator")]
        [HttpGet]
        public ActionResult AddCategoryToPost(int postid)
        {
            var post = _forumFunctions.GetPostById(postid);
            PostViewModel model = new PostViewModel();

            model.Id         = postid;
            model.UrlSeo     = post.UrlSeo;
            model.Categories = _forumFunctions.GetCategories();
            return View(model);
        }

        //[Authorize(Roles = "Admin, Moderator")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddCategoryToPost(PostViewModel model)
        {
            var post     = _forumFunctions.GetPostById(model.Id);
            var postCats = _forumFunctions.GetPostCategories(post.Id);
            List<int> pCatIds = new List<int>();
            foreach (var pCat in postCats)
            {
                pCatIds.Add(pCat.Id);
            }

            var newCats = model.Categories.Where(x => x.Checked == true).ToList();
            List<int> nCatIds = new List<int>();
            foreach (var pCat in newCats)
            {
                nCatIds.Add(pCat.Id);
            }

            if (!pCatIds.SequenceEqual(nCatIds))
            {
                foreach (var pCat in postCats)
                {
                    _forumFunctions.RemovePostCategories(model.Id, pCat.Id);
                }
                foreach (var cat in model.Categories)
                {
                    PostCategory postCategory = new PostCategory();
                    if (cat.Checked == true)
                    {
                        postCategory.PostId     = model.Id;
                        postCategory.CategoryId = cat.Id;
                        postCategory.Checked    = true;
                        _forumFunctions.AddPostCategories(postCategory);
                    }
                }
                _forumFunctions.Save();
            }
            return RedirectToAction("EditPost", new { slug = post.UrlSeo });
        }

        //[Authorize(Roles = "Admin")]
        public ActionResult RemoveCategoryFromPost(string slug, int postid, string catName)
        {
            CreatePostViewModel(slug);
            _forumFunctions.RemoveCategoryFromPost(postid, catName);
            return RedirectToAction("EditPost", new { slug = slug });
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult AddNewCategory(int postid, bool callfrompost)
        {
            if (postid != 0 && callfrompost) //add new category adding\editing post or from categoriesandtags method
            {
                PostViewModel model = new PostViewModel();
                model.Id = postid;
                return View(model);
            }
            else
            {
                return View();
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewCategory(int postid, string catName, string catDesc) //string catUrlSeo, 
        {
            var result = RedirectToAction("CategoriesAndTags", "Forum", new { Message = "CategoryExist" });
            var CheckCategory = _dbhelpers.FindCategoty(catName);

            //chech if category with suck name already exists
            if (CheckCategory == null)
            {
                if (postid != 0)
                {
                    _forumFunctions.AddNewCategory(catName, catDesc); //catUrlSeo, 
                    return RedirectToAction("AddCategoryToPost", new { postid = postid });
                }
                else
                {
                    _forumFunctions.AddNewCategory(catName, catDesc); //catUrlSeo, 
                    return RedirectToAction("CategoriesAndTags", "Forum");
                }
            }
            else
            {
                return result;
            }           
        }

        ///[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult AddTagToPost(int postid)
        {
            var post = _forumFunctions.GetPostById(postid);
            PostViewModel model = new PostViewModel();

            model.Id     = postid;
            model.UrlSeo = post.UrlSeo;
            model.Tags   = _forumFunctions.GetTags();
            return View(model);
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTagToPost(PostViewModel model)
        {
            var post = _forumFunctions.GetPostById(model.Id);
            var postTags = _forumFunctions.GetPostTags(post.Id);

            List<int> pTagIds = new List<int>();
            foreach (var pTag in postTags)
            {
                pTagIds.Add(pTag.Id);
            }

            var newTags = model.Tags.Where(x => x.Checked == true).ToList();
            List<int> nTagIds = new List<int>();
            foreach (var pTag in newTags)
            {
                nTagIds.Add(pTag.Id);
            }
            if (!pTagIds.SequenceEqual(nTagIds))
            {
                foreach (var pTag in postTags)
                {
                    _forumFunctions.RemovePostTags(model.Id, pTag.Id);
                }
                foreach (var tag in model.Tags)
                {
                    PostTag postTag = new PostTag();
                    if (tag.Checked == true)
                    {
                        postTag.PostId  = model.Id;
                        postTag.TagId   = tag.Id;
                        postTag.Checked = true;
                        _forumFunctions.AddPostTags(postTag);
                    }
                }
                _forumFunctions.Save();
            }
            return RedirectToAction("EditPost", new { slug = post.UrlSeo });
        }

        //[Authorize(Roles = "Admin")]
        public ActionResult RemoveTagFromPost(string slug, int postid, string tagName)
        {
            CreatePostViewModel(slug);
            _forumFunctions.RemoveTagFromPost(postid, tagName);
            return RedirectToAction("EditPost", new { slug = slug });
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult AddNewTag(int postid, bool callfrompost)
        {
            if (postid != 0 && callfrompost) //add new tag adding\editing post or from categoriesandtags method
            {
                PostViewModel model = new PostViewModel();
                model.Id = postid;
                return View(model);
            }
            else
            {
                return View();
            }
        }

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewTag(int postid, string tagName) //, string tagUrlSeo
        {
            var result = RedirectToAction("CategoriesAndTags", "Forum", new { Message = "TagExist" });
            var CheckTag = _dbhelpers.FindTag(tagName);
            if (CheckTag == null)
            {
                if (postid != 0) //chech if tag with suck name already exists
                {
                    _forumFunctions.AddNewTag(tagName); //, tagUrlSeo
                    return RedirectToAction("AddTagToPost", new { postid = postid });
                }
                else
                {
                    _forumFunctions.AddNewTag(tagName); //, tagUrlSeo
                    return RedirectToAction("CategoriesAndTags", "Forum");
                }
            }
            else
            {
                return result;
            }            
        }

        //[Authorize(Roles = "Admin")]
        [HttpGet]
        public ActionResult CategoriesAndTags(string message)
        {
            ViewBag.StatusMessage =
                message   == "TagExist" ? " Such tag already exist"
                : message == "CategoryExist" ? " Such category already exist"
                : "";
            checkCategoryList.Clear();
            checkTagList.Clear();
            CreateCatAndTagList();
            return View();
        }  

        //[Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult RemoveCatAndTag(string[] categoryNames, string[] tagNames)
        {
            if (categoryNames != null)
            {
                foreach (var catName in categoryNames)
                {
                    var category = _forumFunctions.GetCategories().Where(x => x.Name == catName).FirstOrDefault();
                    _forumFunctions.RemoveCategory(category);
                }
            }
            if (tagNames != null)
            {
                foreach (var tagName in tagNames)
                {
                    var tag = _forumFunctions.GetTags().Where(x => x.Name == tagName).FirstOrDefault();
                    _forumFunctions.RemoveTag(tag);
                }
            }
            return RedirectToAction("CategoriesAndTags", "Forum");
        }

        [HttpGet]
        public ActionResult PostTagsAndCategory()
        {
            return PartialView();
        }
        #endregion Categoris and Tags

        #region comments
        [ChildActionOnly]
        public ActionResult Comments(PostViewModel model, Post post, string sortOrder)
        {
            ViewBag.CurrentSort  = sortOrder;
            ViewBag.DateSortParm = string.IsNullOrEmpty(sortOrder) ? "date_asc" : "";
            ViewBag.BestSortParm = sortOrder == "Best" ? "best_desc" : "Best";

            var postComments = _forumFunctions.GetPostComments(post.Id).OrderByDescending(d => d.DateTime).ToList();

            foreach (var comment in postComments)
            {
                var likes    = LikeDislikeCount("commentlike", comment.Id);
                var dislikes = LikeDislikeCount("commentdislike", comment.Id);

                comment.NetLikeCount = likes - dislikes;
                if (comment.Replies != null) comment.Replies.Clear();
                List<CommentViewModel> replies = _forumFunctions.GetParentReplies(comment);
                foreach (var reply in replies)
                {
                    var rep = _forumFunctions.GetReplyById(reply.Id);
                    comment.Replies.Add(rep);
                }
            }

            switch (sortOrder)
            {
                case "date_asc":
                    postComments = postComments.OrderBy(x => x.DateTime).ToList();
                    ViewBag.DateSortLink = "active";
                    break;
                case "Best":
                    postComments = postComments.OrderByDescending(x => x.NetLikeCount).ToList();
                    ViewBag.BestSortLink = "active";
                    break;
                case "best_desc":
                    postComments = postComments.OrderBy(x => x.NetLikeCount).ToList();
                    ViewBag.BestSortLink = "active";
                    break;
                default:
                    postComments = postComments.OrderByDescending(x => x.DateTime).ToList();
                    ViewBag.DateSortLink = "active";
                    break;
            }

            model.UrlSeo   = post.UrlSeo;
            model.Comments = postComments;
            return PartialView(model);
        }               

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewComment(string commentBody, string comUserName, string slug, int postid)
        {
            var comment = new Comment()
            {       
                PostId       = postid,
                DateTime     = DateTime.Now,
                UserName     = comUserName,
                Body         = commentBody,
                NetLikeCount = 0
            };
            _forumFunctions.AddNewComment(comment);
            return RedirectToAction("Post", new { slug = slug });
        }

        public PartialViewResult Replies()
        {
            return PartialView();
        }        

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewParentReply(string replyBody, string comUserName, int postid, int commentid, string slug)
        {
            var comDelChck = CommentDeleteCheck(commentid);
            if (!comDelChck)
            {      
                var reply = new Reply()
                {               
                    PostId        = postid,
                    CommentId     = commentid,
                    ParentReplyId = null,
                    DateTime      = DateTime.Now,
                    UserName      = comUserName,
                    Body          = replyBody,
                    NetLikeCount  = 0
                };
                _forumFunctions.AddNewReply(reply);
            }
            return RedirectToAction("Post", new { slug = slug });
        }

        public PartialViewResult ChildReplies()
        {
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult NewChildReply(int preplyid, string comUserName, string replyBody)
        {
            var repDelCheck = ReplyDeleteCheck(preplyid);
            var preply      = _forumFunctions.GetReplyById(preplyid);
            if (!repDelCheck)
            {      
                var reply = new Reply()
                {                   
                    PostId        = preply.PostId,
                    CommentId     = preply.CommentId,
                    ParentReplyId = preply.Id,
                    DateTime      = DateTime.Now,
                    UserName      = comUserName,
                    Body          = replyBody,
                    NetLikeCount  = 0
                };
                _forumFunctions.AddNewReply(reply);
            }
            return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == preply.PostId).FirstOrDefault().UrlSeo });
        }

        [HttpGet]
        public async Task<ActionResult> EditComment(CommentViewModel model, int commentid)
        {
            var user    = await GetCurrentUserAsync();
            var comment = _forumFunctions.GetCommentById(commentid);
            if (comment.UserName == user.UserName || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                model.Id       = commentid;
                model.Body     = comment.Body;                
                return View(model);
            }
            else
            {
                return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == comment.PostId).FirstOrDefault().UrlSeo });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditComment(int commentid, string commentBody)
        {
            var strCurrentUserId = User.Identity.GetUserId();
            
            var comment      = _forumFunctions.GetCommentById(commentid);
            comment.Body     = commentBody;

            if(User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                comment.EditTime = DateTime.Now;
            }
            comment.EditedBy = _forumFunctions.GetUserById(strCurrentUserId);           
            _forumFunctions.Save();
            return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == comment.PostId).FirstOrDefault().UrlSeo });
        }

        [HttpGet]
        public ActionResult DeleteComment(CommentViewModel model, int commentid)
        {
            model.Id = commentid;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]        
        public ActionResult DeleteComment(int commentid, bool deleteReplies)
        {
            var comment     = _forumFunctions.GetCommentById(commentid);
            var postid      = comment.PostId;
            var repliesList = _forumFunctions.GetParentReplies(comment);

            if (deleteReplies)
            {
                _forumFunctions.DeleteCommentWithReplies(commentid);
            }
            else
            {
                if (repliesList.Count() == 0)
                {
                    _forumFunctions.DeleteComment(commentid);
                }
                else
                {                    
                    comment.Body = "<p style=\"color:red;\"><i>This comment has been deleted.</i></p>";
                    comment.Deleted = true;
                    _forumFunctions.Save();
                }
            }
            return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == postid).FirstOrDefault().UrlSeo });
        }


        [HttpGet]
        public async Task<ActionResult> EditReply(CommentViewModel model, int replyid)
        {
            var user = await GetCurrentUserAsync();
            var reply = _forumFunctions.GetReplyById(replyid);
            if (reply.UserName == user.UserName || User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                model.Id   = replyid;
                model.Body = reply.Body;               
                return View(model);
            }
            else
            {
                return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == reply.PostId).FirstOrDefault().UrlSeo });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [ValidateInput(false)]
        public ActionResult EditReply(int replyid, string replyBody)
        {
            var strCurrentUserId = User.Identity.GetUserId();

            var reply  = _forumFunctions.GetReplyById(replyid);
            reply.Body = replyBody;
            if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                reply.EditTime = DateTime.Now;
            }

            reply.EditedBy = _forumFunctions.GetUserById(strCurrentUserId);            
            _forumFunctions.Save();
            return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == reply.PostId).FirstOrDefault().UrlSeo });
        }

        [HttpGet]
        public ActionResult DeleteReply(CommentViewModel model, int replyid)
        {            
           model.Id = replyid;
           return View(model);            
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteReply(int replyid, bool deleteReplies)
        {
            var reply = _forumFunctions.GetReplyById(replyid);
            var repliesList = _forumFunctions.GetChildReplies(reply);
            var postid = reply.PostId;

            if (deleteReplies)
            {
                _forumFunctions.DeleteReplyWithChildren(replyid);
            }
            else
            {
                if (repliesList.Count() == 0)
                {
                    _forumFunctions.DeleteReply(replyid);
                }
                else
                {                    
                    reply.Body = "<p style=\"color:red;\"><i>This comment has been deleted.</i></p>";
                    reply.Deleted = true;
                    _forumFunctions.Save();
                }
            }
            return RedirectToAction("Post", new { slug = _forumFunctions.GetPosts().Where(x => x.Id == postid).FirstOrDefault().UrlSeo });
        }

        public ActionResult UpdateCommentLike(int commentid, string username, string likeordislike, string slug)
        {
            _forumFunctions.UpdateCommentLike(commentid, username, likeordislike);

            return RedirectToAction("Post", new { slug = slug });
        }

        public ActionResult UpdateReplyLike(int replyid, string username, string likeordislike)
        {
            _forumFunctions.UpdateReplyLike(replyid, username, likeordislike);

            var slug = _forumFunctions.GetPostByReply(replyid).UrlSeo;
            return RedirectToAction("Post", "Forum", new { slug = slug });
        }
        #endregion comments

        #region Helpers

        private async Task<ApplicationUser> GetCurrentUserAsync()
        {
            return await UserManager.FindByIdAsync(User.Identity.GetUserId());
        }

        public List<CommentViewModel> GetChildReplies(Reply parentReply)
        {
            return _forumFunctions.GetChildReplies(parentReply);
        }

        public bool CommentDeleteCheck(int commentid)
        {
            return _forumFunctions.CommentDeleteCheck(commentid);
        }

        public bool ReplyDeleteCheck(int replyid)
        {
            return _forumFunctions.ReplyDeleteCheck(replyid);
        }

        //count time passed from comment was written
        public static string TimePassed(DateTime postDate)
        {
            string date = null;
            double dateDiff  = 0.0;
            var timeDiff     = DateTime.Now - postDate;
            var yearPassed   = timeDiff.TotalDays / 365;
            var monthPassed  = timeDiff.TotalDays / 30;
            var dayPassed    = timeDiff.TotalDays;
            var hourPassed   = timeDiff.TotalHours;
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

        //to do: delete excess members
        //obtain unique named Id for view elemets in comments section
        public string[] CommentDetails(Comment comment)
        {            
            string[] commentDetails = new string[14];

            commentDetails[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(comment.UserName);//username            
            var pathToFile = "/Content/images/profile/" + commentDetails[0] + "/" + commentDetails[0] + ".png";
            if (!System.IO.File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(pathToFile)))
            {
                pathToFile = "/Content/images/profile/Default.png";
            }            
            commentDetails[1] = pathToFile + "?time=" + DateTime.Now.ToString();            
            commentDetails[2] = TimePassed(comment.DateTime);//passed time
            commentDetails[3] = comment.DateTime.ToLongDateString().Replace(comment.DateTime.DayOfWeek.ToString() + ", ", "");//comment date
            commentDetails[4] = "parentComment" + comment.Id; //grandparentid
            commentDetails[5] = "mainComment" + comment.Id; //maincommentId
            commentDetails[6] = "commentReplies" + comment.Id; //repliesId
            commentDetails[7] = "commentExpress" + comment.Id; //commentExpid
            commentDetails[8] = "controlExpand" + comment.Id; //ctrlExpId    
            commentDetails[9] = "commentText" + comment.Id; //comText
            commentDetails[10] = "commetTextArea" + comment.Id; //comTextdiv
            commentDetails[11] = "commentReply" + comment.Id; //Reply
            commentDetails[12] = "commentCtrl" + comment.Id; //commentControl
            commentDetails[13] = "commentMenu" + comment.Id; //commentMenu
            
            return commentDetails;
        }

        //obtain unique named Id for view elemets in comment replies section
        public string[] ReplyDetails(int replyId)
        {
            string[] replyDetails = new string[14];
            var reply = _forumFunctions.GetReplyById(replyId);

            replyDetails[0] = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(reply.UserName);//username            
            var pathToFile = "/Content/images/profile/" + replyDetails[0] + "/" + replyDetails[0] + ".png";
            if (!System.IO.File.Exists(System.Web.Hosting.HostingEnvironment.MapPath(pathToFile)))
            {
                pathToFile = "/Content/images/profile/Default.png";
            }
            replyDetails[1] = pathToFile + "?time=" + DateTime.Now.ToString();
            replyDetails[2] = TimePassed(reply.DateTime); //passed time
            replyDetails[3] = reply.DateTime.ToLongDateString().Replace(reply.DateTime.DayOfWeek.ToString() + ", ", ""); //reply date
            replyDetails[4] = "parentComment" + replyId; //grandparentid
            replyDetails[5] = "parentReply" + replyId; //parentreplyId
            replyDetails[6] = "commentReplies" + replyId; //repliesId
            replyDetails[7] = "commentExpress" + replyId; //commentExpid
            replyDetails[8] = "controlExpand" + replyId; //ctrlExpId        
            replyDetails[9] = "commentText" + replyId; //comText
            replyDetails[10] = "commetTextArea" + replyId; //comTextdiv
            replyDetails[11] = "commentReply" + replyId; //Reply
            replyDetails[12] = "commentCtrl" + replyId; //commentControl
            replyDetails[13] = "commentMenu" + replyId; //commentMenu

            return replyDetails;
        }

        public int LikeDislikeCount(string typeAndlike, int id)
        {
            switch (typeAndlike)
            {
                case "commentlike":
                    return _forumFunctions.LikeDislikeCount("commentlike", id);
                case "commentdislike":
                    return _forumFunctions.LikeDislikeCount("commentdislike", id);
                case "replylike":
                    return _forumFunctions.LikeDislikeCount("replylike", id);
                case "replydislike":
                    return _forumFunctions.LikeDislikeCount("replydislike", id);
                default:
                    return 0;
            }
        }

        public IList<Post> GetPosts()
        {
            return _forumFunctions.GetPosts();
        }

        public IList<Category> GetPostCategories(int postId)
        {
            return _forumFunctions.GetPostCategories(postId);
        }

        public IList<Tag> GetPostTags(int postId)//Post post
        {
            return _forumFunctions.GetPostTags(postId);
        }

        public void CreateCatAndTagList()
        {
            foreach (var ct in _forumFunctions.GetCategories())
            {
                checkCategoryList.Add(new AllPostsViewModel { Category = ct, Checked = false });
            }
            foreach (var tg in _forumFunctions.GetTags())
            {
                checkTagList.Add(new AllPostsViewModel { Tag = tg, Checked = false });
            }
        }

        public PostViewModel CreatePostViewModel(string slug)
        {
            PostViewModel model = new PostViewModel();
            var postid = _forumFunctions.GetPostIdBySlug(slug);
            var post   = _forumFunctions.GetPostById(postid);

            model.Id               = postid;
            model.Title            = post.Title;
            model.Body             = post.Body;
            model.Meta             = post.Meta;
            model.UrlSeo           = post.UrlSeo;
            model.Author           = post.Author;
            model.PostModifedBy    = post.PostModifedBy;
            model.ShortDescription = post.ShortDescription;
            model.PostedOn         = post.PostedOn;
            model.isPublished      = post.isPublished;           
            model.PostCategories   = _forumFunctions.GetPostCategories(post.Id).ToList();
            model.PostTags         = _forumFunctions.GetPostTags(post.Id).ToList();
            
            return model;
        }

        public int CountPostComments(int postId)
        {
            var repliesCount = _forumFunctions.GetPostReplies(postId).Count();
            var commentsCount = _forumFunctions.GetPostComments(postId).Count();
            
            return commentsCount + repliesCount;            
        }     

        #endregion helpers
    }
}