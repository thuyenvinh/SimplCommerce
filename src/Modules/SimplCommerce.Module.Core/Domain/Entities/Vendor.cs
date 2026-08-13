using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;

namespace SimplCommerce.Module.Core.Models
{
    public class Vendor : EntityBase
    {
        public Vendor()
        {
            CreatedOn = DateTimeOffset.Now;
        }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Name { get; set; }

        [Required(ErrorMessage = "The {0} field is required.")]
        [StringLength(450)]
        public string Slug { get; set; }

        public string Description { get; set; }

        public string Email { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public bool IsActive { get; set; }

        public bool IsDeleted { get; set; }

        // Wave 8: platform commission taken from every sub-order routed to this
        // vendor. Stored as a percent (0-100). Defaults to 0 — a freshly created
        // vendor keeps 100% until admin sets a rate. Per-category overrides are a
        // follow-up; this single rate covers the marketplace MVP.
        public decimal CommissionPercent { get; set; }

        // Wave 11: payout-rail identifier. Stripe Connect example: "acct_xxx".
        // VNPay merchant ID, MoMo partner code etc. share the same field; the
        // payout endpoint picks the right rail based on which is non-null.
        // Storing the raw provider id keeps the platform-side schema simple while
        // the actual SDK call happens at settlement time (Wave 11+ follow-up).
        [StringLength(450)]
        public string StripeAccountId { get; set; }

        [StringLength(450)]
        public string VnpayMerchantId { get; set; }

        [StringLength(450)]
        public string MomoPartnerCode { get; set; }

        // Wave 16: per-vendor shipping override. Flat fee added to the cart total
        // for every order containing this vendor's items. Defaults to 0 — the
        // platform-level shipping provider still applies on top; the vendor fee
        // is a vendor-side handling charge, not a replacement.
        public decimal ShippingFlatFee { get; set; }

        public IList<User> Users { get; set; } = new List<User>();
    }
}
