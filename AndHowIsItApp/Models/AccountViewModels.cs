using AndHowIsItApp.Resources;
using System.ComponentModel.DataAnnotations;

namespace AndHowIsItApp.Models
{
    public class ExternalLoginConfirmationViewModel
    {
        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "UserNameRequiredError")]
        [Display(Name = "UserName", ResourceType = typeof(Language))]
        public string UserName { get; set; }

        [Required(ErrorMessageResourceType = typeof(Language), ErrorMessageResourceName = "EmailRequiredError")]
        [EmailAddress]
        [Display(Name = "EmailAddress", ResourceType = typeof(Language))]
        public string Email { get; set; }
    }

    public class ExternalLoginListViewModel
    {
        public string ReturnUrl { get; set; }
    }
}
