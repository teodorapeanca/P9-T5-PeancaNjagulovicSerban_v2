using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class DepositRequest
    {
        [Required]
        public long InitiatedByUserId { get; set; }

        [Required]
        public long AccountId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(3)]
        public string Currency { get; set; } = "RON";

        [MaxLength(500)]
        public string? Description { get; set; }
    }
}