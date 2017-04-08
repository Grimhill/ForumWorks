using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Web.Mvc;

namespace Forum.Models
{
    public class ForumViewModel
    {
        public int       Id               { get; set; }
        public DateTime  PostedOn         { get; set; }
        public DateTime? Modified         { get; set; }
        public int       PostDislikes     { get; set; }
        public int       PostLikes        { get; set; }
        public int       TotalPosts       { get; set; } 
               
        public string    ShortDescription { get; set; }
        public string    Title            { get; set; }        
        public string    Author           { get; set; }
        public string    PostModifedBy    { get; set; }
        public string    UrlSlug          { get; set; }

        public IList<Tag>      Tag            { get; set; }
        public IList<Category> PostCategories { get; set; }
        public IList<Tag>      PostTags       { get; set; }

        public List<string>    Category       { get; set; }
        public Post            Post           { get; set; }
    }

    //Post section
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Title")]
        public string Title { get; set; }

        [Required]
        [AllowHtml]
        [DataType(DataType.MultilineText)]
        [Display(Name = "ShortDescription")]
        public string ShortDescription { get; set; }

        [Required]
        [AllowHtml]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Body")]
        public string Body { get; set; }

        [Required]
        [Display(Name = "Meta")]
        public string Meta { get; set; }

        [Required]
        [Display(Name = "UrlSeo")]
        public string UrlSeo { get; set; }
                
        [DefaultValue(0)]
        public int       LikeCount     { get; set; }
        public bool      isPublished   { get; set; }
        public DateTime  PostedOn      { get; set; }
        public DateTime? Modified      { get; set; }
        public string    Author        { get; set; }
        public string    PostModifedBy { get; set; }

        public ICollection<PostCategory> PostCategories { get; set; }
        public ICollection<PostTag>      PostTags       { get; set; }
        public ICollection<PostLike>     PostLikes      { get; set; }
        public ICollection<Comment>      Comments       { get; set; }
        public ICollection<Reply>        Replies        { get; set; }
    }

    public class PostViewModel
    {       
        public int    Id               { get; set; }
        [Required]
        public string Body             { get; set; }
        [Required]
        public string Title            { get; set; }
        [Required]
        public string Meta             { get; set; }
        [Required]
        public string UrlSeo           { get; set; }
        [Required]
        public string ShortDescription { get; set; }
       
        public int      PostCount    { get; set; }
        public int      PostDislikes { get; set; }
        public int      PostLikes    { get; set; }
        public string   Author       { get; set; }
        public string   PostModifedBy{ get; set; }
        public bool     isPublished  { get; set; }
        public DateTime PostedOn     { get; set; }
        public DateTime? Modified    { get; set; }

        public IList<Tag>      PostTags       { get; set; }
        public IList<Category> PostCategories { get; set; }
        public IList<Category> Categories     { get; set; }
        public IList<Tag>      Tags           { get; set; }
        public IList<Comment>  Comments       { get; set; }
    }

    public class AllPostsViewModel
    {
        public int       PostId        { get; set; }
        public DateTime  Date          { get; set; }
        public string    Description   { get; set; }
        public string    Title         { get; set; }
        public string    Author        { get; set; }
        public bool      Checked       { get; set; }
        public string    UrlSlug       { get; set; }

        public DateTime? Modified      { get; set; }
        public string    PostModifedBy { get; set; }
        public bool      isPublished   { get; set; }

        public IList<Category> PostCategories { get; set; }
        public IList<Tag>      PostTags       { get; set; }

        public Tag      Tag      { get; set; }
        public Category Category { get; set; }
    }

    public class PostLike
    {
        [Key]
        public int    Id       { get; set; }
        public int    PostId   { get; set; }
        public string Username { get; set; }
        public bool   Like     { get; set; }
        public bool   Dislike  { get; set; }

        public Post Post { get; set; }
    }
    
    //Category Section
    public class Category
    {
        [Key]        
        public int   Id           { get; set; }
        [Required]
        [Display(Name = "Name")]
        public string Name        { get; set; }
        [Required]
        [Display(Name = "UrlSeo")]
        public string UrlSeo      { get; set; }
        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; }
        public bool   Checked     { get; set; }

        public ICollection<PostCategory> PostCategories { get; set; }
    }

    public class PostCategory
    {
        [Key]
        [Column(Order = 0)]
        public int      PostId     { get; set; }

        [Key]
        [Column(Order = 1)]
        public int      CategoryId { get; set; }

        public bool     Checked    { get; set; }

        public Post     Post       { get; set; }
        public Category Category   { get; set; }
    }

    //Tag section
    public class Tag
    {
        [Key]
        public int    Id      { get; set; }
        [Required]
        [Display(Name = "Name")]
        public string Name    { get; set; }
        [Required]
        [Display(Name = "UrlSeo")]
        public string UrlSeo  { get; set; }
        public bool   Checked { get; set; }

        public ICollection<PostTag> PostTags { get; set; }
    }

    public class PostTag
    {
        [Key]
        [Column(Order = 0)]
        public int PostId   { get; set; }

        [Key]
        [Column(Order = 1)]
        public int  TagId   { get; set; }

        public bool Checked { get; set; }

        public Post Post    { get; set; }
        public Tag  Tag     { get; set; }
    }    

    //Comments Section
    public class CommentViewModel
    {
        public CommentViewModel() { }
        public CommentViewModel(Comment comment)
        {
            Comment = comment;
        }

        public int     Id            { get; set; }
        public int?    ParentReplyId { get; set; }
        public string  Body          { get; set; }        
        public string  UserName      { get; set; }

        public DateTime  DateTime { get; set; }
        public DateTime? EditTime { get; set; }
        public string    EditedBy { get; set; }

        public Comment   Comment   { get; set; }        
        public IList<CommentViewModel> ChildReplies { get; set; }
    }

    public class Comment
    {
        [Key]
        public int       Id           { get; set; }
        public int       PostId       { get; set; }
        public DateTime  DateTime     { get; set; }
        public DateTime? EditTime     { get; set; }
        public string    UserName     { get; set; }
        public string    EditedBy     { get; set; }
        [Required]
        public string    Body         { get; set; }
        [DefaultValue(0)]
        public int       NetLikeCount { get; set; }
        [DefaultValue(false)]
        public bool      Deleted      { get; set; }

        public Post Post { get; set; }
        public ICollection<Reply> Replies { get; set; }
        public ICollection<CommentLike> CommentLikes { get; set; }
    }

    public class Reply
    {
        [Key]
        public int       Id            { get; set; }
        public int       PostId        { get; set; }
        public int       CommentId     { get; set; }
        public int?      ParentReplyId { get; set; }
        public DateTime  DateTime      { get; set; }
        public DateTime? EditTime      { get; set; }        
        public string    UserName      { get; set; }
        public string    EditedBy      { get; set; }
        [Required]
        public string    Body          { get; set; }
        [DefaultValue(false)]
        public bool      Deleted       { get; set; }

        public Post    Post    { get; set; }
        public Comment Comment { get; set; }
        public ICollection<ReplyLike> ReplyLikes { get; set; }
    }

    public class CommentLike
    {
        [Key]
        public int    Id        { get; set; }
        public int    CommentId { get; set; }
        public string Username  { get; set; }
        public bool   Like      { get; set; }
        public bool   Dislike   { get; set; }

        public Comment Comment  { get; set; }
    }

    public class ReplyLike
    {
        [Key]
        public int    Id         { get; set; }
        public int    ReplyId    { get; set; }
        public string Username   { get; set; }
        public bool   Like       { get; set; }
        public bool   Dislike    { get; set; }

        public Reply  Reply      { get; set; }
    }

    public class imagesviewmodel
    {
        public string   Url  { get; set; }
        public string   Name { get; set; }
        public DateTime Date { get; set; }
    }
}