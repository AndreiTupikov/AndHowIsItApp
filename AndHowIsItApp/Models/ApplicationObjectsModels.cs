﻿using System;
using System.Collections.Generic;

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
        //public string Image { get; set; }
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
        public ApplicationUser ApplicationUser { get; set; }
        public Subject Subject { get; set;}
    }
    public class UserLike
    {
        public int Id { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public Review Review { get; set; }
    }
}