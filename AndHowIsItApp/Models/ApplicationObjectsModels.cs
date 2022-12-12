using Dropbox.Api.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AndHowIsItApp.Models
{
    public class Review
    {
        public int Id { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public string Name { get; set; }
        public Subject Subject { get; set; }
        public string Text { get; set; }
        public int ReviewerRating { get; set; }
        public string PictureLink { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime LastChangeDate { get; set; }
        public ICollection<Tag> Tags { get; set; }
        public ICollection<UserLike> UserLikes { get; set; }
        public Review()
        {
            Tags = new List<Tag>();
            CreateDate = DateTime.Now;
            LastChangeDate = DateTime.Now;
        }
    }
    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SubjectGroup SubjectGroup { get; set; }
        public ICollection<Review> Reviews { get; set;}
        public ICollection<UserRating> UserRatings { get; set;}
    }
    public class SubjectGroup
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Subject> Subjects { get; set; }
    }
    public class Tag
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public Tag()
        {
            Reviews = new List<Review>();
        }
    }
    public class UserRating
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        [Required]
        public ApplicationUser ApplicationUser { get; set; }
        [Required]
        public Subject Subject { get; set;}
    }
    public class UserLike
    {
        public int Id { get; set; }
        [Required]
        public ApplicationUser ApplicationUser { get; set; }
        [Required]
        public Review Review { get; set; }
    }
    public class Comment
    {
        public int Id { get; set; }
        public string Text { get; set; }
        [Required]
        public ApplicationUser ApplicationUser { get; set; }
        [Required]
        public Review Review { get; set; }
        public DateTime Date { get; set; }
        public Comment()
        {
            Date = DateTime.Now;
        }
    }
}