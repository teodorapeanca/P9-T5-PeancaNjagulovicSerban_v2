using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("notifications")]
    public class Notification
    {
        [Key]
        [Column("notification_id")]
        public long NotificationId { get; set; }

        [Required]
        [Column("notification_uid")]
        public Guid NotificationUid { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("type")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("title")]
        public string Title { get; set; } = "Notification";

        [Required]
        [MaxLength(2000)]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        [Column("channel")]
        public string Channel { get; set; } = "IN_APP";
        // IN_APP / EMAIL

        [Required]
        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Required]
        [Column("is_sent")]
        public bool IsSent { get; set; } = true;

        [Column("related_transaction_id")]
        public long? RelatedTransactionId { get; set; }

        [Required]
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}