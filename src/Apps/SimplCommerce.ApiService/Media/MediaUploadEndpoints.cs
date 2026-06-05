using Microsoft.AspNetCore.Http.HttpResults;
using SimplCommerce.Module.Core.Services;

namespace SimplCommerce.ApiService.Media;

/// <summary>
/// Media upload gateway. Forwards the uploaded file to the active <see cref="IStorageService"/>
/// implementation (StorageLocal today, StorageAzureBlob / StorageAmazonS3 in production
/// via module swap). Resizing pipeline + thumbnail generation are Phase 7 hardening work.
/// </summary>
public static class MediaUploadEndpoints
{
    public record UploadResponse(string Url, string FileName);

    public static IEndpointRouteBuilder MapMediaEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/media")
            .WithTags("Media")
            .RequireAuthorization("AdminOrVendor")
            .DisableAntiforgery(); // multipart POST from SPA/BFF; CSRF is a BFF concern

        group.MapPost("/upload", UploadAsync);

        return app;
    }

    private static async Task<Results<Ok<UploadResponse>, BadRequest<string>>> UploadAsync(
        IFormFile file,
        IStorageService storage)
    {
        if (file is null || file.Length == 0)
        {
            return TypedResults.BadRequest("No file uploaded.");
        }
        if (file.Length > 10 * 1024 * 1024)
        {
            return TypedResults.BadRequest("File exceeds 10 MB size limit.");
        }

        await using var stream = file.OpenReadStream();
        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}";
        await storage.SaveMediaAsync(stream, fileName, file.ContentType);
        return TypedResults.Ok(new UploadResponse(storage.GetMediaUrl(fileName), fileName));
    }
}
