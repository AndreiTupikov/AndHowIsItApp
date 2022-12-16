using AndHowIsItApp.Models;
using Dropbox.Api;
using Dropbox.Api.Files;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace AndHowIsItApp.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        private ApplicationDbContext db = ApplicationDbContext.Create();
        //подкючить токен для сохранения картинок
        private DropboxClient dbx = new DropboxClient("");
        
        public ActionResult Index()
        {
            var tags = db.Tags.Include("Reviews").Where(t => t.Reviews.Count > 0);
            ViewBag.Tags = tags.OrderByDescending(t => t.Reviews.Count).Take(10);
            ViewBag.SubjectGroups = new SelectList(db.SubjectGroups, "Id", "Name");
            return View();
        }

        public ActionResult GetLatestReviews()
        {
            var latestPreviews = GetAllPreviews().OrderByDescending(p => p.Date).Take(5);
            ViewBag.ParagraphName = "Последние обзоры";
            return PartialView("PreviewSet", latestPreviews);
        }

        public ActionResult GetTopReviews()
        {
            var topPreviews = GetAllPreviews().OrderByDescending(p => p.Likes).ThenByDescending(p => p.Date).Take(5);
            ViewBag.ParagraphName = "Лучшие обзоры за все время";
            return PartialView("PreviewSet", topPreviews);
        }

        public ActionResult SearchResults(string tagName, int? group)
        {
            if (tagName != null)
            {
                var tag = db.Tags.First(t => t.Name == tagName);
                var reviews = db.Reviews.Include("Tags").Where(r => r.Tags.Any(t => t.Name == tagName)).Include("ApplicationUser").Include("Subject");
                return View(reviews);
            }
            else
            {
                if (group == null) return RedirectToAction("Index");
                var subjectGroup = db.SubjectGroups.First(s => s.Id == (int)group);
                var reviews = db.Reviews.Include("Subject").Where(r => r.Subject.SubjectGroup.Id == subjectGroup.Id).Include("ApplicationUser");
                return View(reviews);
            }
        }

        public IEnumerable<PreviewModel> GetAllPreviews()
        {
            return db.Reviews.Select(r => new PreviewModel { ReviewId = r.Id, UserId = r.ApplicationUser.Id, OwnerName = r.ApplicationUser.UserName, SubjectId = r.Subject.Id, Subject = r.Subject.Name, Category = r.Subject.SubjectGroup.Name, Title = r.Name, Rating = r.ReviewerRating, Likes = db.UserLikes.Where(l => l.Review.Id == r.Id).Count(), Date = r.LastChangeDate });
        }

        public async Task<ActionResult> ReviewPage(int reviewId)
        {
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").Include("Subject").FirstOrDefault();
            if (review == null) return RedirectToAction("Index");
            var tags = db.Tags.Where(t => t.Reviews.Any(r => r.Id == review.Id));
            var rating = GetSubjectRating(review.Subject.Id);
            ViewBag.SubjectRating = rating == 0 ? "Оценок нет" : rating.ToString();
            ViewBag.ReviewLikes = GetReviewLikes(reviewId);
            ViewBag.Picture = await DownloadPicture(review.PictureLink);
            ViewBag.Tags = tags;
            return View(review);
        }

        [Authorize]
        public ActionResult CreateReview(string userId)
        {
            ReviewCreateViewModel model = new ReviewCreateViewModel();
            model.AllSubjectGroups = new SelectList(db.SubjectGroups, "Id", "Name");
            ViewBag.UserId = userId;
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateReview(ReviewCreateViewModel model, string[] tags, HttpPostedFileBase uploadPicture)
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
                if (tags != null && tags.Length > 0)
                {
                    foreach (var tag in tags)
                    {
                        var tg = tag.Trim();
                        if (tg.Length > 0)
                        {
                            var newTag = db.Tags.FirstOrDefault(t => t.Name == tg);
                            if (newTag == null)
                            {
                                newTag = new Models.Tag { Name = tg };
                                db.Tags.Add(newTag);
                            }
                            review.Tags.Add(newTag);
                        }
                    }
                }
                db.Reviews.Add(review);
                db.SaveChanges();
                if (uploadPicture != null)
                {
                    await UploadPicture(userId, review.Id, uploadPicture);
                    review.PictureLink = "/" + userId + "/" + review.Id + ".jpg";
                    db.SaveChanges();
                }
                return RedirectToAction("PersonalPage", new { userId });
            }
            model.AllSubjectGroups = new SelectList(db.SubjectGroups, "Id", "Name");
            return View(model);
        }

        [Authorize]
        public ActionResult PersonalPage(string userId)
        {
            if (!User.IsInRole("admin") || userId == null ) userId = User.Identity.GetUserId();
            var myReviews = db.Reviews.Where(r => r.ApplicationUser.Id == userId).OrderByDescending(r => r.LastChangeDate).Include("Subject");
            ViewBag.UserId = userId;
            return View(myReviews);
        }

        [Authorize]
        public ActionResult DeleteReview(string userId, int reviewId)
        {
            if (!User.IsInRole("admin") || userId == null) userId = User.Identity.GetUserId();
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").FirstOrDefault();
            if (review != null && review.ApplicationUser.Id == userId)
            {
                db.Reviews.Remove(review);
                db.SaveChanges();
            }
            return RedirectToAction("PersonalPage", new { userId } );
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

        public int GetTotalUserLikes(string userId)
        {
            var userReviews = db.Reviews.Where(r => r.ApplicationUser.Id == userId);
            var userLikes = db.UserLikes.Where(l => userReviews.Any(r => r.Id == l.Review.Id)).Count();
            return userLikes;
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

        private async Task UploadPicture(string folder, int file, HttpPostedFileBase picture)
        {
            byte[] pictureData = null;
            using (var binaryReader = new BinaryReader(picture.InputStream))
            {
                pictureData = binaryReader.ReadBytes(picture.ContentLength);
            }
            using (var mem = new MemoryStream(pictureData))
            {
                var updated = await dbx.Files.UploadAsync(
                    "/ReviewPictures/" + folder + "/" + file + ".jpg",
                    WriteMode.Overwrite.Instance,
                    body: mem);
            }
        }

        private async Task<byte[]> DownloadPicture(string path)
        {
            if (path == null)
            {
                var filler = System.IO.File.ReadAllBytes(Server.MapPath("~/Content/pictureFiller.jpg"));
                return filler;
            }
            using (var response = await dbx.Files.DownloadAsync("/ReviewPictures" + path))
            {
                return await response.GetContentAsByteArrayAsync();
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult AddComment(string commentText, int reviewId = 0)
        {
            if (commentText != null && reviewId != 0)
            {
                var userId = User.Identity.GetUserId();
                var user = db.Users.FirstOrDefault(u => u.Id == userId);
                var review = db.Reviews.FirstOrDefault(r => r.Id == reviewId);
                Comment comment = new Comment { ApplicationUser = user, Review = review, Text = commentText };
                db.Comments.Add(comment);
                review.LastCommentDate = comment.Date;
                db.SaveChanges();
                return RedirectToAction("ReviewPage", new { reviewId });
            }
            return RedirectToAction("Index");
        }

        public ActionResult GetComments(int reviewId = 0)
        {
            if (reviewId != 0)
            {
                if (Request.IsAjaxRequest() && db.Reviews.First(r => r.Id == reviewId).LastCommentDate.AddSeconds(6) < DateTime.Now) return null;
                var comments = db.Comments.Where(c => c.Review.Id == reviewId).OrderBy(c => c.Date).Include("ApplicationUser");
                return PartialView(comments);
            }
            return RedirectToAction("Index");
        }
    }
}