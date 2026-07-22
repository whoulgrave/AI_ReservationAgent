using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;

namespace CabinReservation.AzureInfrastructure;
public sealed class KeyVaultSecretProvider(SecretClient client, IMemoryCache cache) : ISecretProvider
{
    public async Task<string> GetAsync(string name, CancellationToken ct)
    {
        if (cache.TryGetValue(name, out string? value) && value is not null) return value;
        var secret = await client.GetSecretAsync(name, cancellationToken: ct);
        value = secret.Value.Value;
        cache.Set(name, value, TimeSpan.FromMinutes(15));
        return value;
    }
}
