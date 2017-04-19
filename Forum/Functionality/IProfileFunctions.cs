using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Forum.Models;

namespace Forum.Functionality
{
    public interface IProfileFunctions : IDisposable
    {
        #region CommentWall
        void AddNewWallComment(CommentWall commentWall);
        void AddNewWallReply(CommentWallReply commentWallReply);
        IList<CommentWall> GetProfileComments(string profileId);
        IList<CommentWallReply> GetProfileReplies(string profileId);
        List<CommentWallViewModel> GetProfileParentReplies(CommentWall commentWall);
        List<CommentWallViewModel> GetProfileChildReplies(CommentWallReply parentReply);
        CommentWallReply GetProfileReplyById(int id);
        #endregion

        #region Helpers
        void Save();
        string GetUserById(string id);
        string GetUserIdByUserName(string userName);
        string GetUserMalilByUserName(string userName);
        #endregion
    }
}