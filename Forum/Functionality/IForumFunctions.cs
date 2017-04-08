using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;

using Forum.Models;


namespace Forum.Functionality
{
    public interface IForumFunctions : IDisposable
    {
        #region lists
        IList<Post>     GetPosts();
        IList<Category> GetPostCategories(int postId);
        IList<Tag>      GetPostTags(int postId);     
        int             LikeDislikeCount(string typeAndChoice, int id);
        IList<Tag>      GetTags();
        IList<Category> GetCategories();
        #endregion

        #region 2
        Post GetPostById(int postid);
        int  GetPostIdBySlug(string slug);
        void AddNewPost(Post post);
        void DeletePost(int postid);
        void UpdatePostLike(int postid, string username, string likeordislike);

        void AddPostCategories(PostCategory postCategory);
        void RemovePostCategories(int postid, int categoryid);
        void RemoveCategoryFromPost(int postid, string catName);
        void AddNewCategory(string catName, string catDesc);
        void RemoveCategory(Category category);

        void AddPostTags(PostTag postTag);
        void RemoveTagFromPost(int postid, string tagName);        
        void RemovePostTags(int postid, int tagid);
        void AddNewTag(string tagName);
        void RemoveTag(Tag tag);
        #endregion

        #region comments
        IList<Comment> GetPostComments(Post post);
        IList<Reply> GetPostReplies(Post post);
        List<CommentViewModel> GetParentReplies(Comment comment);
        List<CommentViewModel> GetChildReplies(Reply parentReply);
        Reply GetReplyById(int id);
        bool CommentDeleteCheck(int commentid);
        bool ReplyDeleteCheck(int replyid);

        void UpdateCommentLike(int commentid, string username, string likeordislike);
        void UpdateReplyLike(int replyid, string username, string likeordislike);
        Post GetPostByReply(int replyid);
  
        void AddNewComment(Comment comment);
        void AddNewReply(Reply reply);
        Comment GetCommentById(int id);
        void DeleteComment(int commentid);
        void DeleteReply(int replyid);
        void DeleteCommentWithReplies(int commentid);
        void DeleteReplyWithChilds(int replyid);
        #endregion

        #region some additional helpers
        string GetUserById(string id);
        string GetUserIdByUserName(string userName);
        string GetUserMalilByUserName(string userName);
  
        void Save();
        #endregion
    }
}