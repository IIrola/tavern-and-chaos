using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // Host-only coordinator: seats, payments and rules surround the shared physical table.
    public sealed class PoolRoomController : IDisposable
    {
        public TableSession Table { get; }=new("pool-1",2);
        public EightBallRules Rules { get; private set; }
        public string Notice { get; private set; }="Bola 8 · dos asientos. Abrir cuesta 10 al organizador. Sin apuestas.";
        public int CallBall { get; private set; }
        public int CallPocket { get; private set; }=-1;
        private readonly PoolPractice pool;
        private readonly RoomRoster roster;
        private readonly Func<ulong,TavernActor> actor;
        private readonly Action<ulong,bool,int> changeSeat;
        private readonly HashSet<ulong> departed=new();
        private readonly System.Random random=new();
        private ulong[] sides;
        private double now;
        internal int? FirstSideForCheck;
        public PoolRoomController(PoolPractice pool,RoomRoster roster,Func<ulong,TavernActor> actor,Action<ulong,bool,int> changeSeat)
        {
            this.pool=pool; this.roster=roster; this.actor=actor; this.changeSeat=changeSeat; pool.ShotSettled+=Resolve;
        }
        public static Vector3 SeatPosition(int seat)=>new(seat==0?3.65f:8.35f,.08f,.4f);
        public static bool Near(Vector3 position)=>Mathf.Min(Vector3.Distance(position,SeatPosition(0)),Vector3.Distance(position,SeatPosition(1)))<1.9f;
        public bool Sit(ulong id,bool eligible,double time)
        {
            now=time; var a=actor(id); bool wasSeated=Table.SeatOf(id)>=0;
            bool canSit=eligible&&a&&!a.Fallen&&Near(a.transform.position);
            int preferred=-1;
            bool joining=Table.Phase!=TablePhase.Playing&&Table.Phase!=TablePhase.Result||Table.Participates(id);
            if(canSit&&!wasSeated&&joining) {
                float nearest=1.9f;
                for(int s=0;s<2;s++) if(!Table.Occupant(s).HasValue) {
                    float distance=Vector3.Distance(a.transform.position,SeatPosition(s));
                    if(distance<nearest&&TavernActor.TryFindStandingPosition(SeatPosition(s),a,out var free)&&Vector3.Distance(free,SeatPosition(s))<.05f) { preferred=s; nearest=distance; }
                }
                canSit=preferred>=0;
            }
            var reply=Table.Sit(id,canSit,now,preferred);
            if(reply==SeatReply.Seated&&!wasSeated) changeSeat(id,true,Table.SeatOf(id));
            if(reply==SeatReply.Waiting) Notice="Apuntado a la espera. No se entra a una partida ya iniciada.";
            if(reply==SeatReply.Rejected) Notice="Acércate a un asiento libre y despeja su espacio. La lista de espera conserva su orden.";
            return reply!=SeatReply.Rejected;
        }
        public void Leave(ulong id,double time)
        {
            now=time; if(Table.LeaveSeat(id,now)) changeSeat(id,false,-1);
        }
        public void Disconnect(ulong id,double time)
        {
            Leave(id,time); Table.Remove(id,time); if(Table.Participates(id)) departed.Add(id);
        }
        private int Side(ulong id)=>sides==null?-1:Array.IndexOf(sides,id);
        public bool Handle(ulong id,LanIntent intent,double time)
        {
            now=time;
            if(intent.action==LanAction.PoolLeave) { Leave(id,now); return true; }
            if(intent.action==LanAction.PoolClose) {
                if(Table.Phase!=TablePhase.Result||Table.Organizer!=id) return false;
                Table.Close(); Rules=null; sides=null; CallBall=0; CallPocket=-1; Notice="Nueva ronda: dos personas sentadas, apertura de 10 fichas."; return true;
            }
            if(intent.action==LanAction.PoolStart) {
                if(!Table.CanStart(id,2)) return false;
                for(int s=0;s<2;s++) if(!Table.Occupant(s).HasValue||!actor(Table.Occupant(s).Value)||actor(Table.Occupant(s).Value).Fallen) return false;
                if(!roster.Spend(id,10)) { Notice="El organizador necesita 10 fichas. No se ha cobrado ni iniciado la partida."; return false; }
                // All preconditions were validated in one host tick; no client code runs between debit and start.
                Table.Start(id,2); sides=new[]{Table.Participants[0],Table.Participants[1]};
                int first=DemoGame.Instance.SmokeMode&&FirstSideForCheck.HasValue?Mathf.Clamp(FirstSideForCheck.Value,0,1):random.Next(2); FirstSideForCheck=null;
                Rules=new EightBallRules(first); winnerOverride=null; departed.Clear(); CallBall=0; CallPocket=-1;
                pool.BeginMatch(Rules.Rack(random)); Sync(); return true;
            }
            int side=Side(id);
            if(side<0||Rules==null||Table.Phase!=TablePhase.Playing||Table.SeatOf(id)<0||!actor(id)||actor(id).Fallen||Rules.Turn!=side) return false;
            switch(intent.action) {
                case LanAction.PoolCall:
                    if(Rules.Phase!=EightBallPhase.Aiming||Rules.Break||!Rules.CanCall(side,intent.ball,intent.pocket)) return false;
                    CallBall=intent.ball; CallPocket=intent.pocket; return true;
                case LanAction.PoolPlace:
                    if(Rules.Phase!=EightBallPhase.Placement||!pool.PlaceMatch(intent.move)) return false;
                    Rules.Place(side); Sync(); return true;
                case LanAction.PoolShoot:
                    if(!float.IsFinite(intent.power)||intent.power<.05f||intent.power>1||!Finite(intent.move)||Mathf.Abs(intent.move.y)>.001f||intent.move.sqrMagnitude<.001f||intent.move.sqrMagnitude>1.01f) return false;
                    if(Rules.Phase!=EightBallPhase.Aiming||pool.Moving||!Rules.Break&&(intent.ball!=CallBall||intent.pocket!=CallPocket)) return false;
                    if(!Rules.Shoot(side,intent.ball,intent.pocket)) return false;
                    pool.MatchPhase(EightBallPhase.Aiming,Rules.HeadOnly,Rules.Notice);
                    pool.Strike(intent.power,intent.move); Notice=Rules.Notice; return true;
                case LanAction.PoolAccept:
                case LanAction.PoolRerack:
                    bool rerack=intent.action==LanAction.PoolRerack;
                    if(!Rules.DecideBreak(side,rerack)) return false;
                    if(rerack) pool.BeginMatch(Rules.Rack(random));
                    CallBall=0; CallPocket=-1; Sync(); return true;
            }
            return false;
        }
        private void Resolve(EightBallEvents events)
        {
            if(Rules==null) return; bool wasBreak=Rules.Break;
            Rules.Resolve(events);
            if(Rules.RerackRequired) pool.BeginMatch(Rules.Rack(random));
            if(wasBreak&&!Rules.RerackRequired&&(events.Pockets.ContainsKey(8)||events.OffTable.Contains(8))) pool.RespotMatch(8);
            CallBall=0; CallPocket=-1; Sync();
            ProcessDepartures();
        }
        private void Sync()
        {
            Notice=Rules.Notice; pool.MatchPhase(Rules.Phase,Rules.HeadOnly,Notice);
            if(Rules.Phase==EightBallPhase.Result) Table.Finish(); else Table.SetTurn(sides[Rules.Turn],now);
        }
        public void Tick(double time)
        {
            now=time; ProcessDepartures();
            if(Rules!=null&&Table.Phase==TablePhase.Playing) {
                Table.SetTurn(sides[Rules.Turn],now);
                ulong? expired=Table.ExpiredAbsence(now,pool.Moving);
                if(expired.HasValue) { Rules.Forfeit(Side(expired.Value),"Partida perdida por superar un minuto fuera durante tu turno."); Sync(); }
            }
            if(Table.Phase==TablePhase.Result&&!Table.Organizer.HasValue) { Table.Close(); Rules=null; sides=null; }
        }
        private void ProcessDepartures()
        {
            if(Rules==null||pool.Moving||departed.Count==0) return;
            if(Rules.Phase!=EightBallPhase.Result) foreach(ulong id in departed) { Rules.Forfeit(Side(id),"Partida resuelta por desconexión del rival."); break; }
            // A disconnected side cannot be awarded a result reached while its shot was still resolving.
            if(Rules.Winner>=0&&departed.Contains(sides[Rules.Winner])) {
                int remaining=1-Rules.Winner;
                winnerOverride=departed.Contains(sides[remaining])?-1:remaining;
            }
            Sync(); departed.Clear();
        }
        private int? winnerOverride;
        public PoolRoomView Capture(double time)
        {
            var seated=new List<ulong>(); var indices=new List<int>();
            for(int i=0;i<2;i++) if(Table.Occupant(i).HasValue) { seated.Add(Table.Occupant(i).Value); indices.Add(i); }
            return new PoolRoomView {
                phase=Table.Phase,playPhase=Rules?.Phase??EightBallPhase.Result,notice=Notice,seated=seated.ToArray(),seats=indices.ToArray(),
                participants=sides??Array.Empty<ulong>(),waiting=new List<ulong>(Table.Waiting).ToArray(),
                hasOrganizer=Table.Organizer.HasValue,organizer=Table.Organizer??0,hasTurn=Table.Phase==TablePhase.Playing,turn=Rules!=null?sides[Rules.Turn]:0,
                groups=Rules==null?Array.Empty<int>():new[]{(int)Rules.Groups[0],(int)Rules.Groups[1]},winner=winnerOverride??Rules?.Winner??-1,
                breaking=Rules?.Break??false,headOnly=Rules?.HeadOnly??false,foul=Rules?.Foul??false,callBall=CallBall,callPocket=CallPocket,shots=pool.Shots,
                absenceRemaining=Table.AbsenceRemaining(time),balls=pool.CaptureBalls()
            };
        }
        private static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        public void Dispose() { pool.ShotSettled-=Resolve; pool.EndMatch(); }
    }
}
