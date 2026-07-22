using CabinReservation.Integration.Contracts.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CabinReservation.Admin.Pages;
[Authorize(Policy="Reports")]
public sealed class IndexModel(ICabinApiClient api) : PageModel
{
    [BindProperty(SupportsGet=true)] public DateOnly From { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    [BindProperty(SupportsGet=true)] public DateOnly To { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(1));
    public IReadOnlyList<CalendarDayDto> Days { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) => Days = await api.GetCalendarAsync(From, To, ct);
}
