namespace CabinReservation.Voice;

public sealed record CallSession
(
    string ContextId, 
    string Caller, 
    string? CallConnectionId, 
    string Step, 
    string? ClubNumber = null
);
