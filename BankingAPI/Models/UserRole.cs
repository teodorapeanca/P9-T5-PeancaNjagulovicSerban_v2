using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("user_roles")]
    public class UserRole
    {
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("role_id")]
        public long RoleId { get; set; }
    }
}