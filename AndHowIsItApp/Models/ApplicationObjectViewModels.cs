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
        [Required]
        [StringLength(50, ErrorMessage = "Название обзора должно быть в пределах 50 символов")]
        public string Title { get; set; }
        [Required]
        public int Category { get; set; }
        [Required]
        public string Subject { get; set; }
        [ValidateFile]
        public HttpPostedFileBase Picture { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        [Range(1, 10, ErrorMessage = "Оценка от 1 до 10")]
        public int ReviewerRating { get; set; }
        public SelectList AllCategories { get; set; }
    }
    public class ReviewEditViewModel
    {
        public int ReviewId { get; set; }
        public string UserId { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Название обзора должно быть в пределах 50 символов")]
        public string Title { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        [Range(1, 10, ErrorMessage = "Оценка от 1 до 10")]
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
                ErrorMessage = "Расширение файла должно быть .jpg или .jpeg";
                return false;
            }
            if (file.ContentLength > maxSize)
            {
                ErrorMessage = "Размер файла не должен превышать 2 МБ.";
                return false;
            }
            return true;
        }
    }
}