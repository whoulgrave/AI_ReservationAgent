namespace CabinReservation.AzureInfrastructure;

public interface IAuditArchive { Task<Uri> UploadAsync(string objectName, Stream content, CancellationToken ct); }
