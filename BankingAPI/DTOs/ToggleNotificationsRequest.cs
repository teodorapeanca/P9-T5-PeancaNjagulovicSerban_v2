using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class ToggleNotificationsRequest
    {
        [Required]
        public bool Enabled { get; set; }
    }
}