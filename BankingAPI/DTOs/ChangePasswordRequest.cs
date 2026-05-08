using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Confirmarea parolei nu corespunde.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}