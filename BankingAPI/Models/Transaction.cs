using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("transactions")]
    public class Transaction
    {
        [Key]
        [Column("transaction_id")]
        public long TransactionId { get; set; }

        [Required]
        [Column("transaction_uid")]
        public Guid TransactionUid { get; set; } = Guid.NewGuid();

        [Required]
        [Column("initiated_by_user_id")]
        public long InitiatedByUserId { get; set; }

        [Column("from_account_id")]
        public long? FromAccountId { get; set; }

        [Column("to_account_id")]
        public long? ToAccountId { get; set; }

        [Column("provider_id")]
        public long? ProviderId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("type")]
        public string Type { get; set; } = "TRANSFER";

        [Required]
        [Column("amount", TypeName = "numeric(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [Column("currency")]
        public string Currency { get; set; } = "RON";

        [Required]
        [Column("fee_amount", TypeName = "numeric(18,2)")]
        public decimal FeeAmount { get; set; } = 0;

        [Required]
        [MaxLength(50)]
        [Column("status")]
        public string Status { get; set; } = "COMPLETED";

        [Required]
        [Column("aml_flag")]
        public bool AmlFlag { get; set; } = false;

        [MaxLength(500)]
        [Column("description")]
        public string? Description { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}