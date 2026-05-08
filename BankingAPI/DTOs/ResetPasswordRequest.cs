using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class ResetPasswordRequest
    {
        [Required]
        public string ClientIdentifier { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Confirmarea parolei nu corespunde.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}