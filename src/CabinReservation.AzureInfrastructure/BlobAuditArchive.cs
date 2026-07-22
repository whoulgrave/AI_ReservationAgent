using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace CabinReservation.AzureInfrastructure;

public sealed class BlobAuditArchive(BlobContainerClient container) : IAuditArchive
{
    public async Task<Uri> UploadAsync(string objectName, Stream content, CancellationToken ct)
    {
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        var blob = container.GetBlobClient(objectName);
        await blob.UploadAsync(content, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/x-ndjson" } }, ct);
        return blob.Uri;
    }
}
