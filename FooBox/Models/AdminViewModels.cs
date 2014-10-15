using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;



namespace FooBox.Models
{

    public class AdminNewUserViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "User name")]
        public string Name { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("First name")]
        public string FirstName { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("Last name")]
        public string LastName { get; set; }

                
        [DisplayName("Quota Limit (MB)")]
        public long QuotaLimit { get; set; }
    }

    public class AdminEditUserViewModel
    {
        public long Id { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "User name")]
        public string Name { get; set; }


        [DataType(DataType.Text)]
        [DisplayName("First name")]
        public string FirstName { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("Last name")]
        public string LastName { get; set; }


        [DisplayName("Quota Limit (MB)")]
        public long QuotaLimit { get; set; }
    }

    public class AdminNewGroupViewModel
    {
        [Required]
        [DataType(DataType.Text)]
        [DisplayName("Name")]
        public string Name { get; set; }

        public long Id { get; set; }

        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }

    

    
        public List<UserSelectedViewModel> Users { get; set; }
    }


    public class AdminEditGroupViewModel
    {
        public long Id { get; set; }


        [Required]
        [DataType(DataType.Text)]
        [DisplayName("Name")]
        public string Name { get; set; }


        [DataType(DataType.MultilineText)]
        [DisplayName("Description")]
        public string Description { get; set; }


        public List<UserSelectedViewModel> Users { get; set; }

    }
    public class UserSelectedViewModel
    {
        public long Id { get; set; }

        public bool IsSelected { get; set; }

        public string Name { get; set; }
    }
}
