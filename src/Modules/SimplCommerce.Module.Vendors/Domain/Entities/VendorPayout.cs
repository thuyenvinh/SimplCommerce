using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Models
{
    // Wave 8: a single transfer of accrued vendor earnings out of the platform.
    // Each row groups N completed sub-orders (Order.VendorPayoutId points back
    // here) and stamps the totals at settlement time. Real money movement (Stripe
    // Connect / VNPay vendor sub-account) is Wave 11; this entity gives us the
    // ledger that Wave 11 will mark as "transfer initiated / settled".
    public class VendorPayout : EntityBase
    {
        public VendorPayout()
        {
            CreatedOn = DateTimeOffset.Now;
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
    }
}
