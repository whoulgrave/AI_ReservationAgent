using CabinReservation.Integration.Contracts.Outbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CabinReservation.Integration.Contracts.Api;

public static class IntegrationRegistration
{
    public static IServiceCollection AddCabinApiClient(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(CabinApiOptions.SectionName);
        services.AddOptions<CabinApiOptions>().Bind(section).ValidateOnStart();
        var options = section.Get<CabinApiOptions>() ?? throw new InvalidOperationException("CabinApi configuration is missing.");
        services.AddHttpClient<ICabinApiClient, CabinApiClient>(client =>
        {
            client.BaseAddress = options.BaseAddress;
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        }).AddStandardResilienceHandler();
        services.AddHttpClient<IOutboxApiClient, OutboxApiClient>(client =>
        {
            client.BaseAddress = options.BaseAddress;
            client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
        }).AddStandardResilienceHandler();
        return services;
    }
}
