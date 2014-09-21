using System.ComponentModel.DataAnnotations;

namespace FooBox.Models
{
    public class SetupViewModel
    {
        [Required]
        [Display(Name = "Admin user name")]
        public string AdminUserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Admin password")]
        public string AdminPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm admin password")]
        [Compare("AdminPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmAdminPassword { get; set; }
    }
}
