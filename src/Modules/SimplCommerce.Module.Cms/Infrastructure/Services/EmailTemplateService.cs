#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SimplCommerce.Infrastructure.Data;
using SimplCommerce.Module.Cms.Models;

namespace SimplCommerce.Module.Cms.Services;

public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly IRepository<EmailTemplate> _templates;

    public EmailTemplateService(IRepository<EmailTemplate> templates)
    {
        _templates = templates;
    }

    public async Task<EmailRenderResult?> RenderAsync(string templateKey, IReadOnlyDictionary<string, string> placeholders)
    {
        var template = await _templates.Query()
            .Where(t => t.Key == templateKey && t.IsActive)
            .FirstOrDefaultAsync();
        if (template is null) return null;

        return new EmailRenderResult(
            Subject: Substitute(template.Subject, placeholders),
            BodyHtml: Substitute(template.BodyHtml ?? string.Empty, placeholders));
    }

    // Plain {{Key}} replacement. No conditional / loop logic — keeps placeholder
    // mis-spellings surface as literal tokens in the rendered output so admins
    // notice during preview, rather than silently emitting empty strings.
    private static string Substitute(string template, IReadOnlyDictionary<string, string> placeholders)
    {
        if (placeholders.Count == 0) return template;
        var sb = new StringBuilder(template);
        foreach (var (key, value) in placeholders)
        {
            sb.Replace("{{" + key + "}}", value ?? string.Empty);
        }
        return sb.ToString();
    }
}
