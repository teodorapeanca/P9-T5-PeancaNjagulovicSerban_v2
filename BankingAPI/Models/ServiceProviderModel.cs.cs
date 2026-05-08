using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("service_providers")]
    public class ServiceProviderModel
    {
        [Key]
        [Column("provider_id")]
        public long ProviderId { get; set; }

        [Required]
        [Column("provider_uid")]
        public Guid ProviderUid { get; set; }

        [Required]
        [MaxLength(100)]
        [Column("provider_name")]
        public string ProviderName { get; set; } = string.Empty;

        [Required]
        [MaxLength(34)]
        [Column("provider_account_iban")]
        public string ProviderAccountIban { get; set; } = string.Empty;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;
    }
}