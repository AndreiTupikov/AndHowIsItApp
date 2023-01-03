using AndHowIsItApp.Models;
using Dropbox.Api;
using Dropbox.Api.Files;
using ImageResizer;
using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
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
        private DropboxClient dbx = new DropboxClient("sl.BWJSKT5lWk-hW79ZXaJE06Cjz7IVkFu_rtyn3oRqA-rDbg9sGF8yotuM1tSf7ffPwgZ6dOez2gvkkkK3tLWD-T69mbn1xUtUZFGA0qtu715yLYDwYcmdWy-znyfgbp1mpmegy08");
        
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult GetLatestPreviews()
        {
            var latestReviews = db.Reviews.OrderByDescending(r => r.CreateDate).Take(5).Include("ApplicationUser").Include("Subject.Category").ToList();
            ViewBag.ParagraphName = "Последние обзоры";
            return PartialView("PreviewSet", GetPreviewSet(latestReviews));
        }

        public ActionResult GetTopPreviews()
        {
            var reviews = db.Reviews.Include("ApplicationUser").Include("Subject.Category").ToList();
            var topPreviews = GetPreviewSet(reviews).OrderByDescending(p => p.Likes).ThenByDescending(p => p.Date).Take(5);
            ViewBag.ParagraphName = "Лучшие обзоры за все время";
            return PartialView("PreviewSet", topPreviews);
        }

        public ActionResult TagCloud(int? category, string tag)
        {
            var tags = db.Tags.Include("Reviews").Where(t => t.Reviews.Count > 0).Where(t => t.Name != tag);
            ViewBag.Tags = tags.OrderByDescending(t => t.Reviews.Count).Take(10);
            ViewBag.Categories = new SelectList(db.Categories.ToList().Select(c => new { c.Id, Name = GetLocalizedCategory(c.Name) } ), "Id", "Name", selectedValue: category);
            return PartialView();
        }

        public ActionResult SearchResults(string searchString, string tagName, int? category)
        {
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var reviewIds = FullTextSearch(searchString);
                var reviews = db.Reviews.Where(r => reviewIds.Any(id => id == r.Id)).OrderByDescending(r => r.CreateDate).Include("ApplicationUser").Include("Subject.Category").ToList();
                ViewBag.ResultsBy = searchString;
                return View(GetPreviewSet(reviews));
            }
            if (tagName != null)
            {
                var tagReviewsIds = db.Tags.Include("Reviews").First(t => t.Name == tagName).Reviews.Select(r => r.Id).ToList();
                var reviews = db.Reviews.Where(r => tagReviewsIds.Any(tr => tr == r.Id)).OrderByDescending(r => r.CreateDate).Include("ApplicationUser").Include("Subject.Category").ToList();
                ViewBag.SelectedTag = tagName;
                ViewBag.ResultsBy = tagName;
                return View(GetPreviewSet(reviews));
            }
            else
            {
                var subjectCategory = db.Categories.FirstOrDefault(s => s.Id == category);
                var reviews = db.Reviews.Include("ApplicationUser").Include("Subject.Category");
                ViewBag.ResultsBy = Resources.Language.AllCategories;
                if (category != null)
                {
                    var categoryReviews = reviews.Where(r => r.Subject.Category.Id == subjectCategory.Id).ToList();
                    ViewBag.ResultsBy = GetLocalizedCategory(subjectCategory.Name);
                    ViewBag.SelectedCategory = category;
                    return View(GetPreviewSet(categoryReviews).OrderByDescending(p => p.Date));
                }
                return View(GetPreviewSet(reviews.ToList()).OrderByDescending(p => p.Date));
            }
        }

        private List<int> FullTextSearch(string searchString)
        {
            SqlParameter searchText = new SqlParameter("@text", searchString);
            SqlParameter searchLang = new SqlParameter("@language", GetSearchLanguage(searchString));
            var reviewIds = db.Database.SqlQuery<int>("FreeTextSearch @text, @language", searchText, searchLang).ToList();
            return reviewIds;
        }

        public ActionResult GetPreviewsBySubject(int? subject, int? currentReview)
        {
            var subjectReviews = db.Reviews.Where(r => r.Subject.Id == subject).Where(r => r.Id != currentReview).Include("ApplicationUser").Include("Subject.Category").ToList();
            ViewBag.ParagraphName = "Другие обзоры на это произведение";
            return PartialView("PreviewSet", GetPreviewSet(subjectReviews));
        }

        private IEnumerable<PreviewModel> GetPreviewSet(List<Review> reviews)
        {
            var previews = reviews.Select(r => new PreviewModel
            {
                ReviewId = r.Id,
                Author = new UserViewModel { UserName = r.ApplicationUser.UserName, Likes = GetTotalUserLikes(r.ApplicationUser.Id) },
                Subject = new SubjectViewModel { Id = r.Subject.Id, Category = GetLocalizedCategory(r.Subject.Category.Name), Name = r.Subject.Name, Rating = GetSubjectRating(r.Subject.Id) },
                PictureLink = r.PictureLink,
                Title = r.Name,
                Rating = r.ReviewerRating,
                Likes = GetReviewLikes(r.Id),
                Date = r.CreateDate
            });
            return previews;
        }

        public ActionResult ReviewPage(int? reviewId)
        {
            var review = db.Reviews.Where(r => r.Id == reviewId).Include("ApplicationUser").Include("Subject.Category").FirstOrDefault();
            if (review == null) return RedirectToAction("Index");
            var showReview = new ReviewShowViewModel
            {
                Id = review.Id,
                AuthorId = review.ApplicationUser.Id,
                Author = new UserViewModel { UserName = review.ApplicationUser.UserName, Likes = GetTotalUserLikes(review.ApplicationUser.Id) },
                Subject = new SubjectViewModel { Id = review.Subject.Id, Category = GetLocalizedCategory(review.Subject.Category.Name), Name = review.Subject.Name, Rating = GetSubjectRating(review.Subject.Id) },
                PictureLink = review.PictureLink,
                Title = review.Name,
                Likes = GetReviewLikes(review.Id),
                Text = review.Text,
                Rating = review.ReviewerRating,
                CreateDate = review.CreateDate,
                LastChangeDate = review.LastChangeDate,
                Tags = GetReviewTags(review.Id)
            };
            return View(showReview);
        }

        [Authorize]
        public ActionResult CreateReview(string userId)
        {
            ReviewCreateViewModel model = new ReviewCreateViewModel
            {
                AllCategories = new SelectList(db.Categories.ToList().Select(c => new { c.Id, Name = GetLocalizedCategory(c.Name) }), "Id", "Name"),
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
                    review.PictureLink = $"/{model.UserId}/{review.Id}";
                    db.SaveChanges();
                }
                if (!User.IsInRole("admin")) return RedirectToAction("PersonalPage");
                return RedirectToAction("PersonalPage", new { model.UserId });
            }
            model.AllCategories = new SelectList(db.Categories.ToList().Select(c => new { c.Id, Name = GetLocalizedCategory(c.Name) }), "Id", "Name");
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
                    ReviewerRating = review.ReviewerRating,
                    Tags = GetReviewTags(review.Id)
                };
                return View(model);
            }
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpPost]
        public ActionResult EditReview(ReviewEditViewModel model)
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
                    if (!model.Tags.Any(t => t.Trim() == tag.Name)) review.Tags.Remove(tag);
                }
                foreach (var tag in model.Tags)
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
            var reviews = db.Reviews.Include("ApplicationUser").Where(r => r.ApplicationUser.Id == userId).Include("Subject.Category").OrderByDescending(r => r.CreateDate).ToList();
            if (User.IsInRole("admin")) ViewBag.UserId = userId;
            ViewBag.UserName = db.Users.Single(u => u.Id == userId).UserName;
            ViewBag.UserLikes = GetTotalUserLikes(userId);
            return View(GetPreviewSet(reviews));
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
        private List<string> GetReviewTags(int reviewId)
        {
            var tags = db.Tags.Where(t => t.Reviews.Any(r => r.Id == reviewId)).Select(t => t.Name).ToList();
            return tags;
        }

        private async Task UploadPicture(string folder, int file, HttpPostedFileBase picture)
        {
            var converter = new ImageConverter();
            for (int i = 150; i < 301; i += 150)
            {
                var pictureData = (byte[])converter.ConvertTo(ResizePicture(picture, i), typeof(byte[]));
                using (var mem = new MemoryStream(pictureData))
                {
                    string postfix = i == 150 ? "_preview" : "";
                    var updated = await dbx.Files.UploadAsync(
                        $"/ReviewPictures/{folder}/{file + postfix}.jpg",
                        WriteMode.Overwrite.Instance,
                        body: mem);
                }
            }
        }

        private Bitmap ResizePicture(HttpPostedFileBase picture, int size)
        {
            var r = ImageBuilder.Current.Build(new ImageJob
            {
                Source = picture,
                Dest = typeof(Bitmap),
                Instructions = new Instructions($"maxwidth={size}&maxheight={size}&format=jpg"),
                AddFileExtension = true
            });
            return (Bitmap)r.Result;
        }

        public async Task<ActionResult> DownloadPicture(string path, string postfix)
        {
            int size = string.IsNullOrWhiteSpace(postfix) ? 300 : 150;
            if (string.IsNullOrWhiteSpace(path))
            {
                var filler = System.IO.File.ReadAllBytes(Server.MapPath($"~/Files/Images/pictureFiller{postfix}.jpg"));
                return PartialView(new PictureModel { Picture = filler, Size = size });
            }
            using (var response = await dbx.Files.DownloadAsync($"/ReviewPictures{path + postfix}.jpg"))
            {
                var picture = await response.GetContentAsByteArrayAsync();
                return PartialView(new PictureModel { Picture = picture, Size = size });
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
                var comments = db.Comments.Where(c => c.Review.Id == reviewId).OrderBy(c => c.Date).Include("ApplicationUser").ToList();
                var commentsView = comments.Select(c => new CommentViewModel { Author = new UserViewModel { UserName = c.ApplicationUser.UserName, Likes = GetTotalUserLikes(c.ApplicationUser.Id) }, Text = c.Text, Date = c.Date });
                return PartialView(commentsView);
            }
            return RedirectToAction("Index");
        }

        private string GetLocalizedCategory(string category)
        {
            ResourceManager rm = new ResourceManager("AndHowIsItApp.Resources.Language", Assembly.GetExecutingAssembly());
            return rm.GetString(category);
        }

        private string GetSearchLanguage(string text)
        {
            int en = 0; int ru = 0;
            foreach (var t in text.ToLower())
            {
                if (t > 1071 && t < 1104) ru++;
                else if (t > 96 && t < 123) en++;
            }
            return ru > en ? "russian" : "english";
        }
    }
}