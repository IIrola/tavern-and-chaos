using System;
namespace TavernChaos
{
    public static class ArmRuleChecks
    {
        public static void Run(Action<bool,string> check)
        {
            var r=new ArmRules(0);
            check(r.Start==3&&r.End==18&&r.Phase==ArmPhase.Countdown,"Arm: three-second preparation precedes exactly fifteen seconds of duel");
            check(!r.Tap(0,1)&&!r.Hold(2,true,1)&&!r.SetEffort(0,3,1),"Arm: invalid side, effort and wrong input mode do not act");
            r.SetMode(0,ArmMode.Rhythm,1);r.SetMode(1,ArmMode.Rhythm,1);
            check(!r.Tap(0,2.9)&&r.Position==0,"Arm: countdown input cannot apply force");
            check(r.Tap(0,3.225)&&!r.Tap(0,3.225),"Arm: early edge accepted exactly once");
            check(r.Tap(1,3.525),"Arm: late edge remains inclusive");
            r.Tick(3.526);check(r.Position==0&&r.Pulses[0]==1&&r.Pulses[1]==1&&r.Stamina[0]>87.9,"Arm: both simultaneous sides resolve before testing a pin");
            r=new ArmRules(0);r.SetMode(0,ArmMode.Rhythm,0);
            check(r.Tap(0,3.05)&&r.Feedback[0]==ArmFeedback.Miss&&r.Stamina[0]==96&&!r.Tap(0,3.1),"Arm: bad timing costs resistance with a spam limit");
            check(!r.Tap(0,double.NaN)&&!r.SetEffort(0,-1,3.1)&&!r.SetMode(0,(ArmMode)12,3.1),"Arm: nonfinite time and invalid options are rejected");
            r=new ArmRules(0);r.SetMode(0,ArmMode.Rhythm,0);r.SetEffort(0,2,0);r.Tap(0,3.375);r.SetEffort(0,0,3.4);r.Tick(3.526);
            check(r.Position==14,"Arm: changing force cannot rewrite an already admitted impulse");
            r=new ArmRules(0);r.Hold(0,true,3.25);r.SetAvailable(0,false,3.4);r.Tick(3.526);
            check(r.Position==9,"Arm: leaving a seat does not erase a previously admitted impulse");
            r.Tick(4.3);check(r.Pulses[0]==1,"Arm: absent participant cannot generate a new impulse");
            r=new ArmRules(0);r.Hold(0,true,0);r.Tick(4);check(r.Position==0,"Arm: expired input heartbeat cannot keep wrestling");
            r=new ArmRules(0);r.SetMode(0,ArmMode.Rhythm,0);r.SetDrunk(0,true,0);r.Tap(0,3.375);r.Tick(3.526);
            check(Math.Abs(r.Stamina[0]-76)<.01&&r.Position==9,"Arm: drunkenness doubles resistance cost without increasing strength");
            double tired=r.Stamina[0];r.Tick(4);check(r.Stamina[0]>tired&&r.Stamina[0]<=100,"Arm: resistance recovers between effective inputs");
            var a=new ArmRules(0);var b=new ArmRules(0);b.SetMode(0,ArmMode.Rhythm,0);
            for(int n=0;n<7;n++) {double t=3+.75*(n+.5);a.Hold(0,true,t);b.Tap(0,t);a.Tick(t+.151);b.Tick(t+.151);}
            check(a.Position==b.Position&&Math.Abs(a.Stamina[0]-b.Stamina[0])<.00001,"Arm: holding and rhythm have equal force, cost and recovery");
            r=new ArmRules(0);
            for(int n=0;n<20;n++) {double t=3+.75*(n+.5);r.Hold(0,true,t);r.Hold(1,true,t);r.Tick(t+.151);}
            r.Tick(18);check(r.Phase==ArmPhase.Result&&r.Position==0&&r.Winner==-1,"Arm: equal play ends in an actual draw after fifteen seconds");
            r=new ArmRules(0);for(int n=0;n<20&&r.Phase!=ArmPhase.Result;n++){double t=3+.75*(n+.5);r.Hold(0,true,t);r.Tick(t+.151);}
            check(r.Phase==ArmPhase.Result&&r.Position==100&&r.Winner==0,"Arm: reaching the endpoint wins before the time limit");
            check(!r.Hold(1,true,18)&&!r.SetEffort(0,0,18),"Arm: finished outcome cannot be changed by later input");
            r=new ArmRules(0);check(!r.SetMode(0,ArmMode.Rhythm,19)&&r.Mode[0]==ArmMode.Hold,"Arm: the first request after the deadline cannot change the input mode");
            r=new ArmRules(0);check(!r.SetEffort(0,2,19)&&r.Effort[0]==1,"Arm: the first request after the deadline cannot change the effort");
            r=new ArmRules(0);r.Hold(0,true,3.375);r.BeginClosing(3.4);
            check(r.Closing&&r.HasPending&&r.Phase==ArmPhase.Playing&&!r.Hold(1,true,3.4),"Arm: closing stops new input while preserving admitted impulse");
            r.Tick(3.526);check(r.Phase==ArmPhase.Cancelled&&r.Position==9&&r.Winner==-1,"Arm: unfinished closure resolves its impulse then cancels without invented winner");
            r.Forfeit(0,3.6);check(r.Phase==ArmPhase.Cancelled,"Arm: disconnection cannot convert a cancelled duel into a paid win");
            r=new ArmRules(0);r.Forfeit(0,1);check(r.Phase==ArmPhase.Result&&r.Winner==1,"Arm: disconnection before result forfeits to remaining side");
            r=new ArmRules(0);r.SetMode(0,ArmMode.Rhythm,0);r.Tap(0,3.375);r.Tick(3.526);r.Tick(17.99);r.BeginClosing(17.999);r.Tick(18.01);
            check(r.Phase==ArmPhase.Cancelled&&r.Winner==-1,"Arm: a room deadline before the duel endpoint cancels instead of awarding a late timer win");
            var drink=new IntoxicationState();drink.Add(1,0);drink.Add(9,1);
            check(drink.Level==5&&!drink.ShouldTrip(15.9,true),"Bar: drunkenness clamps at five and cannot trip within fifteen seconds");
            check(drink.ShouldTrip(16,true)&&!drink.ShouldTrip(16.1,true)&&!drink.ShouldTrip(31,false),"Bar: sober movement and a fifteen-second minimum gate trips");
            drink.Tick(31);check(drink.Level==4&&!drink.ShouldTrip(31,true),"Bar: sobriety recovers one independent level at thirty seconds");
            drink.Tick(151);check(drink.Level==0,"Bar: elapsed time can recover all remaining levels");
            drink.Add(3,152);drink.Clear(153);check(drink.Level==0,"Bar: another room clears the intoxication modifier");
        }
    }
}
