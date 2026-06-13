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

        public IList<User> Users { get; set; } = new List<User>();
    }
}
