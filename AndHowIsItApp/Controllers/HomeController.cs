using AndHowIsItApp.Models;
using Dropbox.Api;
using Dropbox.Api.Files;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
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

        public ActionResult TagCloud(int? category, string tag)
        {
            var tags = db.Tags.Include("Reviews").Where(t => t.Reviews.Count > 0).Where(t => t.Name != tag);
            ViewBag.Tags = tags.OrderByDescending(t => t.Reviews.Count).Take(10);
            ViewBag.Categories = new SelectList(db.Categories, "Id", "Name", selectedValue: category);
            return PartialView();
        }

        public ActionResult SearchResults(string tagName, int? category)
        {
            if (tagName != null)
            {
                var tag = db.Tags.Include("Reviews").First(t => t.Name == tagName);
                var previews = GetAllPreviews().Where(p => tag.Reviews.Any(r => r.Id == p.ReviewId)).OrderByDescending(p => p.Date);
                ViewBag.SelectedTag = tagName;
                ViewBag.ResultsBy = tagName;
                return View(previews);
            }
            else
            {
                var subjectCategory = db.Categories.FirstOrDefault(s => s.Id == category);
                var previews = GetAllPreviews().OrderByDescending(p => p.Date);
                ViewBag.ResultsBy = "Все категории";
                if (category != null)
                {
                    previews = previews.Where(p => p.Category == subjectCategory.Name).OrderByDescending(p => p.Date);
                    ViewBag.ResultsBy = subjectCategory.Name;
                }
                ViewBag.SelectedCategory = category;
                return View(previews);
            }
        }

        public IEnumerable<PreviewModel> GetAllPreviews()
        {
            return db.Reviews.Select(r => new PreviewModel {
                ReviewId = r.Id,
                UserId = r.ApplicationUser.Id,
                OwnerName = r.ApplicationUser.UserName,
                SubjectId = r.Subject.Id,
                Subject = r.Subject.Name,
                Category = r.Subject.Category.Name,
                Title = r.Name, Rating = r.ReviewerRating,
                Likes = db.UserLikes.Where(l => l.Review.Id == r.Id).Count(),
                Date = r.CreateDate
            });
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
            ReviewCreateViewModel model = new ReviewCreateViewModel
            {
                AllCategories = new SelectList(db.Categories, "Id", "Name"),
                UserId = userId
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult> CreateReview(ReviewCreateViewModel model, string[] tags, HttpPostedFileBase uploadPicture)
        {
            if (ModelState.IsValid)
            {
                if (model.UserId == null || !User.IsInRole("admin")) model.UserId = User.Identity.GetUserId();
                Subject subject = db.Subjects.Where(s => s.Category.Id == model.Category).FirstOrDefault(s => s.Name == model.Subject);
                if (subject == null)
                {
                    subject = new Subject { Name = model.Subject, Category = db.Categories.FirstOrDefault(s => s.Id == model.Category) };
                    db.Subjects.Add(subject);
                }
                Review review = new Review { ApplicationUser = db.Users.FirstOrDefault(u => u.Id == model.UserId), Name = model.Title, Subject = subject, Text = model.Text, ReviewerRating = model.ReviewerRating };
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
                    await UploadPicture(model.UserId, review.Id, uploadPicture);
                    review.PictureLink = "/" + model.UserId + "/" + review.Id + ".jpg";
                    db.SaveChanges();
                }
                if (!User.IsInRole("admin")) return RedirectToAction("PersonalPage");
                return RedirectToAction("PersonalPage", new { model.UserId });
            }
            model.AllCategories = new SelectList(db.Categories, "Id", "Name");
            return View(model);
        }

        [Authorize]
        public ActionResult EditReview(string userId, int reviewId)
        {
            var review = db.Reviews.Include("ApplicationUser").FirstOrDefault(r => r.Id == reviewId);
            if (userId == null || !User.IsInRole("admin")) userId = User.Identity.GetUserId();
            if (review != null && review.ApplicationUser.Id == userId)
            {
                var model = new ReviewEditViewModel
                {
                    ReviewId = review.Id,
                    UserId = userId,
                    Title = review.Name,
                    Text = review.Text,
                    ReviewerRating = review.ReviewerRating
                };
                ViewBag.Tags = db.Tags.Where(t => t.Reviews.Any(r => r.Id == review.Id)).Select(t => t.Name);
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditReview(ReviewEditViewModel model, string[] tags)
        {
            if (ModelState.IsValid)
            {
                if (model.UserId == null || !User.IsInRole("admin")) model.UserId = User.Identity.GetUserId();
                var review = db.Reviews.Include("ApplicationUser").Include("Tags").FirstOrDefault(r => r.Id == model.ReviewId);
                if (review == null || review.ApplicationUser.Id != model.UserId) return RedirectToAction("Index");
                review.Name = model.Title; review.Text = model.Text; review.ReviewerRating = model.ReviewerRating; review.LastChangeDate = DateTime.Now;
                var oldTags = db.Tags.Where(t => t.Reviews.Any(r => r.Id == review.Id));
                foreach (var tag in oldTags)
                {
                    if (!tags.Any(t => t.Trim() == tag.Name)) review.Tags.Remove(tag);
                }
                foreach (var tag in tags)
                {
                    var tg = tag.Trim();
                    if (tg.Length > 0 && !oldTags.Any(o => o.Name == tg))
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
                db.SaveChanges();
                if (!User.IsInRole("admin")) return RedirectToAction("PersonalPage");
                return RedirectToAction("PersonalPage", new { model.UserId });
            }
            return View(model);
        }

        [Authorize]
        public ActionResult PersonalPage(string userId)
        {
            if (userId == null || !User.IsInRole("admin")) userId = User.Identity.GetUserId();
            var userName = db.Users.FirstOrDefault(u => u.Id == userId).UserName;
            var previews = GetAllPreviews().Where(p => p.OwnerName == userName).OrderByDescending(p => p.Date);
            if (User.IsInRole("admin")) ViewBag.UserId = userId;
            ViewBag.UserName = db.Users.Single(u => u.Id == userId).UserName;
            ViewBag.UserLikes = GetTotalUserLikes(userId);
            return View(previews);
        }

        [Authorize]
        public ActionResult DeleteReview(string userId, int reviewId)
        {
            if (userId == null || !User.IsInRole("admin")) userId = User.Identity.GetUserId();
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").FirstOrDefault();
            if (review != null && review.ApplicationUser.Id == userId)
            {
                db.Reviews.Remove(review);
                db.SaveChanges();
            }
            if (!User.IsInRole("admin")) return RedirectToAction("PersonalPage");
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
        public JsonResult GetSubjects(string group, string prefix)
        {
            int groupId = int.Parse(group);
            var subjects = db.Subjects.Where(s => s.Category.Id == groupId).Where(s => s.Name.Contains(prefix)).Take(5).ToList();
            return Json(subjects, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult GetTags(string prefix)
        {
            var tags = db.Tags.Where(s => s.Name.Contains(prefix)).Take(5).ToList();
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