using Microsoft.EntityFrameworkCore;
using BankingAPI.Models;

namespace BankingAPI.Data
{
    public class BankingDbContext : DbContext
    {
        public BankingDbContext(DbContextOptions<BankingDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<ServiceProviderModel> ServiceProviders { get; set; }
        public DbSet<History> History { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");

                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.UserUid).HasColumnName("user_uid");
                entity.Property(e => e.ClientIdentifier).HasColumnName("client_identifier");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.Phone).HasColumnName("phone");
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
                entity.Property(e => e.FirstName).HasColumnName("first_name");
                entity.Property(e => e.LastName).HasColumnName("last_name");
                entity.Property(e => e.Address).HasColumnName("address");
                entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
                entity.Property(e => e.AcceptedTerms).HasColumnName("accepted_terms");
                entity.Property(e => e.FailedLoginAttempts).HasColumnName("failed_login_attempts");
                entity.Property(e => e.LockoutEnd).HasColumnName("lockout_end");
                entity.Property(e => e.CurrentSessionId).HasColumnName("current_session_id");
                entity.Property(e => e.EmailConfirmed).HasColumnName("email_confirmed");
                entity.Property(e => e.EmailConfirmationToken).HasColumnName("email_confirmation_token");
                entity.Property(e => e.PasswordResetToken).HasColumnName("password_reset_token");
                entity.Property(e => e.PasswordResetExpiresAt).HasColumnName("password_reset_expires_at");
                entity.Property(e => e.TwoFactorCode).HasColumnName("two_factor_code");
                entity.Property(e => e.TwoFactorExpiresAt).HasColumnName("two_factor_expires_at");
                entity.Property(e => e.LastActivityAt).HasColumnName("last_activity_at");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
                entity.Property(e => e.NotificationLevel).HasColumnName("notification_level");
                entity.Property(e => e.NotificationsEnabled).HasColumnName("notifications_enabled");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            });

            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.RoleId).HasColumnName("role_id");
                entity.Property(e => e.RoleName).HasColumnName("role_name");
            });

            modelBuilder.Entity<UserRole>(entity =>
            {
                entity.ToTable("user_roles");
                entity.HasKey(ur => new { ur.UserId, ur.RoleId });
                entity.Property(ur => ur.UserId).HasColumnName("user_id");
                entity.Property(ur => ur.RoleId).HasColumnName("role_id");
            });

            modelBuilder.Entity<ServiceProviderModel>(entity =>
            {
                entity.ToTable("service_providers");
                entity.HasKey(e => e.ProviderId);
                entity.Property(e => e.ProviderId).HasColumnName("provider_id");
                entity.Property(e => e.ProviderUid).HasColumnName("provider_uid");
                entity.Property(e => e.ProviderName).HasColumnName("provider_name");
                entity.Property(e => e.ProviderAccountIban).HasColumnName("provider_account_iban");
                entity.Property(e => e.IsActive).HasColumnName("is_active");
            });

            modelBuilder.Entity<Notification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.NotificationId).HasColumnName("notification_id");
                entity.Property(e => e.NotificationUid).HasColumnName("notification_uid");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Type).HasColumnName("type");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Message).HasColumnName("message");
                entity.Property(e => e.Channel).HasColumnName("channel");
                entity.Property(e => e.IsRead).HasColumnName("is_read");
                entity.Property(e => e.IsSent).HasColumnName("is_sent");
                entity.Property(e => e.RelatedTransactionId).HasColumnName("related_transaction_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.ToTable("audit_logs");
                entity.HasKey(e => e.AuditId);
                entity.Property(e => e.AuditId).HasColumnName("audit_id");
                entity.Property(e => e.AuditUid).HasColumnName("audit_uid");
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.Action).HasColumnName("action");
                entity.Property(e => e.EntityType).HasColumnName("entity_type");
                entity.Property(e => e.EntityId).HasColumnName("entity_id");
                entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasColumnType("inet");
                entity.Property(e => e.HashValue).HasColumnName("hash_value");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            });
        }
    }
}