using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("accounts")]
    public class Account
    {
        [Key]
        [Column("account_id")]
        public long AccountId { get; set; }

        [Column("account_uid")]
        public Guid AccountUid { get; set; }

        [Column("user_id")]
        public long UserId { get; set; }

        [MaxLength(34)]
        [Column("iban")]
        public string? Iban { get; set; }

        [MaxLength(3)]
        [Column("currency")]
        public string? Currency { get; set; }

        [Column("balance", TypeName = "numeric(18,2)")]
        public decimal Balance { get; set; }

        [MaxLength(50)]
        [Column("status")]
        public string? Status { get; set; }

        [Column("daily_limit", TypeName = "numeric(18,2)")]
        public decimal? DailyLimit { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}