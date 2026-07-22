namespace CabinReservation.Integration.Contracts.Api;

public sealed record OutboundEnvelope(Guid MessageId, Guid MemberId, string Channel, string MessageType,
    string PayloadJson, string? EmailAddress, string? MobileNumber, string? PhoneNumber);
