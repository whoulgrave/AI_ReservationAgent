namespace CabinReservation.Voice;

public interface ICallSessionStore 
{ 
    CallSession GetOrCreate(string id, string caller); 
    void Put(CallSession session); 
    bool TryGet(string id, out CallSession? session); 
}
