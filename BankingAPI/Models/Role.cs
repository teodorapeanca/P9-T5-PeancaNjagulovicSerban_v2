using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("roles")]
    public class Role
    {
        [Key]
        [Column("role_id")]
        public long RoleId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("role_name")]
        public string RoleName { get; set; } = string.Empty;
    }
}