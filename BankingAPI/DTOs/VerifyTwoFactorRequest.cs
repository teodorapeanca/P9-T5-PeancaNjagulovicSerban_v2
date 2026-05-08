using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class VerifyTwoFactorRequest
    {
        [Required]
        public string ClientIdentifier { get; set; } = string.Empty;

        [Required]
        public string Code { get; set; } = string.Empty;
    }
}