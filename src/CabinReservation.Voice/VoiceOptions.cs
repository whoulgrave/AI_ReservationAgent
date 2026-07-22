namespace CabinReservation.Voice;

public sealed class VoiceOptions
{
    public string AcsConnectionString { get; init; } = "";
    public string AcsPhoneNumber { get; init; } = "";
    public Uri PublicBaseUri { get; init; } = new("https://example.invalid/");
    public string VoiceName { get; init; } = "en-US-AvaMultilingualNeural";
}
