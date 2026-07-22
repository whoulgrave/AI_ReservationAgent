using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CabinReservation.Api.Tests;

public sealed class ApiSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    public ApiSmokeTests(WebApplicationFactory<Program> factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_is_anonymous_and_healthy()
    {
        using var response = await _client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Protected_endpoint_requires_api_key()
    {
        using var response = await _client.GetAsync("/api/members");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
