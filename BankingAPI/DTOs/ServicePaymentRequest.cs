public class ServicePaymentRequest
{
    public int InitiatedByUserId { get; set; }

    public string FromIban { get; set; } = string.Empty; // ✔ modificat

    public int ProviderId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}