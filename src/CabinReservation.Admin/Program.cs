using CabinReservation.Integration.Contracts.Api;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme).AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Reports", p => p.RequireRole("Cabin.Reporter", "Cabin.Administrator"));
    o.AddPolicy("Roster", p => p.RequireRole("Cabin.RosterAdministrator", "Cabin.Administrator"));
    o.AddPolicy("Audit", p => p.RequireRole("Cabin.Auditor", "Cabin.Administrator"));
});
builder.Services.AddRazorPages().AddMicrosoftIdentityUI();
builder.Services.AddCabinApiClient(builder.Configuration);
var app = builder.Build();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.Run();
