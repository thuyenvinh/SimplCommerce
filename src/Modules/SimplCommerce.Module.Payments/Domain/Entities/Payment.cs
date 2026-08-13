using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Orders.Models;

namespace SimplCommerce.Module.Payments.Models
{
    public class Payment : EntityBase
    {
        public Payment()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
        }

        public long OrderId { get; set; }

        public Order Order { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }

        public decimal Amount { get; set; }

        public decimal PaymentFee { get; set; }

        [StringLength(450)]
        public string PaymentMethod { get; set; }

        [StringLength(450)]
        public string GatewayTransactionId { get; set; }

        public PaymentStatus Status { get; set; }

        public string FailureMessage { get; set; }

        // G03: how much of Amount has actually been refunded. Null = no refunds.
        // Status flips to Refunded only when RefundedAmount == Amount; partials
        // keep Status = Succeeded so capture-vs-refund stays distinguishable.
        public decimal? RefundedAmount { get; set; }

        public DateTimeOffset? RefundedOn { get; set; }
    }
}
