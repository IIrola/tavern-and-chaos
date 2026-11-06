using System;
namespace TavernChaos
{
    public static class RoomLifecycleChecks
    {
        public static void Run(Action<bool,string> check)
        {
            check(RoomLifecycle.ValidMinutes(20)&&RoomLifecycle.ValidMinutes(30)&&RoomLifecycle.ValidMinutes(45)&&RoomLifecycle.ValidMinutes(60)&&!RoomLifecycle.ValidMinutes(21)&&!RoomLifecycle.ValidMinutes(0),"Session: exactly the four GDD durations are admitted");
            var c=new RoomLifecycle(20);
            check(c.Accepting(19.999)&&!c.Accepting(20),"Session: deadline rejects new intents before the next clock transition");
            c.Tick(19,true,true,true,false); check(c.Phase==RoomPhase.Open,"Session: an active round protects zero available balances");
            c.Tick(20,true,true,true,false); check(c.Phase==RoomPhase.Draining&&c.Reason==RoomEndReason.Time,"Session: deadline enters draining instead of discarding the active round");
            c.Tick(21,false,true,false,false); check(c.Phase==RoomPhase.Draining,"Session: pending payouts block the final report");
            c.Tick(22,false,false,true,false); check(c.Phase==RoomPhase.Draining,"Session: already-released physical action must finish");
            c.Tick(23,false,false,false,false); check(c.Phase==RoomPhase.Results&&c.Reason==RoomEndReason.Time,"Session: drained time closure shows results without an insolvency cinematic");
            c.Tick(50,true,true,true,true); check(c.Phase==RoomPhase.Results&&!c.Accepting(50),"Session: a finished room cannot resume or reopen on a late balance change");
            c=new RoomLifecycle(100); c.Tick(1,false,false,false,true);
            check(c.Phase==RoomPhase.Open,"Session: a single connected person able to pay prevents collective eviction");
            c.Tick(2,true,false,false,false); check(c.Phase==RoomPhase.Open,"Session: zero-bet active rounds still prevent eviction");
            c.Tick(3,false,true,false,false); check(c.Phase==RoomPhase.Open,"Session: pending return protects a temporarily empty wallet");
            c.Tick(4,false,false,true,false); check(c.Phase==RoomPhase.Open,"Session: a released bottle or attack prevents premature eviction");
            c.Tick(5,false,false,false,false); check(c.Phase==RoomPhase.Ejecting&&c.Reason==RoomEndReason.Insolvency&&!c.Accepting(5),"Session: insolvency starts one shared six-second timeline after all pending work");
            c.Tick(10.999,false,false,false,false); check(c.Phase==RoomPhase.Ejecting,"Session: cinematic cannot finish before its server deadline");
            c.Tick(11,false,false,false,false); check(c.Phase==RoomPhase.Results,"Session: cinematic finishes exactly at six server seconds");
            c=new RoomLifecycle(10); c.Tick(10,false,false,false,false); check(c.Reason==RoomEndReason.Time&&c.Phase==RoomPhase.Results,"Session: time has explicit priority when both end conditions coincide");
            c=new RoomLifecycle(100); c.Expire(3); check(!c.Accepting(3)&&c.Deadline==3,"Session: opt-in accelerated test uses the same deadline gate");
            var l=new SessionLedger(); l.Join(0,"A","a"); l.Join(1,"B","b");
            l.ReserveOpening("r","pool",0,new ulong[]{0,1},10,50);l.Activate("r");
            l.Spend("empty0",0,l.Balance(0),"service");l.Spend("empty1",1,l.Balance(1),"service");
            c=new RoomLifecycle(100);c.Tick(1,true,l.HasPending,false,false);
            l.SettleMatch("r",new ulong[]{1});c.Tick(2,false,l.HasPending,false,l.Balance(1)>=5);
            check(c.Phase==RoomPhase.Open&&l.Balance(1)==100&&l.AuditDelta==0,"Session: a pending winner replenishes the room before insolvency evaluation");
            l.Spend("emptyWinner",1,100,"service");c.Tick(3,false,l.HasPending,false,false);
            check(c.Phase==RoomPhase.Ejecting&&l.AuditDelta==0,"Session: only settled, genuinely unaffordable wallets trigger eviction");
            l=new SessionLedger();l.Join(0,"A","a");l.Join(1,"B","b");
            l.ReserveOpening("r","pool",0,new ulong[]{0,1},10,50);l.Activate("r");l.CancelMatch("r");l.Close();
            check(l.Closed&&!l.HasPending&&l.Destroyed==1990&&l.BarRevenue==10&&l.Find(0).Wins==0&&l.AuditDelta==0,"Session: unfinished round refunds stakes before clearing balances, without inventing a win");
        }
    }
}
