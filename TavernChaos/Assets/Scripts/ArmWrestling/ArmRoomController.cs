using System;
using System.Collections.Generic;
using UnityEngine;
namespace TavernChaos
{
    public sealed class ArmRoomController : IRoomTable
    {
        public TableSession Table {get;}=new("arm-1",2);
        public ArmRules Rules {get;private set;}
        private readonly RoomRoster roster;
        private readonly Func<ulong,TavernActor> actor;
        private readonly Action<ulong,bool,int> seat;
        private readonly HashSet<ulong> ready=new();
        private readonly long fee;
        private readonly bool[] quotedDrunk={false,false};
        private readonly ArmMode[] modes={ArmMode.Hold,ArmMode.Hold};
        private readonly int[] efforts={1,1};
        private ulong[] sides;
        private uint revision=1;
        private long stake;
        private ulong serial;
        private string round;
        private double now;
        private bool closing;
        private MatchReceipt receipt;
        private int? paidWinner;
        public string Notice {get;private set;}="Dos personas. Apertura 5. Mantener es una alternativa completa al ritmo manual.";
        public static readonly Vector3 Center=new(-2.3f,0,1.8f);
        public static Vector3 SeatPosition(int index)=>Center+new Vector3(index==0?-1.35f:1.35f,.08f,0);
        public static bool Near(Vector3 p)=>Mathf.Min(Vector3.Distance(p,SeatPosition(0)),Vector3.Distance(p,SeatPosition(1)))<1.65f;
        public ArmRoomController(RoomRoster roster,Func<ulong,TavernActor> actor,Action<ulong,bool,int> seat,EconomySettings prices)
        {this.roster=roster;this.actor=actor;this.seat=seat;fee=prices.Opening(TavernActivity.ArmWrestle);}
        private void Invalidate(){ready.Clear();revision=revision==uint.MaxValue?1:revision+1;}
        private void RefreshQuote()
        {
            if(Table.Phase!=TablePhase.Preparation)return;
            for(int i=0;i<2;i++) {
                var id=Table.Occupant(i);bool drunk=id.HasValue&&actor(id.Value)&&actor(id.Value).Ebriety>0;
                if(quotedDrunk[i]!=drunk){quotedDrunk[i]=drunk;Invalidate();Notice="Cambió el coste por ebriedad. Revisad el desglose y confirmad de nuevo.";}
            }
        }
        public bool Sit(ulong id,bool eligible,double time)
        {
            now=time;var a=actor(id);bool old=Table.SeatOf(id)>=0;
            bool can=eligible&&a&&!a.Fallen&&Near(a.transform.position);int preferred=-1;
            if(can&&!old&&(Table.Phase!=TablePhase.Playing&&Table.Phase!=TablePhase.Result||Table.Participates(id))) {
                float best=1.65f;
                for(int i=0;i<2;i++) if(!Table.Occupant(i).HasValue) {
                    float d=Vector3.Distance(a.transform.position,SeatPosition(i));
                    if(d<best&&TavernActor.TryFindStandingPosition(SeatPosition(i),a,out var free)&&Vector3.Distance(free,SeatPosition(i))<.05f){best=d;preferred=i;}
                }
                can=preferred>=0;
            }
            var result=Table.Sit(id,can,now,preferred);
            if(result==SeatReply.Seated&&!old){seat(id,true,Table.SeatOf(id));if(Table.Phase==TablePhase.Preparation){modes[Table.SeatOf(id)]=ArmMode.Hold;efforts[Table.SeatOf(id)]=1;Invalidate();RefreshQuote();}}
            if(result==SeatReply.Waiting)Notice="En espera para el siguiente duelo. No se cobra por esperar.";
            return result!=SeatReply.Rejected;
        }
        public void Leave(ulong id,double time)
        {
            now=time;
            if(Table.LeaveSeat(id,now)){seat(id,false,-1);if(Table.Phase==TablePhase.Preparation)Invalidate();}
            int side=sides==null?-1:Array.IndexOf(sides,id);if(side>=0)Rules?.SetAvailable(side,false,now);
        }
        public void Disconnect(ulong id,double time)
        {
            Leave(id,time);Table.Remove(id,time);
            int side=sides==null?-1:Array.IndexOf(sides,id);if(side>=0&&!roster.Ledger.IsSettled(round))Rules?.Forfeit(side,time);
        }
        public bool Handle(ulong id,LanIntent intent,double time)
        {
            now=time;RefreshQuote();int seated=Table.SeatOf(id);
            if(intent.action==LanAction.ArmLeave){Leave(id,now);return true;}
            if(intent.action==LanAction.ArmClose) {
                if(Table.Phase!=TablePhase.Result||Table.Organizer!=id||!roster.Ledger.IsSettled(round))return false;
                Table.Close();Rules=null;sides=null;round=null;paidWinner=null;Invalidate();return true;
            }
            if(intent.action==LanAction.ArmWager) {
                if(Table.Phase!=TablePhase.Preparation||Table.Organizer!=id||intent.proposal!=revision||intent.wager<0||intent.wager>long.MaxValue/2)return false;
                if(stake!=intent.wager){stake=intent.wager;Invalidate();}return true;
            }
            if(intent.action==LanAction.ArmReady) {
                if(Table.Phase!=TablePhase.Preparation||seated<0||intent.proposal!=revision||!actor(id)||actor(id).Fallen)return false;
                if(ready.Remove(id))return true;
                long cost=Table.Organizer==id?fee:0;
                if(stake>long.MaxValue-cost||roster.Ledger.Balance(id)<stake+cost){Notice="Saldo insuficiente para tu total. No se ha cobrado nada.";return false;}
                ready.Add(id);Notice="Confirmado. El organizador inicia cuando ambos estén listos.";return true;
            }
            if(intent.action==LanAction.ArmStart) {
                if(!Table.CanStart(id,2)||intent.proposal!=revision)return false;
                for(int i=0;i<2;i++)if(!Table.Occupant(i).HasValue||!ready.Contains(Table.Occupant(i).Value)||!actor(Table.Occupant(i).Value)||actor(Table.Occupant(i).Value).Fallen)return false;
                var players=new[]{Table.Occupant(0).Value,Table.Occupant(1).Value};string key=Table.Id+"/"+(++serial);
                if(roster.Ledger.ReserveOpening(key,"arm",id,players,fee,stake)!=LedgerResult.Applied){Notice="No se pudo reservar el total. Nadie ha pagado.";return false;}
                try {
                    var rules=new ArmRules(now);
                    for(int i=0;i<2;i++){rules.SetMode(i,modes[i],now);rules.SetEffort(i,efforts[i],now);rules.SetDrunk(i,actor(players[i]).Ebriety>0,now);}
                    if(!Table.Start(id,2))throw new InvalidOperationException("Arm table changed during reservation");
                    Rules=rules;sides=players;round=key;closing=false;paidWinner=null;
                    if(roster.Ledger.Activate(key)!=LedgerResult.Applied)throw new InvalidOperationException("Arm reservation could not activate");
                    Notice="Preparados. Tres, dos, uno…";return true;
                } catch(Exception error) {roster.Ledger.AbortOpening(key);Table.AbortStart();Rules=null;sides=null;round=null;Invalidate();Debug.LogException(error);return false;}
            }
            if(seated<0||!actor(id)||actor(id).Fallen)return false;
            if(intent.action==LanAction.ArmMode) {
                if(intent.choice<0||intent.choice>1||Table.Phase==TablePhase.Result)return false;
                if(Rules!=null&&!Rules.SetMode(Array.IndexOf(sides,id),(ArmMode)intent.choice,now))return false;
                modes[seated]=(ArmMode)intent.choice;return true;
            }
            if(intent.action==LanAction.ArmEffort) {
                if(intent.choice<0||intent.choice>2||Table.Phase==TablePhase.Result)return false;
                if(Rules!=null&&!Rules.SetEffort(Array.IndexOf(sides,id),intent.choice,now))return false;
                efforts[seated]=intent.choice;return true;
            }
            int side=sides==null?-1:Array.IndexOf(sides,id);
            if(side<0||Rules==null||Table.Phase!=TablePhase.Playing)return false;
            if(intent.action==LanAction.ArmHold)return Rules.Hold(side,intent.run,now);
            if(intent.action==LanAction.ArmTap)return Rules.Tap(side,now);
            return false;
        }
        public void Stop(ulong id,double time) {int side=sides==null?-1:Array.IndexOf(sides,id);if(side>=0)Rules?.Hold(side,false,time);}
        public void Tick(double time)
        {
            now=time;RefreshQuote();
            if(Rules!=null&&Table.Phase==TablePhase.Playing) {
                for(int i=0;i<2;i++){var a=actor(sides[i]);Rules.SetAvailable(i,a&&!a.Fallen&&Table.SeatOf(sides[i])>=0,now);Rules.SetDrunk(i,a&&a.Ebriety>0,now);}
                Rules.Tick(now);if(Rules.Phase==ArmPhase.Result)Table.Finish();
                else if(Rules.Phase==ArmPhase.Playing)Notice="El ritmo continúa. Descansa cuando baje tu resistencia.";
            }
            Settle();
        }
        private void Settle()
        {
            if(Rules==null||Rules.Phase!=ArmPhase.Result||roster.Ledger.IsSettled(round))return;
            int winner=Rules.Winner;
            if(winner>=0&&!roster.Ledger.IsConnected(sides[winner]))winner=roster.Ledger.IsConnected(sides[1-winner])?1-winner:-2;
            bool draw=winner==-1;
            var winners=draw?sides:winner>=0?new[]{sides[winner]}:Array.Empty<ulong>();
            if(roster.Ledger.SettleMatch(round,winners,draw)==LedgerResult.Rejected)return;
            paidWinner=winner;
            Notice=draw?"Empate: cada apuesta devuelta.":winner>=0?"Gana "+roster.Ledger.Find(sides[winner]).Name+". Bote liquidado.":"Sin ganador elegible: ronda liquidada.";
            receipt=new MatchReceipt{id=round,game="Pulseadas",winnerName=winner>=0?roster.Ledger.Find(sides[winner]).Name:"",hasWinner=winner>=0,players=(ulong[])sides.Clone(),payouts=new[]{roster.Ledger.Payout(round,sides[0]),roster.Ledger.Payout(round,sides[1])},pot=stake*2};
        }
        public bool FinishSession(double time)
        {
            now=time;
            if(!closing){closing=true;Rules?.BeginClosing(now);}
            if(Rules!=null&&Rules.Phase!=ArmPhase.Result&&Rules.Phase!=ArmPhase.Cancelled)return false;
            Settle();if(round!=null&&!roster.Ledger.IsSettled(round))roster.Ledger.CancelMatch(round);
            for(int i=0;i<2;i++)if(Table.Occupant(i).HasValue)Leave(Table.Occupant(i).Value,now);
            Table.Close();Rules=null;sides=null;round=null;paidWinner=null;Invalidate();return true;
        }
        public ArmRoomView Capture()
        {
            var occupants=new List<ulong>();var slots=new List<int>();for(int i=0;i<2;i++)if(Table.Occupant(i).HasValue){occupants.Add(Table.Occupant(i).Value);slots.Add(i);}
            var names=new List<string>();if(sides!=null)foreach(ulong id in sides)names.Add(roster.Ledger.Find(id).Name);
            return new ArmRoomView{phase=Table.Phase,gamePhase=Rules?.Phase??ArmPhase.Countdown,seated=occupants.ToArray(),seats=slots.ToArray(),participants=sides??Array.Empty<ulong>(),names=names.ToArray(),ready=new List<ulong>(ready).ToArray(),waiting=new List<ulong>(Table.Waiting).ToArray(),organizer=Table.Organizer??ulong.MaxValue,proposal=revision,openingFee=fee,stake=stake,heldPot=roster.Ledger.Pot(round),pot=stake*2,settled=roster.Ledger.IsSettled(round),payouts=sides==null?new long[]{0,0}:new[]{roster.Ledger.Payout(round,sides[0]),roster.Ledger.Payout(round,sides[1])},winner=paidWinner??Rules?.Winner??-1,position=Rules?.Position??0,start=Rules?.Start??0,end=Rules?.End??0,beat=Rules?.Beat??0,stamina=Rules==null?new[]{100f,100f}:new[]{(float)Rules.Stamina[0],(float)Rules.Stamina[1]},modes=Rules==null?new[]{(int)modes[0],(int)modes[1]}:new[]{(int)Rules.Mode[0],(int)Rules.Mode[1]},efforts=Rules==null?new[]{efforts[0],efforts[1]}:new[]{Rules.Effort[0],Rules.Effort[1]},feedbackTime=Rules==null?new[]{0d,0d}:new[]{Rules.FeedbackTime[0],Rules.FeedbackTime[1]},feedback=Rules==null?new[]{0,0}:new[]{(int)Rules.Feedback[0],(int)Rules.Feedback[1]},pulses=Rules==null?new[]{0,0}:new[]{Rules.Pulses[0],Rules.Pulses[1]},drunk=Rules==null?new[]{quotedDrunk[0],quotedDrunk[1]}:new[]{Rules.Drunk[0],Rules.Drunk[1]},notice=Notice,receipt=receipt};
        }
        public void Dispose(){if(round!=null&&!roster.Ledger.IsSettled(round))roster.Ledger.CancelMatch(round);}
    }
}
