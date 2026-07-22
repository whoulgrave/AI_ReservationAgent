using CabinReservation.Persistence.Enums;
using System.ComponentModel.DataAnnotations;

namespace CabinReservation.Api.Contracts;

public sealed record UpdatePreferenceRequest
(
    CommunicationChannel PreferredChannel,
    bool Verified,
    [property: Required] string SourceChannel
);
