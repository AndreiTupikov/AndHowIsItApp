using System;
using System.ComponentModel.DataAnnotations;
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
        [Required]
        public string Text { get; set; }
        [Required]
        public int ReviewerRating { get; set; }
        public SelectList AllCategories { get; set; }
    }
    public class PreviewModel
    {
        public int ReviewId { get; set; }
        public string UserId { get; set; }
        public string OwnerName { get; set; }
        public int SubjectId { get; set; }
        public string Subject { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public int Rating { get; set; }
        public int Likes { get; set; }
        public DateTime Date { get; set; }
    }
}