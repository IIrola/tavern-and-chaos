using System;
namespace TavernChaos
{
    public enum ArmMode { Hold, Rhythm }
    public enum ArmPhase { Countdown, Playing, Result, Cancelled }
    public enum ArmFeedback { None, Hit, Miss, Tired }
    // Pure authority simulation. A simultaneous beat is one transaction for both participants.
    public sealed class ArmRules
    {
        public const double Countdown=3,Duration=15,Period=.75,Window=.15,Heartbeat=.3;
        private static readonly int[] Force={6,9,14},Cost={7,12,19};
        public static int ForceFor(int effort)=>Force[Math.Clamp(effort,0,2)];
        public static int CostFor(int effort,bool drunk)=>Cost[Math.Clamp(effort,0,2)]*(drunk?2:1);
        public double Start { get; }
        public double End=>Start+Duration;
        public ArmPhase Phase { get; private set; }=ArmPhase.Countdown;
        public int Position { get; private set; }
        public int Winner { get; private set; }=-1;
        public int Beat { get; private set; }
        public bool Closing { get; private set; }
        public readonly double[] Stamina={100,100};
        public readonly ArmMode[] Mode={ArmMode.Hold,ArmMode.Hold};
        public readonly int[] Effort={1,1},Pulses={0,0};
        public readonly ArmFeedback[] Feedback={ArmFeedback.None,ArmFeedback.None};
        public readonly double[] FeedbackTime={0,0};
        public readonly bool[] Drunk={false,false},Available={true,true};
        private readonly bool[] held={false,false},pending={false,false};
        private readonly int[] pendingEffort={1,1};
        private readonly double[] heldStart={0,0},heldUntil={0,0},activeUntil={0,0},lastMiss={-10,-10};
        private double time;
        public double BeatTime=>Start+Period*(Beat+.5);
        public ArmRules(double now)
        {
            if(!double.IsFinite(now)) throw new ArgumentOutOfRangeException(nameof(now));
            time=now;Start=now+Countdown;
        }
        public bool HasPending=>pending[0]||pending[1];
        private bool Valid(int side,double now)=>side>=0&&side<2&&double.IsFinite(now)&&now>=time&&Phase!=ArmPhase.Result&&Phase!=ArmPhase.Cancelled&&!Closing;
        public bool SetMode(int side,ArmMode mode,double now)
        {
            if(!Valid(side,now)||mode<ArmMode.Hold||mode>ArmMode.Rhythm)return false;
            Tick(now);if(!Valid(side,now))return false;Mode[side]=mode;held[side]=false;return true;
        }
        public bool SetEffort(int side,int effort,double now)
        {
            if(!Valid(side,now)||effort<0||effort>2)return false;
            Tick(now);if(!Valid(side,now))return false;Effort[side]=effort;return true;
        }
        public void SetAvailable(int side,bool value,double now)
        {
            if(side<0||side>1)return;Tick(now);Available[side]=value;if(!value)held[side]=false;
        }
        public void SetDrunk(int side,bool value,double now) { if(side<0||side>1)return;Tick(now);Drunk[side]=value; }
        public bool Hold(int side,bool down,double now)
        {
            if(!Valid(side,now)||Mode[side]!=ArmMode.Hold||!Available[side])return false;
            Tick(now);if(Phase==ArmPhase.Result)return false;
            if(down) {if(!held[side]||now>heldUntil[side])heldStart[side]=now;heldUntil[side]=now+Heartbeat;held[side]=true;}
            else held[side]=false;
            Observe(now);return true;
        }
        public bool Tap(int side,double now)
        {
            if(!Valid(side,now)||Mode[side]!=ArmMode.Rhythm||!Available[side])return false;
            Tick(now);if(Phase!=ArmPhase.Playing)return false;
            if(Math.Abs(now-BeatTime)<=Window+1e-8) {
                if(pending[side])return false;pending[side]=true;pendingEffort[side]=Effort[side];return true;
            }
            if(now-lastMiss[side]<.2)return false;
            lastMiss[side]=now;Stamina[side]=Math.Max(0,Stamina[side]-4*(Drunk[side]?2:1));activeUntil[side]=now+.12;Feedback[side]=ArmFeedback.Miss;FeedbackTime[side]=now;Pulses[side]++;return true;
        }
        public void Tick(double now)
        {
            if(!double.IsFinite(now)||now<time||Phase==ArmPhase.Result||Phase==ArmPhase.Cancelled)return;
            if(now>=Start)Phase=ArmPhase.Playing;
            while(Beat<20&&BeatTime+Window<Math.Min(now,End)-1e-8) {
                double at=BeatTime+Window;Observe(at);Recover(at);
                int left=Resolve(0,at),right=Resolve(1,at);Position=Math.Clamp(Position+left-right,-100,100);Beat++;
                pending[0]=pending[1]=false;
                if(Math.Abs(Position)>=100) {Winner=Position>0?0:1;Phase=ArmPhase.Result;time=at;return;}
                if(Closing)break;
            }
            if(Closing) {if(!HasPending)Phase=ArmPhase.Cancelled;Recover(now);return;}
            Observe(Math.Min(now,End));Recover(Math.Min(now,End));
            if(now>=End) {Winner=Position==0?-1:Position>0?0:1;Phase=ArmPhase.Result;}
        }
        private void Observe(double now)
        {
            if(Closing||Beat>=20)return;
            for(int side=0;side<2;side++) if(!pending[side]&&Mode[side]==ArmMode.Hold&&Available[side]&&held[side]
                &&Math.Max(heldStart[side],BeatTime-Window)<=Math.Min(now,Math.Min(heldUntil[side],BeatTime+Window))+1e-8) {
                pending[side]=true;pendingEffort[side]=Effort[side];
            }
        }
        private int Resolve(int side,double now)
        {
            if(!pending[side])return 0;
            int cost=CostFor(pendingEffort[side],Drunk[side]);activeUntil[side]=now+.12;Pulses[side]++;FeedbackTime[side]=now;
            if(Stamina[side]+1e-7<cost) {Feedback[side]=ArmFeedback.Tired;return 0;}
            Stamina[side]=Math.Max(0,Stamina[side]-cost);Feedback[side]=ArmFeedback.Hit;return Force[pendingEffort[side]];
        }
        private void Recover(double now)
        {
            for(int side=0;side<2;side++)Stamina[side]=Math.Min(100,Stamina[side]+Math.Max(0,now-Math.Max(time,activeUntil[side]))*12);
            time=Math.Max(time,now);
        }
        public void BeginClosing(double now)
        {
            if(Closing||Phase==ArmPhase.Result||Phase==ArmPhase.Cancelled)return;
            Tick(now);Closing=true;held[0]=held[1]=false;if(!HasPending)Phase=ArmPhase.Cancelled;
        }
        public void Forfeit(int side,double now)
        {
            if(side<0||side>1||Phase==ArmPhase.Result||Phase==ArmPhase.Cancelled)return;
            Tick(now);if(Phase==ArmPhase.Result||Phase==ArmPhase.Cancelled)return;Winner=1-side;Phase=ArmPhase.Result;
        }
    }
}
