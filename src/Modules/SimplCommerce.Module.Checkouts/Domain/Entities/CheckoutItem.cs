using System;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Catalog.Models;

namespace SimplCommerce.Module.Checkouts.Models
{
    public class CheckoutItem : EntityBase
    {
        public DateTimeOffset CreatedOn { get; set; }

        public long ProductId { get; set; }

        public Product Product { get; set; }

        public int Quantity { get; set; }

        public Guid CheckoutId { get; set; }

        public Checkout Checkout { get; set; }

        // G14: snapshot of the product's calculated price at the moment the
        // CheckoutItem was created. OrderService.CreateOrder compares this against
        // the current Product.Price; a drift larger than the configured threshold
        // (Catalog:PriceDriftThresholdPercent) rejects the order so the buyer
        // doesn't get charged a different amount than they saw at checkout.
        public decimal? LockedPrice { get; set; }

        public DateTimeOffset? LockedPriceOn { get; set; }
    }
}
