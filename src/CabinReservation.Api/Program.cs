using CabinReservation.Persistence.Context;
using CabinReservation.Persistence;
using CabinReservation.Api.Endpoints;
using CabinReservation.Api.Middleware;
using CabinReservation.Api.Options;
using CabinReservation.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<ReservationPolicyOptions>()
    .Bind(builder.Configuration.GetSection(ReservationPolicyOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddDbContext<CabinDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("CabinDatabase")));

builder.Services.AddScoped<ISystemClock, SystemClock>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddScoped<IWaitListService, WaitListService>();
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRosterService, RosterService>();
builder.Services.AddScoped<IAuditWriter, AuditWriter>();

builder.Services.AddHostedService<WaitListOfferWorker>();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cabin Reservation API",
        Version = "v1",
        Description = "Authoritative reservation service for one club cabin."
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = ApiKeyMiddleware.HeaderName,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = $"Supply the configured API key in the {ApiKeyMiddleware.HeaderName} header."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "ApiKey"
            }
        }] = Array.Empty<string>()
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiKeyMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(options => {
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cabin Reservation API v1");
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    utc = DateTimeOffset.UtcNow
})).AllowAnonymous();

app.MapMemberEndpoints();
app.MapReservationEndpoints();
app.MapWaitListEndpoints();
app.MapCalendarEndpoints();
app.MapRosterEndpoints();
app.MapAuditEndpoints();
app.MapOutboxEndpoints();

await DatabaseInitializer.InitializeAsync(app.Services);

app.Run();

public partial class Program;
