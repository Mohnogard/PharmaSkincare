using System.ComponentModel.DataAnnotations;

namespace PharmaSkincare.ViewModels
{
    public class LoginViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }
    }

    public class RegisterViewModel
    {
        [Required, MaxLength(100), Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100), Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? Phone { get; set; }
    }

    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), MinLength(8)]
        [Display(Name = "New Password")]
        public string Password { get; set; } = string.Empty;

        [Required, DataType(DataType.Password), Compare("Password")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class EditProfileViewModel
    {
        [Required, MaxLength(100), Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, MaxLength(100), Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Phone, Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        public string? Address { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImage { get; set; }

        public string? CurrentImageUrl { get; set; }
    }

    public class AuthViewModel
    {
        public LoginViewModel Login { get; set; } = new();
        public RegisterViewModel Register { get; set; } = new();
        public string ActiveTab { get; set; } = "login";
    }

    public class UserManagementViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public List<string> Roles { get; set; } = new();
        public string? SelectedRole { get; set; }
    }
}
