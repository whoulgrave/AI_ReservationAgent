namespace CabinReservation.Integration.Contracts.Outbox;

public sealed record OutboxLeaseDto(Guid Id, Guid MemberId, int Channel, string MessageType, string PayloadJson,
    string? EmailAddress, string? MobileNumber, string? PhoneNumber, string LeaseToken);
