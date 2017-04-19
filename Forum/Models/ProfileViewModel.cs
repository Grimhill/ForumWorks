using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace Forum.Models
{
    public class ProfileViewModel
    {
        [Display(Name = "UserName")]
        public string UserName { get; set; }

        public string UserRole { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "City")]
        public string City { get; set; }

        [Display(Name = "Gender")]
        public string Gender { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "BirthDate (MM/dd/yyyy)")]
        public DateTime? BirthDate { get; set; }

        [Display(Name = "About Yourself")]
        public string YourSelfDescription { get; set; }

        public DateTime Registered { get; set; }
        public DateTime LastLogin { get; set; }

        public bool OnlineStatus { get; set; }

        public IList<CommentWall> CommentWall { get; set; }
       
        public string ProfileUrl { get; set; }
    }

    public class CommentWallViewModel
    {
        public CommentWallViewModel() { }
        public CommentWallViewModel(CommentWall commentWall)
        {
            CommentWall = commentWall;
        }

        public int Id             { get; set; }
        public int? ParentReplyId { get; set; }
        public string Body        { get; set; }
        public string UserName    { get; set; }
        public DateTime DateTime  { get; set; }

        public CommentWall CommentWall { get; set; }
        public IList<CommentWallViewModel> WallChildReplies { get; set; }
    }

    public class CommentWall
    {
        [Key]
        public int Id            { get; set; }
        public string ProfileId  { get; set; } //UserName profile owner
        public string UserName   { get; set; }
        public DateTime DateTime { get; set; }
        [Required]
        public string Body { get; set; }

        public ICollection<CommentWallReply> CommentWallReplies { get; set; }
    }

    public class CommentWallReply
    {
        [Key]
        public int Id             { get; set; }
        public string ProfileId   { get; set; } //UserName profile owner
        public string UserName    { get; set; }
        public DateTime DateTime  { get; set; }
        [Required]
        public string Body        { get; set; }

        public int CommentId      { get; set; }
        public int? ParentReplyId { get; set; }

        public CommentWall CommentWall { get; set; }
    }

    public class PrivateMessages
    {
        [Key]
        public int Id             { get; set; }
        public int UserReseiverId { get; set; }
        public int UserSenderId   { get; set; }
        public string Title       { get; set; }
        public string Body        { get; set; }
        public DateTime DateTime  { get; set; }
        bool state                { get; set; } //executed or not
    }
}