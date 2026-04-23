using Ganss.Xss;

namespace SimplCommerce.ApiService.Hardening;

/// <summary>
/// Shared <see cref="HtmlSanitizer"/> singleton configured to match the SimplCommerce
/// threat model: accept basic rich-text tags + safe attributes, strip event handlers,
/// javascript: URIs, embed/object/iframe. Used by review submission + CMS page bodies.
/// </summary>
public static class HtmlSanitizerExtensions
{
    public static IServiceCollection AddSimplHtmlSanitizer(this IServiceCollection services)
    {
        services.AddSingleton<IHtmlSanitizer>(_ =>
        {
            var s = new HtmlSanitizer();
            // Defaults already cover the common safe tag set. Tighten by removing the
            // ones we never want the ecommerce UI to render.
            s.AllowedTags.Remove("iframe");
            s.AllowedTags.Remove("object");
            s.AllowedTags.Remove("embed");
            s.AllowedTags.Remove("form");
            s.AllowedSchemes.Remove("javascript");
            s.AllowedSchemes.Remove("vbscript");
            return s;
        });
        return services;
    }
}
