using Azure.Identity;
using CabinReservation.EmailIntake;
using Microsoft.Graph;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddOptions<EmailIntakeOptions>().Bind(builder.Configuration.GetSection("EmailIntake")).ValidateOnStart();
builder.Services.AddSingleton(sp => new GraphServiceClient(new ClientSecretCredential(
    builder.Configuration["EmailIntake:TenantId"], builder.Configuration["EmailIntake:ClientId"], builder.Configuration["EmailIntake:ClientSecret"])));
builder.Services.AddHttpClient<IHermesEmailClient, HermesEmailClient>();
builder.Services.AddHostedService<MailboxWorker>();
await builder.Build().RunAsync();
