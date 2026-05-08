using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class UpdateNotificationLevelRequest
    {
        [Required]
        [RegularExpression("^(Low|Medium|High)$", ErrorMessage = "Nivelul trebuie să fie Low, Medium sau High.")]
        public string NotificationLevel { get; set; } = "Medium";
    }
}