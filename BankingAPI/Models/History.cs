using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("history")]
    public class History
    {
        [Key]
        [Column("id")]
        public long Id { get; set; }

        [Column("transaction_id")]
        public long TransactionId { get; set; }

        [Required, MaxLength(30)]
        [Column("event_type")]
        public string EventType { get; set; } = "CREATED";

        [MaxLength(500)]
        [Column("details")]
        public string? Details { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}