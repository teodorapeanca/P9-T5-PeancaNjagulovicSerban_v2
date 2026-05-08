using System.ComponentModel.DataAnnotations;

namespace BankingAPI.DTOs
{
    public class UpdateUserRequest
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}