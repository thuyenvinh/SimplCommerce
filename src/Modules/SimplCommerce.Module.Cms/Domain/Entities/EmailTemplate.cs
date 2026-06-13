using System;
using System.ComponentModel.DataAnnotations;
using SimplCommerce.Infrastructure.Models;

namespace SimplCommerce.Module.Cms.Models
{
    // Wave 17: admin-editable email template. Modules send mail via
    // IEmailTemplateService.RenderAsync("template-key", model) — the service
    // looks up the template by Key and substitutes placeholders. When no row
    // is found the service falls back to the module's hard-coded default
    // (kept inline in each service for cold-boot safety).
    public class EmailTemplate : EntityBase
    {
        public EmailTemplate()
        {
            CreatedOn = DateTimeOffset.Now;
            LatestUpdatedOn = DateTimeOffset.Now;
            IsActive = true;
        }

        // Stable identifier modules look up by. Examples: "order-confirmation",
        // "vendor-application-approved", "back-in-stock". Unique.
        [Required]
        [StringLength(450)]
        public string Key { get; set; }

        // Human-readable name shown in the admin CMS list.
        [Required]
        [StringLength(450)]
        public string Name { get; set; }

        [Required]
        [StringLength(450)]
        public string Subject { get; set; }

        // HTML body with {{Placeholder}} substitution. The exact placeholder set
        // is documented per template in the admin UI; mis-spelled placeholders
        // stay literal in the output so admins notice during preview.
        public string BodyHtml { get; set; }

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedOn { get; set; }

        public DateTimeOffset LatestUpdatedOn { get; set; }
    }
}
