using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;
using SimplCommerce.Module.Core.Models;

namespace SimplCommerce.Module.Vendors.Models
{
    public enum VendorDocumentType
    {
        BusinessRegistration = 1,
        TaxId = 5,
        IdentityProof = 10,
        BankStatement = 15,
        AddressProof = 20,
        Other = 90,
    }

    public enum VendorDocumentStatus
    {
        Submitted = 1,
        Verified = 5,
        Rejected = 10,
    }

    // Wave 14: KYC artifact upload. Either attached to a VendorApplication
    // (during onboarding) or to an active Vendor (post-approval re-verification).
    // The file itself is stored via IMediaService; this row carries the
    // verification workflow metadata.
    public class VendorDocument : EntityBase
    {
        public VendorDocument()
        {
            CreatedOn = DateTimeOffset.Now;
            Status = VendorDocumentStatus.Submitted;
        }

        public long? VendorApplicationId { get; set; }

        public VendorApplication VendorApplication { get; set; }

        public long? VendorId { get; set; }

        public Vendor Vendor { get; set; }

        // The Core_Media row that actually holds the upload. IMediaService writes
        // the binary via a storage provider (Local / S3 / Azure Blob) keyed by
        // the FileName, which we copy onto Media.FileName at upload time.
        public long MediaId { get; set; }

        public Media Media { get; set; }

        public VendorDocumentType DocumentType { get; set; }

        public VendorDocumentStatus Status { get; set; }

        public long UploadedByUserId { get; set; }

        public User UploadedByUser { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public long? VerifiedByUserId { get; set; }

        public User VerifiedByUser { get; set; }

        public DateTimeOffset? VerifiedOn { get; set; }

        [StringLength(2000)]
        public string AdminNote { get; set; }
    }
}
