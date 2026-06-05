using System.Text;
using System.Xml;
using SimplCommerce.Storefront.Services.ApiClients;

namespace SimplCommerce.Storefront.Services;

/// <summary>
/// Builds a sitemap.xml from category + product slugs known to the ApiService.
/// Walks the catalog page-by-page (cap 5000 URLs by default — sitemap.org allows up to 50k,
/// but most SEO crawlers stop reading earlier and the response is meant to be cached anyway).
///
/// Output is valid against https://www.sitemaps.org/protocol.html: includes &lt;loc&gt;,
/// &lt;changefreq&gt;, &lt;priority&gt;. &lt;lastmod&gt; would require an updated_at field
/// on the API contract — left as a follow-up.
/// </summary>
public sealed class SitemapBuilder
{
    private const int MaxUrls = 5000;
    private const int PageSize = 200;

    private readonly ICatalogApi _catalog;

    public SitemapBuilder(ICatalogApi catalog) => _catalog = catalog;

    public async Task<string> BuildAsync(string baseUrl, CancellationToken ct)
    {
        baseUrl = baseUrl.TrimEnd('/');

        var sb = new StringBuilder();
        var settings = new XmlWriterSettings { Indent = false, OmitXmlDeclaration = false, Async = true };
        await using var writer = XmlWriter.Create(sb, settings);

        await writer.WriteStartDocumentAsync();
        writer.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

        WriteUrl(writer, baseUrl + "/", "daily", "1.0");

        var categories = await _catalog.ListCategoriesAsync(ct) ?? Array.Empty<CategoryItem>();
        foreach (var cat in categories)
        {
            WriteUrl(writer, $"{baseUrl}/category/{Uri.EscapeDataString(cat.Slug)}", "weekly", "0.7");
        }

        var emitted = 1 + categories.Count;
        var page = 1;
        while (emitted < MaxUrls)
        {
            var batch = await _catalog.ListProductsAsync(page: page, pageSize: PageSize, ct: ct);
            if (batch is null || batch.Items.Count == 0) break;
            foreach (var p in batch.Items)
            {
                if (emitted >= MaxUrls) break;
                WriteUrl(writer, $"{baseUrl}/product/{Uri.EscapeDataString(p.Slug)}", "weekly", "0.6");
                emitted++;
            }
            if (batch.Items.Count < PageSize) break;
            page++;
        }

        await writer.WriteEndElementAsync();
        await writer.WriteEndDocumentAsync();
        await writer.FlushAsync();
        return sb.ToString();
    }

    private static void WriteUrl(XmlWriter w, string loc, string changefreq, string priority)
    {
        w.WriteStartElement("url");
        w.WriteElementString("loc", loc);
        w.WriteElementString("changefreq", changefreq);
        w.WriteElementString("priority", priority);
        w.WriteEndElement();
    }
}
