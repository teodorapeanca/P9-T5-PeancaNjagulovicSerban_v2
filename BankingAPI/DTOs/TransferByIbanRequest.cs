namespace BankingAPI.DTOs
{
    public class TransferByIbanRequest
    {
        public long InitiatedByUserId { get; set; }

        public string FromIban { get; set; } = string.Empty;

        public string ToIban { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
    }
}