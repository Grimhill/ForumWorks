using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Forum.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public string    FirstName           { get; internal set; }
        public string    LastName            { get; internal set; }        
        public string    Country             { get; internal set; }
        public string    City                { get; internal set; }
        public string    Gender              { get; internal set; }
        public DateTime? BirthDate           { get; internal set; }
        public string    YourSelfDescription { get; internal set; }

        public DateTime JoinDate            { get; internal set; }
        public DateTime LastLoginDate       { get; internal set; }
        public bool     OnlineStatus        { get; set; }
        //public DateTime EmailLinkDate       { get; internal set; }


        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }

    public class ForumDbContext : DbContext
    {
        public ForumDbContext() : base("DefaultConnection")
        {
        }

        public static ForumDbContext Create()
        {
            return new ForumDbContext();
        }
        public DbSet<Post>         Posts          { get; set; }
        public DbSet<Category>     Categories     { get; set; }
        public DbSet<Tag>          Tags           { get; set; }
        public DbSet<PostTag>      PostTags       { get; set; }
        public DbSet<PostCategory> PostCategories { get; set; }
        public DbSet<PostLike>     PostLikes      { get; set; }
        public DbSet<Comment>      Comments       { get; set; }
        public DbSet<Reply>        Replies        { get; set; }       
        public DbSet<CommentLike>  CommentLikes   { get; set; }
        public DbSet<ReplyLike>    ReplyLikes     { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            #region Mapping
            /*
             * base.OnModelCreating(modelBuilder);

            //Post
            //one-many post - postcategories
            modelBuilder.Entity<Post>()
               .HasMany(s => s.PostCategories)
               .WithRequired(s => s.Post)
               .HasForeignKey(s => s.PostId);

            //one-many post - posttags
            modelBuilder.Entity<Post>()
               .HasMany(s => s.PostTags)
               .WithRequired(s => s.Post)
               .HasForeignKey(s => s.PostId);            

            //one-many post - postcomments
            modelBuilder.Entity<Post>()
               .HasMany(s => s.Comments)
               .WithRequired(s => s.Post)
               .HasForeignKey(s => s.PostId);

            //one-many post - postcommentReplies
            modelBuilder.Entity<Post>()
               .HasMany(s => s.Replies)
               .WithRequired(s => s.Post)
               .HasForeignKey(s => s.PostId);

            //one-many post - postLikes
            modelBuilder.Entity<Post>()
               .HasMany(s => s.PostLikes)
               .WithRequired(s => s.Post)
               .HasForeignKey(s => s.PostId);

            //Category
            //one-many categories - postcategories
            modelBuilder.Entity<Category>()
               .HasMany(s => s.PostCategories)
               .WithRequired(s => s.Category)
               .HasForeignKey(s => s.CategoryId);

            //Tag
            //one-many tags - posttags
            modelBuilder.Entity<Tag>()
               .HasMany(s => s.PostTags)
               .WithRequired(s => s.Tag)
               .HasForeignKey(s => s.TagId);

            //Commnets
            //one-many comment - comment replies
            modelBuilder.Entity<Comment>()
               .HasMany(s => s.Replies)
               .WithRequired(s => s.Comment)
               .HasForeignKey(s => s.CommentId);

            //one-many comment - comment likes
            modelBuilder.Entity<Comment>()
               .HasMany(s => s.CommentLikes)
               .WithRequired(s => s.Comment)
               .HasForeignKey(s => s.CommentId);

            //one-many comment reply - comment reply likes
            modelBuilder.Entity<Reply>()
               .HasMany(s => s.ReplyLikes)
               .WithRequired(s => s.Reply)
               .HasForeignKey(s => s.ReplyId);
               */
            #endregion
        }
    }
}