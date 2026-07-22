using System.Collections.Concurrent;

namespace CabinReservation.Voice;

public sealed class MemoryCallSessionStore : ICallSessionStore
{
    private readonly ConcurrentDictionary<string, CallSession> _sessions = new();
    public CallSession GetOrCreate(string id, string caller) => _sessions.GetOrAdd(id, _ => new(id, caller, null, "AwaitingConnection"));
    public void Put(CallSession session) => _sessions[session.ContextId] = session;
    public bool TryGet(string id, out CallSession? session) => _sessions.TryGetValue(id, out session);
}
