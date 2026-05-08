using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class TransactionHistoryFilterRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MinAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? MaxAmount { get; set; }

        [MaxLength(50)]
        public string? Type { get; set; }

        [MaxLength(50)]
        public string? Status { get; set; }

        [MaxLength(3)]
        public string? Currency { get; set; }
    }
}