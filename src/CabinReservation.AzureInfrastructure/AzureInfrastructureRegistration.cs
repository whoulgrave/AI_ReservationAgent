using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CabinReservation.AzureInfrastructure;

public static class AzureInfrastructureRegistration
{
    public static IServiceCollection AddCabinAzureInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        var credential = new DefaultAzureCredential();
        var vault = new Uri(configuration["Azure:KeyVaultUri"] ?? throw new InvalidOperationException("Azure:KeyVaultUri missing."));
        var storage = new Uri(configuration["Azure:AuditContainerUri"] ?? throw new InvalidOperationException("Azure:AuditContainerUri missing."));
        services.AddSingleton(new SecretClient(vault, credential));
        services.AddSingleton(new BlobContainerClient(storage, credential));
        services.AddSingleton<ISecretProvider, KeyVaultSecretProvider>();
        services.AddSingleton<IAuditArchive, BlobAuditArchive>();
        return services;
    }
}
