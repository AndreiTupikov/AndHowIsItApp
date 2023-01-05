using AndHowIsItApp.Resources;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AndHowIsItApp.Models
{
    public class ReviewCreateViewModel
    {
        public string UserId { get; set; }

        [Display(Name = "Title", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TitleRequiredError")]
        [StringLength(50, ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TitleLengthError")]
        public string Title { get; set; }

        [Display(Name = "Category", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "CategoryRequiredError")]
        public int Category { get; set; }

        [Display(Name = "Subject", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "SubjectRequiredError")]
        [StringLength(50, ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "SubjectLengthError")]
        public string Subject { get; set; }

        [ValidateFile]
        [Display(Name = "Picture", ResourceType = typeof(Language))]
        public HttpPostedFileBase Picture { get; set; }

        [Display(Name = "Text", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TextRequiredError")]
        public string Text { get; set; }

        [Display(Name = "AuthorsRating", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "RatingRequiredError")]
        [Range(1, 10, ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "RatingRangeError")]
        public int ReviewerRating { get; set; }

        public SelectList AllCategories { get; set; }
    }
    public class ReviewEditViewModel
    {
        [Required]
        public int ReviewId { get; set; }

        public string UserId { get; set; }

        [Display(Name = "Title", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TitleRequiredError")]
        [StringLength(50, ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TitleLengthError")]
        public string Title { get; set; }

        [Display(Name = "Text", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "TextRequiredError")]
        public string Text { get; set; }

        [Display(Name = "AuthorsRating", ResourceType = typeof(Language))]
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "RatingRequiredError")]
        [Range(1, 10, ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "RatingRangeError")]
        public int ReviewerRating { get; set; }

        public List<string> Tags { get; set; }
    }
    public class ReviewShowViewModel
    {
        public int Id { get; set; }
        public string AuthorId { get; set; }
        public UserViewModel Author { get; set; }
        public SubjectViewModel Subject { get; set; }
        public string PictureLink { get; set; }
        public string Title { get; set; }
        public int Likes { get; set; }
        public string Text { get; set; }
        public int Rating { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public List<string> Tags { get; set; }
    }
    public class PreviewModel
    {
        public int ReviewId { get; set; }
        public UserViewModel Author { get; set; }
        public SubjectViewModel Subject { get; set; }
        public string PictureLink { get; set; }
        public string Title { get; set; }
        public int Rating { get; set; }
        public int Likes { get; set; }
        public DateTime Date { get; set; }
    }
    public class UserViewModel
    {
        public string UserName { get; set; }
        public int Likes { get; set; }
    }
    public class SubjectViewModel
    {
        public int Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
    }
    public class CommentViewModel
    {
        public UserViewModel Author { get; set; }
        public string Text { get; set; }
        public DateTime Date { get; set; }
    }
    public class PictureModel
    {
        public byte[] Picture { get; set; }
        public int Size { get; set; }
    }
    public class UserAdministrationModel
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsAdmin { get; set; }
    }
    public class ValidateFileAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            int maxSize = 1024 * 1024 * 2;
            string[] AllowedExtensions = new string[] { ".jpg", ".jpeg" };
            var file = value as HttpPostedFileBase;
            if (file == null) return true;
            if (!AllowedExtensions.Contains(file.FileName.Substring(file.FileName.LastIndexOf('.'))))
            {
                ErrorMessageResourceType = typeof(Language);
                ErrorMessageResourceName = "PictureExtensionError";
                return false;
            }
            if (file.ContentLength > maxSize)
            {
                ErrorMessageResourceType = typeof(Language);
                ErrorMessageResourceName = "PictureSizeError";
                return false;
            }
            return true;
        }
    }
}