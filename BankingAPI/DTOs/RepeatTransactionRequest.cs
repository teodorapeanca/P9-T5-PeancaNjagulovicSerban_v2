using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class RepeatTransactionRequest
    {
        [Required]
        public long TransactionId { get; set; }
    }
}