using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class MaintenanceNotificationRequest
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(2000)]
        public string Message { get; set; } = string.Empty;
    }
}