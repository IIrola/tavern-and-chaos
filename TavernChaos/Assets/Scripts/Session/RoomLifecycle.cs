using System;
namespace TavernChaos
{
    public enum RoomPhase { Open, Draining, Ejecting, Results }
    public enum RoomEndReason { None, Time, Insolvency }
    // Pure host clock. Settlement is performed by the game before reporting it drained.
    public sealed class RoomLifecycle
    {
        public const double EjectionSeconds=6;
        public double Deadline { get; private set; }
        public double PhaseStarted { get; private set; }
        public RoomPhase Phase { get; private set; }
        public RoomEndReason Reason { get; private set; }
        public RoomLifecycle(double deadline) { if(!double.IsFinite(deadline)) throw new ArgumentOutOfRangeException(nameof(deadline)); Deadline=deadline; }
        public static bool ValidMinutes(int minutes)=>minutes==20||minutes==30||minutes==45||minutes==60;
        public bool Accepting(double now)=>Phase==RoomPhase.Open&&now<Deadline;
        internal void Expire(double now) { Deadline=Math.Min(Deadline,now); }
        public void Tick(double now,bool activeRound,bool pendingPayments,bool pendingPhysics,bool canAfford)
        {
            if(Phase==RoomPhase.Results) return;
            if(Phase==RoomPhase.Open) {
                if(now>=Deadline) Change(RoomPhase.Draining,RoomEndReason.Time,now);
                else if(!activeRound&&!pendingPayments&&!pendingPhysics&&!canAfford) Change(RoomPhase.Ejecting,RoomEndReason.Insolvency,now);
            }
            if(Phase==RoomPhase.Draining&&!activeRound&&!pendingPayments&&!pendingPhysics) Change(RoomPhase.Results,Reason,now);
            if(Phase==RoomPhase.Ejecting&&now-PhaseStarted>=EjectionSeconds) Change(RoomPhase.Results,Reason,now);
        }
        private void Change(RoomPhase phase,RoomEndReason reason,double now) { Phase=phase; Reason=reason; PhaseStarted=now; }
    }
}
