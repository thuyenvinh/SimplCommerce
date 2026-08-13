using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Models
{
    // Wave 7: marketplace onboarding. A customer submits a VendorApplication
    // from the storefront, an admin reviews + approves/rejects from the admin
    // queue. On approval a Vendor row is created, User.VendorId is stamped,
    // and the user gains the "vendor" role.
    public enum VendorApplicationStatus
    {
        Pending = 1,
        Approved = 5,
        Rejected = 10,
    }

    public class VendorApplication : EntityBase
    {
        public VendorApplication()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
            Status = VendorApplicationStatus.Pending;
        }

        // The customer-account user submitting the application. We keep this
        // distinct from the resulting Vendor.Users so we can show "applied by"
        // separately from "active vendor staff" once the application is approved.
        public long ApplicantUserId { get; set; }

        public User ApplicantUser { get; set; }

        [Required]
        [StringLength(450)]
        public string BusinessName { get; set; }

        [Required]
        [StringLength(450)]
        public string Slug { get; set; }

        public string Description { get; set; }

        [StringLength(450)]
        public string ContactEmail { get; set; }

        [StringLength(100)]
        public string ContactPhone { get; set; }

        // Free-text registration documents identifier — actual KYC upload is a
        // follow-up (Wave 7+ would integrate a doc-storage flow). For now the
        // string just stores "self-declared" / "received via email" markers.
        [StringLength(1000)]
        public string RegistrationNote { get; set; }

        public VendorApplicationStatus Status { get; set; }

        // Admin who actioned the decision + reason. Null until first action.
        public long? DecidedByUserId { get; set; }

        public DateTimeOffset? DecidedOn { get; set; }

        [StringLength(2000)]
        public string DecisionNote { get; set; }

        // Stamped on approval so we can navigate from the application back to
        // the created Vendor row without a separate join.
        public long? CreatedVendorId { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
    }
}
