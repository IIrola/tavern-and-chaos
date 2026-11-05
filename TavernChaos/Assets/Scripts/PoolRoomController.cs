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
        public string Notice { get; private set; }
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
        internal bool FailOpeningForCheck;
        private readonly HashSet<ulong> ready=new();
        private readonly long openingFee;
        private long stake;
        private uint proposal=1;
        private ulong roundSerial;
        private string roundKey;
        private PoolReceipt receipt;
        public PoolRoomController(PoolPractice pool,RoomRoster roster,Func<ulong,TavernActor> actor,Action<ulong,bool,int> changeSeat,EconomySettings prices)
        {
            this.pool=pool; this.roster=roster; this.actor=actor; this.changeSeat=changeSeat; openingFee=prices.Opening(TavernActivity.Pool); pool.ShotSettled+=Resolve;
            Notice="Bola 8 · apertura "+openingFee+" al organizador. Ambos confirman la apuesta, incluido cero.";
        }
        private void InvalidateSetup() { ready.Clear(); proposal=proposal==uint.MaxValue?1:proposal+1; }
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
            if(reply==SeatReply.Seated&&!wasSeated) { changeSeat(id,true,Table.SeatOf(id)); if(Table.Phase==TablePhase.Preparation) InvalidateSetup(); }
            if(reply==SeatReply.Waiting) Notice="Apuntado a la espera. No se entra a una partida ya iniciada.";
            if(reply==SeatReply.Rejected) Notice="Acércate a un asiento libre y despeja su espacio. La lista de espera conserva su orden.";
            return reply!=SeatReply.Rejected;
        }
        public void Leave(ulong id,double time)
        {
            now=time; bool setup=Table.Phase==TablePhase.Preparation;
            if(Table.LeaveSeat(id,now)) { changeSeat(id,false,-1); if(setup) InvalidateSetup(); }
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
                if(Table.Phase!=TablePhase.Result||Table.Organizer!=id||!roster.Ledger.IsSettled(roundKey)) return false;
                Table.Close(); Rules=null; sides=null; roundKey=null; CallBall=0; CallPocket=-1; InvalidateSetup(); Notice="Nueva ronda: revisa apertura y apuesta y confirma de nuevo."; return true;
            }
            if(intent.action==LanAction.PoolWager) {
                if(Table.Phase!=TablePhase.Preparation||Table.Organizer!=id||intent.proposal!=proposal||intent.wager<0||intent.wager>long.MaxValue/2) return false;
                if(stake!=intent.wager) { stake=intent.wager; InvalidateSetup(); }
                Notice="Apuesta propuesta: "+stake+" por lado. Ambos deben confirmar el desglose."; return true;
            }
            if(intent.action==LanAction.PoolReady) {
                if(Table.Phase!=TablePhase.Preparation||Table.SeatOf(id)<0||intent.proposal!=proposal||!actor(id)||actor(id).Fallen) return false;
                if(ready.Remove(id)) return true;
                if(stake>long.MaxValue-(Table.Organizer==id?openingFee:0)||roster.Ledger.Balance(id)<stake+(Table.Organizer==id?openingFee:0)) { Notice="Saldo insuficiente para confirmar apertura y apuesta. No se ha cobrado nada."; return false; }
                ready.Add(id); Notice="Confirmación recibida. El organizador inicia cuando ambos estén listos."; return true;
            }
            if(intent.action==LanAction.PoolStart) {
                if(!Table.CanStart(id,2)||intent.proposal!=proposal) return false;
                for(int s=0;s<2;s++) if(!Table.Occupant(s).HasValue||!ready.Contains(Table.Occupant(s).Value)||!actor(Table.Occupant(s).Value)||actor(Table.Occupant(s).Value).Fallen) return false;
                var candidates=new[]{Table.Occupant(0).Value,Table.Occupant(1).Value}; string key=Table.Id+"/"+(++roundSerial);
                if(roster.Ledger.ReserveOpening(key,"pool",id,candidates,openingFee,stake)!=LedgerResult.Applied) { Notice="No se pudo reservar el total de ambos lados. No se ha cobrado nada."; return false; }
                bool controlledFailure=DemoGame.Instance.SmokeMode&&FailOpeningForCheck;
                try {
                    if(controlledFailure) { FailOpeningForCheck=false; throw new InvalidOperationException("Controlled opening failure"); }
                    int first=DemoGame.Instance.SmokeMode&&FirstSideForCheck.HasValue?Mathf.Clamp(FirstSideForCheck.Value,0,1):random.Next(2); FirstSideForCheck=null;
                    var rules=new EightBallRules(first); pool.BeginMatch(rules.Rack(random));
                    if(!Table.Start(id,2)) throw new InvalidOperationException("Table preparation changed before start");
                    sides=candidates; Rules=rules; roundKey=key; winnerOverride=null; departed.Clear(); CallBall=0; CallPocket=-1; Sync();
                    if(roster.Ledger.Activate(key)!=LedgerResult.Applied) throw new InvalidOperationException("Opening reservation could not activate");
                    return true;
                } catch(Exception error) {
                    roster.Ledger.AbortOpening(key); Table.AbortStart(); Rules=null; sides=null; roundKey=null; InvalidateSetup();
                    pool.EndMatch(); pool.SetRoomView(Table.SeatOf(0)>=0); Notice="Inicio cancelado: se devolvieron todos los importes reservados.";
                    if(!controlledFailure) Debug.LogException(error); return false;
                }
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
            SettleResult();
            if(Table.Phase==TablePhase.Result&&!Table.Organizer.HasValue&&roster.Ledger.IsSettled(roundKey)) { Table.Close(); Rules=null; sides=null; roundKey=null; InvalidateSetup(); }
        }
        private void ProcessDepartures()
        {
            if(Rules==null||pool.Moving||departed.Count==0) return;
            if(roster.Ledger.IsSettled(roundKey)) { departed.Clear(); return; }
            if(Rules.Phase!=EightBallPhase.Result) foreach(ulong id in departed) { Rules.Forfeit(Side(id),"Partida resuelta por desconexión del rival."); break; }
            // A disconnected side cannot be awarded a result reached while its shot was still resolving.
            if(Rules.Winner>=0&&departed.Contains(sides[Rules.Winner])) {
                int remaining=1-Rules.Winner;
                winnerOverride=departed.Contains(sides[remaining])?-1:remaining;
            }
            Sync(); departed.Clear();
        }
        private void SettleResult()
        {
            if(Rules==null||Rules.Phase!=EightBallPhase.Result||roundKey==null||roster.Ledger.IsSettled(roundKey)) return;
            int winner=winnerOverride??Rules.Winner;
            if(winner>=0&&!roster.Ledger.IsConnected(sides[winner])) winner=roster.Ledger.IsConnected(sides[1-winner])?1-winner:-1;
            winnerOverride=winner;
            var winners=winner>=0?new[]{sides[winner]}:Array.Empty<ulong>();
            var result=roster.Ledger.SettleMatch(roundKey,winners);
            if(result==LedgerResult.Rejected) { Notice="Resultado registrado; liquidación pendiente. No se volverá a cobrar la apertura."; return; }
            Notice=Rules.Notice+(stake>0?" Bote de "+(stake*2)+" liquidado una vez.":" Sin apuesta: la victoria cuenta, sin premio en fichas.");
            receipt=new PoolReceipt{id=roundKey,winnerName=winner>=0?roster.Ledger.Find(sides[winner]).Name:"",hasWinner=winner>=0,players=(ulong[])sides.Clone(),payouts=new[]{roster.Ledger.Payout(roundKey,sides[0]),roster.Ledger.Payout(roundKey,sides[1])},pot=stake*2};
        }
        public void CancelRound()
        {
            if(roundKey!=null&&!roster.Ledger.IsSettled(roundKey)) roster.Ledger.CancelMatch(roundKey);
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
                absenceRemaining=Table.AbsenceRemaining(time),balls=pool.CaptureBalls(),
                proposal=proposal,stake=stake,openingFee=openingFee,pot=roundKey==null?0:stake*2,heldPot=roster.Ledger.Pot(roundKey),ready=new List<ulong>(ready).ToArray(),settled=roster.Ledger.IsSettled(roundKey),payouts=sides==null?Array.Empty<long>():new[]{roster.Ledger.Payout(roundKey,sides[0]),roster.Ledger.Payout(roundKey,sides[1])},
                participantNames=sides==null?Array.Empty<string>():new[]{roster.Ledger.Find(sides[0]).Name,roster.Ledger.Find(sides[1]).Name},receipt=receipt
            };
        }
        private static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        public void Dispose() { CancelRound(); pool.ShotSettled-=Resolve; pool.EndMatch(); }
    }
}
