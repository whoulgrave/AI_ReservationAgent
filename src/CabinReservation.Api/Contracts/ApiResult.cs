namespace CabinReservation.Api.Contracts;

public sealed record ApiResult<T>
(
    bool Success,
    string Code,
    string Message,
    T? Data = default
);
