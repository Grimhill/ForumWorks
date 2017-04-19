using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Forum.Models;

namespace Forum.Functionality
{
    public class ProfileFunctions: IProfileFunctions
    {
        private ForumDbContext _context;
        private ApplicationDbContext _userContext;
        public ProfileFunctions(ForumDbContext context, ApplicationDbContext userContext)
        {
            _context     = context;
            _userContext = userContext;
        }

        #region commentWall
        public void AddNewWallComment(CommentWall commentWall)
        {
            _context.CommenstWall.Add(commentWall);
            Save();
        }

        public void AddNewWallReply(CommentWallReply commentWallReply)
        {
            _context.CommentWallReplies.Add(commentWallReply);
            Save();
        }

        public IList<CommentWall> GetProfileComments(string profileId)
        {
            return _context.CommenstWall.Where(p => p.ProfileId == profileId).ToList();
        }

        public IList<CommentWallReply> GetProfileReplies(string profileId)
        {
            return _context.CommentWallReplies.Where(p => p.ProfileId == profileId).ToList();
        }

        public List<CommentWallViewModel> GetProfileParentReplies(CommentWall commentWall)
        {
            var parentReplies = _context.CommentWallReplies.Where(p => p.CommentId == commentWall.Id && p.ParentReplyId == null).ToList();
            List<CommentWallViewModel> parReplies = new List<CommentWallViewModel>();
            foreach (var par in parentReplies)
            {
                var chReplies = GetProfileChildReplies(par);
                parReplies.Add(new CommentWallViewModel() { Body = par.Body, ParentReplyId = par.ParentReplyId, DateTime = par.DateTime, Id = par.Id, UserName = par.UserName, WallChildReplies = chReplies });
            }
            return parReplies;
        }

        public List<CommentWallViewModel> GetProfileChildReplies(CommentWallReply parentReply)
        {
            List<CommentWallViewModel> chldReplies = new List<CommentWallViewModel>();
            if (parentReply != null)
            {
                var childReplies = _context.CommentWallReplies.Where(p => p.ParentReplyId == parentReply.Id).ToList();
                foreach (var chReply in childReplies)
                {
                    var chReplies = GetProfileChildReplies(chReply);
                    chldReplies.Add(new CommentWallViewModel() { Body = chReply.Body, ParentReplyId = chReply.ParentReplyId, DateTime = chReply.DateTime, Id = chReply.Id, UserName = chReply.UserName, WallChildReplies = chReplies });
                }
            }
            return chldReplies;
        }

        public CommentWallReply GetProfileReplyById(int Id)
        {
            return _context.CommentWallReplies.Where(p => p.Id == Id).FirstOrDefault();
        }
        #endregion

        #region Helpers
        public void Save()
        {
            _context.SaveChanges();
        }

        public IList<ApplicationUser> GetUsers()
        {
            return _userContext.Users.ToList();
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
        #endregion
    }
}