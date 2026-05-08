using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}