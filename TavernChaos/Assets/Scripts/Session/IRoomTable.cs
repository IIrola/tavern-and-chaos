namespace TavernChaos
{
    // Host coordinator queries every game before it can settle and close a room.
    public interface IRoomTable : System.IDisposable
    {
        TableSession Table {get;}
        void Leave(ulong id,double now);
        void Disconnect(ulong id,double now);
        void Tick(double now);
        bool FinishSession(double now);
    }
}
