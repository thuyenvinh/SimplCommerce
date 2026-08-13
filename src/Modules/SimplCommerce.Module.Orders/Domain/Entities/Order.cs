using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Orders.Models
{
    public class Order : EntityBase
    {
        public Order()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
            OrderStatus = OrderStatus.New;
            IsMasterOrder = false;
        }

        public long CustomerId { get; set; }

        [JsonIgnore] // To simplify the json stored in order history
        public User Customer { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public long LatestUpdatedById { get; set; }

        [JsonIgnore]
        public User LatestUpdatedBy { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long CreatedById { get; set; }

        [JsonIgnore]
        public User CreatedBy { get; set; }

        public long? VendorId { get; set; }

        [StringLength(450)]
        public string CouponCode { get; set; }

        [StringLength(450)]
        public string CouponRuleName { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal SubTotal { get; set; }

        public decimal SubTotalWithDiscount { get; set; }

        public long ShippingAddressId { get; set; }

        public OrderAddress ShippingAddress { get; set; }

        public long BillingAddressId { get; set; }

        public OrderAddress BillingAddress { get; set; }

        public IList<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public OrderStatus OrderStatus { get; set; }

        [StringLength(1000)]
        public string OrderNote { get; set; }

        public long? ParentId { get; set; }

        [JsonIgnore]
        public Order Parent { get; set; }

        public bool IsMasterOrder { get; set; }

        [StringLength(450)]
        public string ShippingMethod { get; set; }

        public decimal ShippingFeeAmount { get; set; }

        public decimal TaxAmount { get; set; }

        public decimal OrderTotal { get; set; }

        [StringLength(450)]
        public string PaymentMethod { get; set; }

        public decimal PaymentFeeAmount { get; set; }

        // Wave 8: platform commission accrued on this (sub-)order. Always zero on
        // master orders (they have no VendorId) and on plain platform-owned orders;
        // populated only on vendor sub-orders, computed at CreateOrder time from
        // (sub-order subtotal * vendor.CommissionPercent / 100).
        public decimal CommissionAmount { get; set; }

        // Marks that the platform has paid this sub-order's vendor portion out.
        // VendorPayout records the actual transfer + groups multiple orders into
        // one payout for reporting.
        public long? VendorPayoutId { get; set; }

        public IList<Order> Children { get; protected set; } = new List<Order>();

        public void AddOrderItem(OrderItem item)
        {
            item.Order = this;
            OrderItems.Add(item);
        }
    }
}
