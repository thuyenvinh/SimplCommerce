namespace SimplCommerce.Module.Payments.Models
{
    public enum PaymentStatus
    {
        Succeeded = 1,

        Failed = 5,

        // G34: terminal state for a fully-refunded payment. Partial refunds keep
        // Status = Succeeded and surface their value via Payment.RefundedAmount.
        Refunded = 10
    }
}
