﻿using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;

namespace AndHowIsItApp.Models
{
    public class ReviewCreateViewModel
    {
        public string UserId { get; set; }
        [Required]
        [StringLength(50, ErrorMessage = "Название обзора должно быть в пределах 50 символов")]
        public string Name { get; set; }
        [Required]
        public int SubjectGroup { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Text { get; set; }
        [Required]
        public int ReviewerRating { get; set; }
        public string Image { get; set; }
        public string Tags { get; set; }
        public SelectList AllTags { get; set; }
        public SelectList AllSubjectGroups { get; set; }
    }
}