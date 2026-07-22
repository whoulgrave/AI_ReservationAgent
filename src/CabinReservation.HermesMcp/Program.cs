using CabinReservation.Integration.Contracts.Api;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCabinApiClient(builder.Configuration);
builder.Services.AddMcpServer().WithHttpTransport().WithToolsFromAssembly();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? ["http://localhost"]).AllowAnyHeader().AllowAnyMethod()));
var app = builder.Build();
app.UseCors();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapMcp("/mcp");
app.Run();
