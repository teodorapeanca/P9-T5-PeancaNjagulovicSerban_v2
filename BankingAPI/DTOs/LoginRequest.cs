using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string ClientIdentifier { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}