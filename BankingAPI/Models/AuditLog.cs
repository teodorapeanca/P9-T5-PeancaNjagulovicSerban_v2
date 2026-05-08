using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;

namespace BankingAPI.Models
{
    [Table("audit_logs")]
    public class AuditLog
    {
        [Key]
        [Column("audit_id")]
        public long AuditId { get; set; }

        [Column("audit_uid")]
        public Guid AuditUid { get; set; } = Guid.NewGuid();

        [Column("user_id")]
        public long? UserId { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        [Column("entity_type")]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(50)]
        [Column("entity_id")]
        public string? EntityId { get; set; }

        [Column("ip_address", TypeName = "inet")]
        public IPAddress? IpAddress { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("hash_value")]
        public string HashValue { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}