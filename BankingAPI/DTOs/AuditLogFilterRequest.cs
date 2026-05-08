using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class AuditLogFilterRequest
    {
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        [MaxLength(100)]
        public string? Action { get; set; }

        [MaxLength(50)]
        public string? EntityType { get; set; }

        public long? UserId { get; set; }
    }
}