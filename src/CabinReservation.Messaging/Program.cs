using Azure.Communication.Email;
using Azure.Communication.Sms;
using CabinReservation.Integration.Contracts.Api;
using CabinReservation.Messaging;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCabinApiClient(builder.Configuration);
builder.Services.AddOptions<MessagingOptions>().Bind(builder.Configuration.GetSection("Messaging")).ValidateOnStart();
builder.Services.AddSingleton(sp => new SmsClient(builder.Configuration["Messaging:AcsConnectionString"] ?? throw new InvalidOperationException("ACS connection string missing.")));
builder.Services.AddSingleton(sp => new EmailClient(builder.Configuration["Messaging:AcsEmailConnectionString"] ?? builder.Configuration["Messaging:AcsConnectionString"] ?? throw new InvalidOperationException("ACS email connection string missing.")));
builder.Services.AddHttpClient<IHermesConversationClient, HermesConversationClient>();
builder.Services.AddSingleton<IMessageTemplateRenderer, MessageTemplateRenderer>();
builder.Services.AddHostedService<OutboxDispatcher>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapPost("/webhooks/acs/sms", SmsWebhook.HandleAsync);
app.Run();
