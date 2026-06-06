using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Checkouts.Models
{
    public class Checkout : EntityBaseWithTypedId<Guid>
    {
        public Checkout()
        {
            Id = Guid.NewGuid();
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }

        public IList<CheckoutItem> CheckoutItems { get; protected set; } = new List<CheckoutItem>();

        public long CustomerId { get; set; }

        public User Customer { get; set; }

        public long CreatedById { get; set; }

        public User CreatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        [StringLength(450)]
        public string CouponCode { get; set; }

        [StringLength(450)]
        public string CouponRuleName { get; set; }

        [StringLength(450)]
        public string ShippingMethod { get; set; }

        public bool IsProductPriceIncludeTax { get; set; }

        public decimal? ShippingAmount { get; set; }

        public decimal? TaxAmount { get; set; }

        public long? VendorId { get; set; }

        /// <summary>
        /// Json serialized of shipping form
        /// </summary>
        public string ShippingData { get; set; }

        [StringLength(1000)]
        public string OrderNote { get; set; }

        // G04: idempotency cookie. CreateOrder stamps the resulting Order.Id back
        // here so retry callers (e.g. VNPay return + IPN both fire on a single
        // checkout) get the same order instead of a duplicate row.
        public long? OrderCreatedId { get; set; }
    }
}
