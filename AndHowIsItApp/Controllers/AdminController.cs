using AndHowIsItApp.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Linq;
using System.Web.Mvc;

namespace AndHowIsItApp.Controllers
{
    [Authorize(Roles = "admin")]
    public class AdminController : Controller
    {
        private static ApplicationDbContext db = ApplicationDbContext.Create();
        private ApplicationUserManager userManager = new ApplicationUserManager(new UserStore<ApplicationUser>(db));

        public ActionResult ManageUsers()
        {
            var adminRole = db.Roles.Single(r => r.Name == "admin");
            var users = db.Users.Select(u => new UserAdministrationModel { Id = u.Id, UserName = u.UserName, Email = u.Email, IsBlocked = u.LockoutEndDateUtc != null && u.LockoutEndDateUtc > DateTime.Now, IsAdmin = u.Roles.Any(r => r.RoleId == adminRole.Id )} );
            return View(users);
        }

        [HttpPost]
        public ActionResult ManageUsers(UserAdministrationModel user, string option)
        {
            if (user == null || user.Id == User.Identity.GetUserId()) return RedirectToAction("ManageUsers", "Admin");
            if (user.IsAdmin && !User.IsInRole("adminMaster")) return RedirectToAction("ManageUsers", "Admin");
            var targetUser = db.Users.FirstOrDefault(u => u.Id == user.Id);
            switch (option)
            {
                case "block":
                    if (user.IsBlocked) UnblockUser(targetUser);
                    else BlockUser(targetUser);
                    break;
                case "delete":
                    DeleteUser(targetUser);
                    break;
                case "toadmin":
                    if (User.IsInRole("adminMaster"))
                    {
                        if (user.IsAdmin) UnmakeAdmin(targetUser);
                        else MakeAdmin(targetUser);
                    }
                    break;
            }
            return RedirectToAction("ManageUsers", "Admin");
        }

        private void BlockUser(ApplicationUser user)
        {
            user.LockoutEndDateUtc = DateTime.Now.AddYears(100);
            userManager.UpdateSecurityStamp(user.Id);
            db.SaveChanges();
        }

        private void UnblockUser(ApplicationUser user)
        {
            user.LockoutEndDateUtc = null;
            db.SaveChanges();
        }

        private void DeleteUser(ApplicationUser user)
        {
            var reviews = db.Reviews.Where(r => r.ApplicationUser.Id == user.Id);
            db.Reviews.RemoveRange(reviews);
            db.Users.Remove(user);
            db.SaveChanges();
        }

        private void MakeAdmin(ApplicationUser user)
        {
            userManager.AddToRole(user.Id, "admin");
            db.SaveChanges();
        }

        private void UnmakeAdmin(ApplicationUser user)
        {
            userManager.RemoveFromRole(user.Id, "admin");
            db.SaveChanges();
        }
    }
}