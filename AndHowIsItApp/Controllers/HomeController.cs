using AndHowIsItApp.Models;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace AndHowIsItApp.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationDbContext db = ApplicationDbContext.Create();
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetLatestReviews()
        {
            var latestReviews = db.Reviews.OrderByDescending(r => r.LastChangeDate).Take(5).Include("ApplicationUser").Include("Subject");
            ViewBag.ParagraphName = "Последние обзоры";
            return View("GetReviewsSet", latestReviews);
        }

        public ActionResult GetTopReviews()
        {
            var topReviews = db.Reviews.OrderByDescending(r => r.ReviewerRating).Take(5).Include("ApplicationUser").Include("Subject");
            ViewBag.ParagraphName = "Лучшие обзоры за все время";
            return View("GetReviewsSet", topReviews);
        }

        public ActionResult ReviewPage(int reviewId)
        {
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").Include("Subject").FirstOrDefault();
            if (review == null) return RedirectToAction("Index");
            var tags = db.Tags.Where(t => t.Reviews.Any(r => r.Id == review.Id));
            var rating = GetSubjectRating(review.Subject.Id);
            ViewBag.SubjectRating = rating == 0 ? "Оценок нет" : rating.ToString();
            ViewBag.ReviewLikes = GetReviewLikes(reviewId);
            ViewBag.Tags = tags;
            return View(review);
        }

        [Authorize]
        public ActionResult CreateReview(string userId)
        {
            ReviewCreateViewModel model = new ReviewCreateViewModel();
            model.AllTags = new SelectList(db.Tags, "Name", "Id");
            model.AllSubjectGroups = new SelectList(db.SubjectGroups, "Id", "Name");
            ViewBag.UserId = userId;
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult CreateReview(ReviewCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = model.UserId;
                if (!User.IsInRole("admin") || userId == null)
                {
                    userId = User.Identity.GetUserId();
                }
                Subject subject = db.Subjects.Where(s => s.SubjectGroup.Id == model.SubjectGroup).FirstOrDefault(s => s.Name == model.Subject);
                if (subject == null)
                {
                    subject = new Subject { Name = model.Subject, SubjectGroup = db.SubjectGroups.FirstOrDefault(s => s.Id == model.SubjectGroup) };
                    db.Subjects.Add(subject);
                }
                Review review = new Review { ApplicationUser = db.Users.FirstOrDefault(u => u.Id == userId), Name = model.Name, Subject = subject, Text = model.Text, ReviewerRating = model.ReviewerRating };
                if (model.Tags != null && model.Tags.Length > 0)
                {
                    var tags = model.Tags.Split('|');
                    foreach ( var tag in tags)
                    {
                        var tg = tag.Trim();
                        if (tg.Length > 0)
                        {
                            var newTag = db.Tags.FirstOrDefault(t => t.Name == tg);
                            if (newTag == null){
                                newTag = new Tag { Name = tg };
                                db.Tags.Add(newTag);
                            }
                            review.Tags.Add(newTag);
                        }
                    }
                }
                db.Reviews.Add(review);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            model.AllSubjectGroups = new SelectList(db.SubjectGroups, "Id", "Name");
            return View(model);
        }

        [Authorize]
        public ActionResult PersonalPage(string userId)
        {
            if (!User.IsInRole("admin") || userId == null )
            {
                userId = User.Identity.GetUserId();
            }
            var myReviews = db.Reviews.Where(r => r.ApplicationUser.Id == userId).Include("Subject");
            ViewBag.UserId = userId;
            return View(myReviews);
        }

        [Authorize]
        public ActionResult DeleteReview(string userId, int reviewId)
        {
            if (!User.IsInRole("admin") || userId == null)
            {
                userId = User.Identity.GetUserId();
            }
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").FirstOrDefault();
            if (review != null && review.ApplicationUser.Id == userId)
            {
                db.Reviews.Remove(review);
                db.SaveChanges();
            }
            return RedirectToAction("PersonalPage", "Home", new { userId } );
        }

        [Authorize]
        public double RateSubject(int subjectId, int rating)
        {
            var userId = User.Identity.GetUserId();
            var subjectRating = db.UserRatings.Where(u => u.ApplicationUser.Id == userId).FirstOrDefault(s => s.Subject.Id == subjectId);
            if (subjectRating != null) subjectRating.Rating = rating;
            else
            {
                var user = db.Users.Single(u => u.Id == userId);
                var subject = db.Subjects.Single(s => s.Id == subjectId);
                db.UserRatings.Add(new UserRating { ApplicationUser = user, Subject = subject, Rating = rating });
            }
            db.SaveChanges();
            return GetSubjectRating(subjectId);
        }

        public double GetSubjectRating(int subjectId)
        {
            var ratings = db.UserRatings.Where(r => r.Subject.Id == subjectId);
            if (ratings.Count() < 1) return 0;
            double allRatings = 0.0;
            foreach (var rating in ratings) allRatings += rating.Rating;
            double result = (double)(allRatings / ratings.Count()) * 10;
            return Math.Round(result) / 10;
        }

        [Authorize]
        public int GetUserRating(int subjectId)
        {
            var userId = User.Identity.GetUserId();
            var rating = db.UserRatings.Where(r => r.Subject.Id == subjectId).FirstOrDefault(r => r.ApplicationUser.Id == userId);
            if (rating == null) return 0;
            return rating.Rating;
        }

        [Authorize]
        public int LikeReview(int reviewId)
        {
            var userId = User.Identity.GetUserId();
            var like = db.UserLikes.Where(l => l.Review.Id == reviewId).FirstOrDefault(l => l.ApplicationUser.Id == userId);
            if (like == null)
            {
                var user = db.Users.FirstOrDefault(u => u.Id == userId);
                var review = db.Reviews.FirstOrDefault(r => r.Id == reviewId);
                db.UserLikes.Add(new UserLike { ApplicationUser = user, Review = review });
            } else db.UserLikes.Remove(like);
            db.SaveChanges();
            return GetReviewLikes(reviewId);
        }

        public int GetReviewLikes(int reviewId)
        {
            var likes = db.UserLikes.Where(l => l.Review.Id == reviewId).Count();
            return likes;
        }

        [Authorize]
        public bool GetUserLike(int reviewId)
        {
            var userId = User.Identity.GetUserId();
            return db.UserLikes.Where(l => l.Review.Id == reviewId).Any(l => l.ApplicationUser.Id == userId);
        }

        [HttpPost]
        public JsonResult GetSubjects(string group, string Prefix)
        {
            int groupId = int.Parse(group);
            var subjects = db.Subjects.Where(s => s.SubjectGroup.Id == groupId).Where(s => s.Name.Contains(Prefix)).Take(5).ToList();
            return Json(subjects, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetTags(string Prefix)
        {
            var tags = db.Tags.Where(s => s.Name.Contains(Prefix)).Take(5).ToList();
            return Json(tags, JsonRequestBehavior.AllowGet);
        }

        [Authorize(Roles = "admin")]
        public ActionResult ManageUsers()
        {
            var users = db.Users;
            ViewBag.Users = users;
            return View();
        }
    }
}