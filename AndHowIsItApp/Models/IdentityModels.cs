using System.Collections.Generic;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace AndHowIsItApp.Models
{
    // В профиль пользователя можно добавить дополнительные данные, если указать больше свойств для класса ApplicationUser. Подробности см. на странице https://go.microsoft.com/fwlink/?LinkID=317594.
    public class ApplicationUser : IdentityUser
    {
        public ICollection<Review> Reviews { get; set; }
        public ICollection<UserRating> UserRatings { get; set; }
        public ICollection<UserLike> UserLikes { get; set; }

        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Обратите внимание, что authenticationType должен совпадать с типом, определенным в CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Здесь добавьте утверждения пользователя
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
            var userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(context));
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            var role1 = new IdentityRole { Name = "admin" };
            var role2 = new IdentityRole { Name = "adminMaster" };
            var role3 = new IdentityRole { Name = "user" };
            roleManager.Create(role1);
            roleManager.Create(role2);
            roleManager.Create(role3);
            var admin = new ApplicationUser { Email = "admin.2ahii@gmail.com", UserName = "admin" };
            string password = "Qwerty1!";
            var result = userManager.Create(admin, password);
            if (result.Succeeded)
            {
                userManager.AddToRole(admin.Id, role1.Name);
                userManager.AddToRole(admin.Id, role2.Name);
                userManager.AddToRole(admin.Id, role3.Name);
            }
            context.Categories.Add(new Category { Name = "Книги" });
            context.Categories.Add(new Category { Name = "Фильмы" });
            context.Categories.Add(new Category { Name = "Игры" });
            base.Seed(context);
        }
    }
}