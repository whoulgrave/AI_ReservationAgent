using Microsoft.AspNetCore.Authorization; using Microsoft.AspNetCore.Mvc.RazorPages; namespace CabinReservation.Admin.Pages; [Authorize(Policy="Audit")] public sealed class AuditModel : PageModel { }
