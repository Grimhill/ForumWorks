using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using Forum.Models;
using Microsoft.AspNet.Identity.EntityFramework;

namespace Forum.Functionality
{
    public class ForumFunctions :IForumFunctions, IDisposable
    {
        private ForumDbContext _context;
        private ApplicationDbContext _userContext;
        public ForumFunctions(ForumDbContext context, ApplicationDbContext userContext)
        {
            _context     = context;
            _userContext = userContext;
        }

        #region 1
        public IList<Post> GetPosts()
        {
            return _context.Posts.ToList();
        }

        public IList<Tag> GetTags()
        {
            return _context.Tags.ToList();
        }

        public IList<Category> GetCategories()
        {
            return _context.Categories.ToList();
        }

        public IList<Category> GetPostCategories(int postId)
        {
            var categoryIds = _context.PostCategories.Where(p => p.PostId == postId).Select(p => p.CategoryId).ToList();
            List<Category> categories = new List<Category>();
            foreach (var catId in categoryIds)
            {
                categories.Add(_context.Categories.Where(p => p.Id == catId).FirstOrDefault());
            }
            return categories;
        }

        public IList<Tag> GetPostTags(int postId)
{
            var tagIds = _context.PostTags.Where(p => p.PostId == postId).Select(p => p.TagId).ToList();
            List<Tag> tags = new List<Tag>();
            foreach (var tagId in tagIds)
            {
                tags.Add(_context.Tags.Where(p => p.Id == tagId).FirstOrDefault());
            }
            return tags;
        }

        public int LikeDislikeCount(string typeAndChoice, int id)
        {
            int count = 0;
            switch (typeAndChoice)
            {
                case "postlike":
                    count = _context.PostLikes.Where(p => p.PostId == id && p.Like == true).Count();
                    return count;
                case "postdislike":
                    count = _context.PostLikes.Where(p => p.PostId == id && p.Dislike == true).Count();
                    return count;
                case "commentlike":
                    count = _context.CommentLikes.Where(p => p.CommentId == id && p.Like == true).Count();
                    return count;
                case "commentdislike":
                    count = _context.CommentLikes.Where(p => p.CommentId == id && p.Dislike == true).Count();
                    return count;
                case "replylike":
                    count = _context.ReplyLikes.Where(p => p.ReplyId == id && p.Like == true).Count();
                    return count;
                case "replydislike":
                    count = _context.ReplyLikes.Where(p => p.ReplyId == id && p.Dislike == true).Count();
                    return count;
                default:
                    return 0;
            }
        }
        #endregion

        #region post
        public Post GetPostById(int id)
        {
            return _context.Posts.Find(id);
        }

        //Warning id
        public int GetPostIdBySlug(string slug)
        {
            return _context.Posts.Where(x => x.UrlSeo == slug).FirstOrDefault().Id;//.ToString();
        }

        public void AddNewPost(Post post)
        {
            _context.Posts.Add(post);
            Save();
        }

        public void DeletePost(int postid)
        {
            var postCategories = _context.PostCategories.Where(p => p.PostId == postid).ToList();
            var postLikes      = _context.PostLikes.Where(p => p.PostId == postid).ToList();
            var postTags       = _context.PostTags.Where(p => p.PostId == postid).ToList();
            var postComments   = _context.Comments.Where(p => p.PostId == postid).ToList();
            var postReplies    = _context.Replies.Where(p => p.PostId == postid).ToList();
            var post           = _context.Posts.Find(postid);

            foreach (var pc in postCategories) _context.PostCategories.Remove(pc);
            foreach (var pl in postLikes) _context.PostLikes.Remove(pl);
            foreach (var pt in postTags) _context.PostTags.Remove(pt);
            foreach (var pcom in postComments)
            {
                var commentLikes = _context.CommentLikes.Where(x => x.CommentId == pcom.Id).ToList();
                foreach (var cl in commentLikes) _context.CommentLikes.Remove(cl);
                _context.Comments.Remove(pcom);
            }
            foreach (var pr in postReplies)
            {
                var replyLikes = _context.ReplyLikes.Where(x => x.ReplyId == pr.Id).ToList();
                foreach (var rl in replyLikes) _context.ReplyLikes.Remove(rl);
                _context.Replies.Remove(pr);
            }
            _context.Posts.Remove(post);
            Save();
        }        

        //individual table - we can obtain info about users clicked like\dislike
        public void UpdatePostLike(int postid, string username, string likeordislike)
        {
            //check that one user cant like or dislike more than one time
            var postLike = _context.PostLikes.Where(x => x.Username == username && x.PostId == postid).FirstOrDefault();
            if (postLike != null)
            {
                switch (likeordislike)
                {
                    case "like":
                        if (postLike.Like == false) { postLike.Like = true; postLike.Dislike = false; }
                        else postLike.Like = false;
                        Save();
                        break;
                    case "dislike":
                        if (postLike.Dislike == false) { postLike.Like = false; postLike.Dislike = true; }
                        else postLike.Dislike = false;
                        Save();
                        break;
                }
                if (postLike.Like == false && postLike.Dislike == false) _context.PostLikes.Remove(postLike);
            }
            else
            {
                switch (likeordislike)
                {
                    case "like":
                        postLike = new PostLike() { PostId = postid, Username = username, Like = true, Dislike = false };
                        _context.PostLikes.Add(postLike);
                        Save();
                        break;
                    case "dislike":
                        postLike = new PostLike() { PostId = postid, Username = username, Like = false, Dislike = true };
                        _context.PostLikes.Add(postLike);
                        Save();
                        break;
                }
            }
            var post = _context.Posts.Where(x => x.Id == postid).FirstOrDefault();
            post.LikeCount = LikeDislikeCount("postlike", postid) - LikeDislikeCount("postdislike", postid);
            Save();
        }
        #endregion post

        #region category and tag
        public void AddPostCategories(PostCategory postCategory)
        {
            _context.PostCategories.Add(postCategory);
        }

        public void RemovePostCategories(int postid, int categoryid)
        {
            PostCategory postCategory = _context.PostCategories.Where(x => x.PostId == postid && x.CategoryId == categoryid).FirstOrDefault();
            _context.PostCategories.Remove(postCategory);
            Save();
        }

        public void RemoveCategoryFromPost(int postid, string catName)
        {
            var categoryId = _context.Categories.Where(x => x.Name == catName).Select(x => x.Id).FirstOrDefault();
            var category   = _context.PostCategories.Where(x => x.PostId == postid && x.CategoryId == categoryId).FirstOrDefault();
            _context.PostCategories.Remove(category);
            Save();
        }

        public void AddNewCategory(string catName, string catDesc) //string catUrlSeo, 
        {
            var catSeo = catName.Replace(" ", "-") + "Seo";
            var category = new Category { Name = catName, Description = catDesc, UrlSeo = catSeo, Checked = false }; //Id = num,  UrlSeo = catUrlSeo           
            _context.Categories.Add(category);
            Save();
        }

        public void AddPostTags(PostTag postTag)
        {
            _context.PostTags.Add(postTag);
        }               

        public void RemovePostTags(int postid, int tagid)
        {
            PostTag postTag = _context.PostTags.Where(x => x.PostId == postid && x.TagId == tagid).FirstOrDefault();
            _context.PostTags.Remove(postTag);
            Save();
        }

        public void RemoveTagFromPost(int postid, string tagName)
        {
            var tagid = _context.Tags.Where(x => x.Name == tagName).Select(x => x.Id).FirstOrDefault();
            var tag   = _context.PostTags.Where(x => x.PostId == postid && x.TagId == tagid).FirstOrDefault();
            _context.PostTags.Remove(tag);
            Save();
        }

        public void AddNewTag(string tagName) //, string tagUrlSeo
        {
            var tagSeo = tagName.Replace(" ", "-") + "Seo";
            var tag = new Tag { Name = tagName, UrlSeo = tagSeo, Checked = false }; //Id = num, UrlSeo = tagUrlSeo           
            _context.Tags.Add(tag);
            Save();
        }

        public void RemoveCategory(Category category)
        {
            var postCategories = _context.PostCategories.Where(x => x.CategoryId == category.Id).ToList();
            foreach (var postCat in postCategories)
            {
                _context.PostCategories.Remove(postCat);
            }
            _context.Categories.Remove(category);
            Save();
        }

        public void RemoveTag(Tag tag)
        {
            var postTags = _context.PostTags.Where(x => x.TagId == tag.Id).ToList();
            foreach (var postTag in postTags)
            {
                _context.PostTags.Remove(postTag);
            }
            _context.Tags.Remove(tag);
            Save();
        }       
        #endregion category and tag

        //Comment section
        #region Comments
        public IList<Comment> GetPostComments(int postId)
        {
            return _context.Comments.Where(p => p.PostId == postId).ToList();
        }

        public IList<Reply> GetPostReplies(int postId)
        {
            return _context.Replies.Where(p => p.PostId == postId).ToList();
        }

        public List<CommentViewModel> GetParentReplies(Comment comment)
        {
            var parentReplies = _context.Replies.Where(p => p.CommentId == comment.Id && p.ParentReplyId == null).ToList();
            List<CommentViewModel> parReplies = new List<CommentViewModel>();
            foreach (var par in parentReplies)
            {   
                var chReplies = GetChildReplies(par);
                parReplies.Add(new CommentViewModel() { Body = par.Body, ParentReplyId = par.ParentReplyId, DateTime = par.DateTime, Id = par.Id, UserName = par.UserName, ChildReplies = chReplies });
            }
            return parReplies;
        }

        public List<CommentViewModel> GetChildReplies(Reply parentReply)
        {
            List<CommentViewModel> chldReplies = new List<CommentViewModel>();
            if (parentReply != null)
            {
                var childReplies = _context.Replies.Where(p => p.ParentReplyId == parentReply.Id).ToList();
                foreach (var chReply in childReplies)
                {
                    var chReplies = GetChildReplies(chReply);
                    chldReplies.Add(new CommentViewModel() { Body = chReply.Body, ParentReplyId = chReply.ParentReplyId, DateTime = chReply.DateTime, Id = chReply.Id, UserName = chReply.UserName, ChildReplies = chReplies });
                }
            }
            return chldReplies;
        }

        public Reply GetReplyById(int id)
        {
            return _context.Replies.Where(p => p.Id == id).FirstOrDefault();
        }

        public bool CommentDeleteCheck(int commentid)
        {
            return _context.Comments.Where(x => x.Id == commentid).Select(x => x.Deleted).FirstOrDefault();
        }

        public bool ReplyDeleteCheck(int replyid)
        {
            return _context.Replies.Where(x => x.Id == replyid).Select(x => x.Deleted).FirstOrDefault();
        }
        
        //individual tables for likes - in future we can obtain ifno about users, who turn like\dislike
        public void UpdateCommentLike(int commentid, string username, string likeordislike)
        {
            //one user cant like or dislike more than one time and allow to change his vote
            var commentLike = _context.CommentLikes.Where(x => x.Username == username && x.CommentId == commentid).FirstOrDefault();
            if (commentLike != null)
            {
                switch (likeordislike)
                {
                    case "like":
                        if (commentLike.Like == false) { commentLike.Like = true; commentLike.Dislike = false; }
                        else commentLike.Like = false;
                        Save();
                        break;
                    case "dislike":
                        if (commentLike.Dislike == false) { commentLike.Dislike = true; commentLike.Like = false; }
                        else commentLike.Dislike = false;
                        Save();
                        break;
                }
                if (commentLike.Like == false && commentLike.Dislike == false) _context.CommentLikes.Remove(commentLike);
            }
            else
            {
                switch (likeordislike)
                {
                    case "like":
                        commentLike = new CommentLike() { CommentId = commentid, Username = username, Like = true, Dislike = false };
                        _context.CommentLikes.Add(commentLike);
                        Save();
                        break;
                    case "dislike":
                        commentLike = new CommentLike() { CommentId = commentid, Username = username, Like = false, Dislike = true };
                        _context.CommentLikes.Add(commentLike);
                        Save();
                        break;
                }
            }
            var comment = _context.Comments.Where(x => x.Id == commentid).FirstOrDefault();
            comment.NetLikeCount = LikeDislikeCount("commentlike", commentid) - LikeDislikeCount("commentdislike", commentid);
            Save();
        }

        public void UpdateReplyLike(int replyid, string username, string likeordislike)
        {
            //one user cant like or dislike more than one time and allow to change his vote
            var replyLike = _context.ReplyLikes.Where(x => x.Username == username && x.ReplyId == replyid).FirstOrDefault();
            if (replyLike != null)
            {
                switch (likeordislike)
                {
                    case "like":
                        if (replyLike.Like == false) { replyLike.Like = true; replyLike.Dislike = false; }
                        else replyLike.Like = false;
                        Save();
                        break;
                    case "dislike":
                        if (replyLike.Dislike == false) { replyLike.Dislike = true; replyLike.Like = false; }
                        else replyLike.Dislike = false;
                        Save();
                        break;
                }
                if (replyLike.Like == false && replyLike.Dislike == false) _context.ReplyLikes.Remove(replyLike);
            }
            else
            {
                switch (likeordislike)
                {
                    case "like":
                        replyLike = new ReplyLike() { ReplyId = replyid, Username = username, Like = true, Dislike = false };
                        _context.ReplyLikes.Add(replyLike);
                        Save();
                        break;
                    case "dislike":
                        replyLike = new ReplyLike() { ReplyId = replyid, Username = username, Like = false, Dislike = true };
                        _context.ReplyLikes.Add(replyLike);
                        Save();
                        break;
                }
            }
            var reply = _context.Replies.Where(x => x.Id == replyid).FirstOrDefault();
            reply.NetLikeCount = LikeDislikeCount("replylike", replyid) - LikeDislikeCount("replydislike", replyid);
            Save();
        }

        public Post GetPostByReply(int replyid)
        {
            var postid = _context.Replies.Where(x => x.Id == replyid).Select(x => x.PostId).FirstOrDefault();
            return _context.Posts.Where(x => x.Id == postid).FirstOrDefault();
        }

        public void AddNewComment(Comment comment)
        {
            _context.Comments.Add(comment);
            Save();
        }

        public void AddNewReply(Reply reply)
        {
            _context.Replies.Add(reply);
            Save();
        }

        public Comment GetCommentById(int id)
        {
            return _context.Comments.Where(p => p.Id == id).FirstOrDefault();
        }

        public void DeleteComment(int commentid)
        {
            var comment = _context.Comments.Where(x => x.Id == commentid).FirstOrDefault();
            _context.Comments.Remove(comment);
            Save();
        }

        public void DeleteReply(int replyid)
        {
            var reply = _context.Replies.Where(x => x.Id == replyid).FirstOrDefault();
            _context.Replies.Remove(reply);
            Save();
        }

        //Pretty like additional functions for this :3 although we can implement Delete and delete with rep(children) in one method
        public void DeleteCommentWithReplies(int commentid)
        {
            var comment = _context.Comments.Where(x => x.Id == commentid).FirstOrDefault();
            _context.Comments.Remove(comment);
            //Save();
            var replies = _context.Replies.Where(x => x.CommentId == commentid).ToList();
            if (replies.Count() != 0)
            {
                _context.Replies.RemoveRange(replies);
            }
            Save();
        }

        public void DeleteReplyWithChildren(int replyid)
        {
            var reply = _context.Replies.Where(x => x.Id == replyid).FirstOrDefault();
            _context.Replies.Remove(reply);

            //looking for all children in "root" parentreply           
            var children = _context.Replies.Where(x => x.ParentReplyId == reply.Id).ToList();
            //looking for all children in children in children
            if (children.Count() != 0)
            {
                foreach (var child in children)
                {
                    _context.Replies.Remove(child);
                    //looking for all children, where parentReply rank higher child
                    var moreChildren = _context.Replies.Where(x => x.ParentReplyId == child.Id).ToList();
                    if (moreChildren.Count() != 0)
                    {
                        foreach (var mrCh in moreChildren)
                        {
                            DeleteReplyWithChildren(mrCh.Id); //recusive using our function
                        }
                    }
                }
            }

            //little variant. Varian above looking hard, and don`t if work directly for 100%, but i like it :3
            //var allChildren = _context.Replies.Where(x => x.CommentId == reply.CommentId && x.ParentReplyId >= reply.Id).ToList();
            //_context.Replies.RemoveRange(allChildren);
            Save();
        }
        #endregion comments

        #region helpers
        public void Save()
        {
            _context.SaveChanges();
        }

        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        //Useful functions 
        public string GetUserById(string id)
        {
            return _userContext.Users.FirstOrDefault(p => p.Id == id).UserName;            
        }

        public string GetUserIdByUserName(string userName)
        {
            return _userContext.Users.FirstOrDefault(p => p.UserName == userName).Id;
        }

        public string GetUserMalilByUserName(string userName)
        {
            return _userContext.Users.FirstOrDefault(p => p.UserName == userName).Email;
        }
        #endregion helpers
    }
}