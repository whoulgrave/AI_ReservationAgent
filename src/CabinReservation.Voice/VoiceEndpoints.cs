using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.Extensions.Options;

namespace CabinReservation.Voice;

public static class VoiceEndpoints
{
    public static async Task<IResult> IncomingAsync(HttpRequest request, CallAutomationClient client, ICallSessionStore sessions,
        IOptions<VoiceOptions> options, CancellationToken ct)
    {
        var events = EventGridEvent.ParseMany(await BinaryData.FromStreamAsync(request.Body, ct));
        foreach (var evt in events)
        {
            if (evt.TryGetSystemEventData(out object? sd) && sd is SubscriptionValidationEventData validation)
                return Results.Ok(new SubscriptionValidationResponse { ValidationResponse = validation.ValidationCode });
            if (!string.Equals(evt.EventType, "Microsoft.Communication.IncomingCall", StringComparison.OrdinalIgnoreCase)) continue;
            var data = evt.Data.ToObjectFromJson<AcsIncomingCallEventData>()!;
            var caller = data.FromCommunicationIdentifier.RawId;
            var contextId = Guid.NewGuid().ToString("N");
            sessions.GetOrCreate(contextId, caller);
            var callback = new Uri(options.Value.PublicBaseUri, $"webhooks/acs/calls/events/{contextId}");
            await client.AnswerCallAsync(data.IncomingCallContext, callback, cancellationToken: ct);
        }
        return Results.Ok();
    }

    public static async Task<IResult> EventsAsync(string contextId, HttpRequest request, CallAutomationClient client,
        ICallSessionStore sessions, IOptions<VoiceOptions> options, CancellationToken ct)
    {
        var events = CallAutomationEventParser.ParseMany(await BinaryData.FromStreamAsync(request.Body, ct));
        foreach (var evt in events)
        {
            if (evt is CallConnected connected)
            {
                if (!sessions.TryGet(contextId, out var session) || session is null) continue;
                session = session with { CallConnectionId = connected.CallConnectionId, Step = "CollectMemberNumber" };
                sessions.Put(session);
                var connection = client.GetCallConnection(connected.CallConnectionId);
                await connection.GetCallMedia().StartRecognizingAsync(new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(session.Caller), 6)
                {
                    Prompt = new TextSource("Welcome to the cabin reservation service. Enter your club member number, followed by pound.") { VoiceName = options.Value.VoiceName },
                    InterruptPrompt = true,
                    StopTones = new List<DtmfTone> { DtmfTone.Pound },
                    OperationContext = "member-number"
                }, ct);
            }
            else if (evt is RecognizeCompleted recognized && sessions.TryGet(contextId, out var session) && session?.CallConnectionId is not null)
            {
                var connection = client.GetCallConnection(session.CallConnectionId);
                var tones = recognized.RecognizeResult is DtmfResult dtmf ? string.Concat(dtmf.Tones.Select(ToDigit)) : "";
                sessions.Put(session with { ClubNumber = tones, Step = "MainMenu" });
                await connection.GetCallMedia().StartRecognizingAsync(new CallMediaRecognizeDtmfOptions(new PhoneNumberIdentifier(session.Caller), 1)
                {
                    Prompt = new TextSource("Press 1 to reserve, 2 to cancel, 3 to hear your reservations, or 4 for availability.") { VoiceName = options.Value.VoiceName },
                    OperationContext = "main-menu"
                }, ct);
            }
            else if (evt is RecognizeFailed failed && sessions.TryGet(contextId, out var failedSession) && failedSession?.CallConnectionId is not null)
            {
                await client.GetCallConnection(failedSession.CallConnectionId).GetCallMedia().PlayToAllAsync(
                    new TextSource("I could not understand that entry. Please call again.") { VoiceName = options.Value.VoiceName }, cancellationToken: ct);
            }
        }
        return Results.Ok();
    }

    private static char ToDigit(DtmfTone tone)
    {
        // Use .Equals for value comparison since DtmfTone is not a constant type
        if (tone.Equals(DtmfTone.Zero)) return '0';
        if (tone.Equals(DtmfTone.One)) return '1';
        if (tone.Equals(DtmfTone.Two)) return '2';
        if (tone.Equals(DtmfTone.Three)) return '3';
        if (tone.Equals(DtmfTone.Four)) return '4';
        if (tone.Equals(DtmfTone.Five)) return '5';
        if (tone.Equals(DtmfTone.Six)) return '6';
        if (tone.Equals(DtmfTone.Seven)) return '7';
        if (tone.Equals(DtmfTone.Eight)) return '8';
        if (tone.Equals(DtmfTone.Nine)) return '9';
        return ' ';
    }
}
