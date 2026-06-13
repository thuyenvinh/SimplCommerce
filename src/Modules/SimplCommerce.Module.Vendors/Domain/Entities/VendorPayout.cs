using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Models
{
    // Wave 11: payout method + settlement status. PayoutMethod identifies which
    // rail the platform used (or will use); Status tracks the transfer lifecycle.
    public enum PayoutMethod
    {
        Manual = 1,         // platform admin pays out-of-band (bank transfer, cash)
        StripeConnect = 5,
        VnpayVendor = 10,
        MomoVendor = 15,
    }

    public enum PayoutStatus
    {
        Pending = 1,        // recorded, not yet sent
        Sent = 5,           // SDK call dispatched / bank transfer initiated
        Completed = 10,     // provider confirms funds settled
        Failed = 15,        // provider returned an error; admin needs to retry
    }

    // Wave 8: a single transfer of accrued vendor earnings out of the platform.
    // Each row groups N completed sub-orders (Order.VendorPayoutId points back
    // here) and stamps the totals at settlement time. Wave 11 adds the
    // method + status workflow + provider response capture.
    public class VendorPayout : EntityBase
    {
        public VendorPayout()
        {
            CreatedOn = DateTimeOffset.Now;
            Method = PayoutMethod.Manual;
            Status = PayoutStatus.Pending;
        }

        public long VendorId { get; set; }

        public Vendor Vendor { get; set; }

        public long CreatedByUserId { get; set; }

        public User CreatedByUser { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        // Gross amount paid out to the vendor BEFORE platform commission. Stored
        // as a snapshot so payout audit doesn't drift when admins later adjust
        // vendor.CommissionPercent.
        public decimal GrossAmount { get; set; }

        // Platform commission withheld on the orders included in this payout.
        public decimal CommissionAmount { get; set; }

        // Net amount actually transferred: GrossAmount - CommissionAmount - any
        // PaymentFee on the included orders (Wave 11 handles fee deduction).
        public decimal NetAmount { get; set; }

        public int OrderCount { get; set; }

        [StringLength(450)]
        public string ExternalTransferReference { get; set; }

        [StringLength(1000)]
        public string Note { get; set; }

        // Wave 11: settlement lifecycle.
        public PayoutMethod Method { get; set; }

        public PayoutStatus Status { get; set; }

        public DateTimeOffset? SentOn { get; set; }

        public DateTimeOffset? CompletedOn { get; set; }

        // Raw JSON / text the provider returned. Stored verbatim for audit; not
        // parsed by the platform. Capped at 4000 so a giant Stripe error blob
        // doesn't blow out a single payout row.
        [StringLength(4000)]
        public string ProviderResponse { get; set; }
    }
}
