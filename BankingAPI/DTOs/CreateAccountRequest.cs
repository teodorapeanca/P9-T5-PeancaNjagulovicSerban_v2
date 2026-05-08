using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class CreateAccountRequest
    {
        [Required]
        public string Currency { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Balance { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal DailyLimit { get; set; } = 0;
    }
}