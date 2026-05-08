using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankingAPI.Models
{
    [Table("users")]
    public class User
    {
        [Key]
        [Column("user_id")]
        public long UserId { get; set; }

        [Required]
        [Column("user_uid")]
        public Guid UserUid { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("client_identifier")]
        public string ClientIdentifier { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        [Column("phone")]
        public string? Phone { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("first_name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [Column("last_name")]
        public string LastName { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("address")]
        public string? Address { get; set; }

        [Column("date_of_birth")]
        public DateTime DateOfBirth { get; set; }

        [Column("accepted_terms")]
        public bool AcceptedTerms { get; set; }

        [Column("failed_login_attempts")]
        public int FailedLoginAttempts { get; set; }

        [Column("lockout_end")]
        public DateTime? LockoutEnd { get; set; }

        [Column("current_session_id")]
        public string? CurrentSessionId { get; set; }

        [Column("email_confirmed")]
        public bool EmailConfirmed { get; set; }

        [Column("email_confirmation_token")]
        public string? EmailConfirmationToken { get; set; }

        [Column("password_reset_token")]
        public string? PasswordResetToken { get; set; }

        [Column("password_reset_expires_at")]
        public DateTime? PasswordResetExpiresAt { get; set; }

        [Column("two_factor_code")]
        public string? TwoFactorCode { get; set; }

        [Column("two_factor_expires_at")]
        public DateTime? TwoFactorExpiresAt { get; set; }

        [Column("last_activity_at")]
        public DateTime? LastActivityAt { get; set; }
        [Column("notifications_enabled")]
        public bool NotificationsEnabled { get; set; } = true;
        [Column("is_active")]
        public bool IsActive { get; set; }

        [Required]
        [MaxLength(10)]
        [Column("notification_level")]
        public string NotificationLevel { get; set; } = "Medium";

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}