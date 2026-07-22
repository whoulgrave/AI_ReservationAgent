namespace CabinReservation.AzureInfrastructure;

public interface ISecretProvider { Task<string> GetAsync(string name, CancellationToken ct); }
