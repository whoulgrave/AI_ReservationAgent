using Microsoft.Extensions.Options;

namespace CabinReservation.Integration.Contracts.Api;

public sealed record ApiResult<T>(bool Success, string Code, string Message, T? Data);
