#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SimplCommerce.Module.Cms.Services;

/// <summary>
/// Wave 17: rendering façade. Callers pass a template key plus a flat dictionary
/// of placeholder values; the service looks up the active template by key,
/// substitutes <c>{{Placeholder}}</c> tokens, and returns subject + body. When
/// no row is active the call returns null and the caller falls back to its
/// hard-coded default — so admins removing a template never breaks email
/// delivery, it just rewinds to the inline copy.
/// </summary>
public interface IEmailTemplateService
{
    Task<EmailRenderResult?> RenderAsync(string templateKey, IReadOnlyDictionary<string, string> placeholders);
}

public sealed record EmailRenderResult(string Subject, string BodyHtml);
