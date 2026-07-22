using Azure.Communication.CallAutomation;
using CabinReservation.Integration.Contracts.Api;
using CabinReservation.Voice;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCabinApiClient(builder.Configuration);
builder.Services.AddOptions<VoiceOptions>().Bind(builder.Configuration.GetSection("Voice")).ValidateOnStart();
builder.Services.AddSingleton(_ => new CallAutomationClient(builder.Configuration["Voice:AcsConnectionString"] ?? throw new InvalidOperationException("ACS connection string missing.")));
builder.Services.AddSingleton<ICallSessionStore, MemoryCallSessionStore>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapPost("/webhooks/acs/calls/incoming", VoiceEndpoints.IncomingAsync);
app.MapPost("/webhooks/acs/calls/events/{contextId}", VoiceEndpoints.EventsAsync);
app.Run();
