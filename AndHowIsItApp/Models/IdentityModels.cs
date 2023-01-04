using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AndHowIsItApp.Models
{
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Review> Reviews { get; set; }
        public ICollection<UserRating> UserRatings { get; set; }
        public ICollection<UserLike> UserLikes { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            return userIdentity;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserRating> UserRatings { get; set; }
        public DbSet<UserLike> UserLikes { get; set; }
        public DbSet<Comment> Comments { get; set; }

        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
    public class ApplicationDbInitializer : DropCreateDatabaseAlways<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var role1 = new IdentityRole { Name = "admin" };
            var role2 = new IdentityRole { Name = "adminMaster" };
            roleManager.Create(role1);
            roleManager.Create(role2);
            context.Categories.Add(new Category { Name = "Books" });
            context.Categories.Add(new Category { Name = "Movies" });
            context.Categories.Add(new Category { Name = "Video Games" });
            base.Seed(context);
        }
    }
}