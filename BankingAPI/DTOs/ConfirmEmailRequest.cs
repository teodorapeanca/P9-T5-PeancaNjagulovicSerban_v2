using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class ConfirmEmailRequest
    {
        [Required]
        public string ClientIdentifier { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}