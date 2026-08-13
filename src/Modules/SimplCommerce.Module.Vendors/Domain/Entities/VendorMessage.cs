using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Models
{
    // Wave 15: buyer ↔ vendor messaging. Thread = (CustomerUserId, VendorId);
    // each VendorMessage row is one entry in the thread. OrderId is optional —
    // when set the message is anchored to a specific order (admin / vendor can
    // filter "show me messages about order #1234").
    public enum VendorMessageSenderKind
    {
        Customer = 1,
        Vendor = 5,
        Admin = 10,
    }

    public class VendorMessage : EntityBase
    {
        public VendorMessage()
        {
            CreatedOn = DateTimeOffset.Now;
        }

        public long VendorId { get; set; }

        public Vendor Vendor { get; set; }

        public long CustomerUserId { get; set; }

        public User CustomerUser { get; set; }

        // The user who actually wrote this message — when SenderKind = Customer
        // it equals CustomerUserId; when Vendor / Admin it's the staff user
        // replying on behalf of the vendor or platform.
        public long FromUserId { get; set; }

        public User FromUser { get; set; }

        public VendorMessageSenderKind SenderKind { get; set; }

        // Optional order anchor for the thread.
        public long? OrderId { get; set; }

        [Required]
        [StringLength(4000)]
        public string Body { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        // Marked by the OPPOSITE party when they open the thread. So when a
        // customer message is "read", ReadAt is set by the vendor (or admin)
        // viewing it, and vice versa. Same-side reads don't update this column.
        public DateTimeOffset? ReadAt { get; set; }
    }
}
