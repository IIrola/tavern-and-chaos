using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public static class EightBallChecks
    {
        private static EightBallEvents Shot(int first,int pot=-1,int pocket=0,bool rail=true)
        {
            var e=new EightBallEvents(); if(first>0) e.Contact(first,10); if(rail) e.Rail(0,11); if(pot>=0) e.Pockets.Add(pot,pocket); return e;
        }
        private static EightBallRules Open()
        {
            var r=new EightBallRules(0); r.Place(0); r.Shoot(0,0,-1); r.Resolve(Shot(1,1)); return r;
        }
        private static EightBallRules Assigned()
        {
            var r=Open(); r.Shoot(0,2,0); r.Resolve(Shot(2,2)); return r;
        }
        public static void Run(Action<bool,string> check)
        {
            var r=new EightBallRules(1);
            check(r.Turn==1&&r.Break&&r.HeadOnly&&r.Phase==EightBallPhase.Placement,"Rules: selected first side starts behind head string");
            check(!r.Place(0),"Rules: other side cannot place cue");
            r.Place(1); check(!r.Shoot(0,0,-1)&&r.Shoot(1,0,-1)&&!r.Shoot(1,0,-1),"Rules: correct side starts shot only once");
            var e=Shot(1); for(int i=1;i<=4;i++) e.Rail(i,11); r.Resolve(e);
            check(r.Turn==0&&r.Phase==EightBallPhase.Aiming&&r.Groups[0]==BallGroup.Open,"Rules: four object rails legalize dry break without assigning groups");
            r=new EightBallRules(0); r.Place(0); r.Shoot(0,0,-1); e=Shot(1); for(int i=0;i<6;i++) e.Rail(1,11+i); r.Resolve(e);
            check(r.Phase==EightBallPhase.BreakDecision&&r.Turn==1,"Rules: repeat contacts by one ball do not count as four object rails");
            check(!r.DecideBreak(0,true)&&r.DecideBreak(1,true)&&r.Break&&r.Turn==0,"Rules: opponent alone chooses repeat with original breaker");
            r.Place(0); r.Shoot(0,0,-1); r.Resolve(Shot(1)); r.DecideBreak(1,false);
            check(r.Phase==EightBallPhase.Aiming&&r.Turn==1&&!r.HeadOnly,"Rules: accept insufficient break without rearranging balls");
            r=Open(); check(r.Turn==0&&r.Groups[0]==BallGroup.Open,"Rules: break pot keeps turn with open table");
            r=new EightBallRules(0); r.Place(0); r.Shoot(0,0,-1); r.Resolve(Shot(1,8));
            check(r.OnTable(8)&&r.Winner==-1&&r.Turn==0&&!r.Break,"Rules: eight on break is re-spotted without win or loss");
            r=new EightBallRules(0); r.Place(0); r.Shoot(0,0,-1); e=Shot(1,8); e.Pockets.Add(0,1); r.Resolve(e);
            check(r.Phase==EightBallPhase.Placement&&r.HeadOnly&&r.Turn==1&&r.OnTable(8),"Rules: eight plus scratch on break grants cue behind line");
            r.Place(1); r.Shoot(1,2,0); e=Shot(2,2); e.Contact(2,10,true); r.Resolve(e);
            check(r.Foul,"Rules: first target behind head string after break foul is illegal");
            r=new EightBallRules(0); r.Place(0); r.Shoot(0,0,-1); e=Shot(1,8); e.Pockets.Add(0,1); r.Resolve(e);
            r.Place(1); r.Shoot(1,2,0); e=Shot(2,2); e.Contact(2,10,true); e.Contact(3,10,false); r.Resolve(e);
            check(!r.Foul,"Rules: simultaneous legal contact beyond head string is not invalidated by callback ordering");
            r=Open(); check(!r.Shoot(0,8,0)&&!r.Shoot(0,2,6),"Rules: no early eight announcement or nonexistent pocket");
            r.Shoot(0,2,0); r.Resolve(Shot(8,2));
            check(r.Foul&&r.Groups[0]==BallGroup.Open,"Rules: eight first on open table fouls even when called ball pots");
            r=Assigned(); check(r.Groups[0]==BallGroup.Solids&&r.Groups[1]==BallGroup.Stripes&&r.Turn==0,"Rules: legal called pot assigns opposite groups");
            check(!r.Shoot(0,9,0)&&!r.Shoot(0,8,0),"Rules: no opponent-group or premature eight call");
            r.Shoot(0,3,0); r.Resolve(Shot(3,3,1)); check(!r.Foul&&r.Turn==1&&!r.OnTable(3),"Rules: wrong pocket loses turn but object remains potted");
            r=Assigned(); r.Shoot(0,3,0); r.Resolve(Shot(9,-1,0,false)); check(r.Foul&&r.Phase==EightBallPhase.Placement&&!r.HeadOnly,"Rules: wrong first contact grants full-table cue in hand");
            r=Assigned(); r.Shoot(0,3,0); r.Resolve(Shot(3,-1,0,false)); check(r.Foul,"Rules: no rail or pot after contact fouls");
            r=Assigned(); r.Shoot(0,3,0); e=Shot(3,-1,0,false); e.Rail(0,9); r.Resolve(e); check(r.Foul,"Rules: rail before contact cannot legalize shot");
            r=Assigned(); r.Shoot(0,3,0); e=Shot(9); e.Contact(3,10); r.Resolve(e); check(!r.Foul,"Rules: simultaneous first contacts resolve as set independent of callback order");
            r=Assigned(); r.Shoot(0,3,0); e=Shot(3,3); e.Pockets.Add(0,1); r.Resolve(e); check(r.Foul&&!r.OnTable(3)&&r.Turn==1,"Rules: coloured pots remain down after scratch");
            r=Assigned(); r.Shoot(0,3,0); e=Shot(3); e.OffTable.Add(3); r.Resolve(e); check(r.Foul&&r.OnTable(3),"Rules: jumped colour is re-spotted and fouls");
            r=Assigned(); r.Shoot(0,3,0); e=Shot(3); e.Timeout=true; r.Resolve(e); check(r.Foul&&r.Turn==1,"Rules: physics safety timeout resolves as foul");
            r=Assigned(); r.Shoot(0,3,0); r.Resolve(Shot(3,8)); check(r.Winner==1&&r.Phase==EightBallPhase.Result,"Rules: premature eight loses");
            for(int kind=0;kind<4;kind++) {
                r=Assigned(); for(int ball=3;ball<=7;ball++) { r.Shoot(0,ball,0); r.Resolve(Shot(ball,ball)); }
                check(r.Left(0)==0&&r.CanCall(0,8,0),"Rules: cleared group permits eight, case "+kind);
                r.Shoot(0,8,0); e=Shot(8,8,kind==1?1:0); if(kind==2) e.Pockets.Add(0,2); if(kind==3) { e.Pockets.Remove(8); e.OffTable.Add(8); }
                r.Resolve(e); check(r.Winner==(kind==0?0:1),"Rules: final eight legal/wrong-pocket/scratch/off-table outcome, case "+kind);
            }
            r=Assigned(); for(int ball=3;ball<7;ball++) { r.Shoot(0,ball,0); r.Resolve(Shot(ball,ball)); }
            r.Shoot(0,7,0); e=Shot(7,7); e.Pockets.Add(8,0); r.Resolve(e); check(r.Winner==1,"Rules: last own ball and eight together lose");
            bool racks=true; var random=new Random(812);
            for(int n=0;n<1000;n++) { var rack=new EightBallRules(n%2).Rack(random); var unique=new HashSet<int>(rack); racks&=unique.Count==15&&rack[4]==8&&EightBallRules.GroupOf(rack[10])!=EightBallRules.GroupOf(rack[14]); }
            check(racks,"Rules: 1000 seeded racks have unique balls, centre eight and opposite rear corners");
            r=Open(); r.Shoot(0,2,0); e=Shot(8);
            for(int i=2;i<16;i++) if(i!=8) e.Pockets.Add(i,0);
            r.Resolve(e);
            check(r.RerackRequired&&r.Break&&r.Phase==EightBallPhase.Placement&&r.OnTable(1)&&r.OnTable(15),"Rules: open table with no colours cannot deadlock; repeat original rack without another opening charge");
            r=new EightBallRules(1); r.Place(1); r.Shoot(1,0,-1); e=Shot(1);
            for(int i=1;i<16;i++) e.Pockets.Add(i,0);
            r.Resolve(e);
            check(r.RerackRequired&&r.Turn==1&&r.OnTable(8)&&r.OnTable(15),"Rules: all colours and eight on break restore a playable original rack rather than a deadlocked open table");
            var t=new TableSession("pool",2); check(t.Sit(1,false,0)==SeatReply.Rejected&&!t.Organizer.HasValue,"Seats: ineligible actor cannot claim seat");
            t.Sit(1,true,0); t.Sit(2,true,0); check(t.Organizer==1&&t.SeatOf(1)!=t.SeatOf(2)&&t.Sit(3,true,0)==SeatReply.Rejected,"Seats: exclusive seats and organizer");
            check(!t.Start(2,2)&&t.Start(1,2),"Seats: only organizer starts with minimum participants");
            t.SetTurn(1,10); t.LeaveSeat(2,20); check(t.Sit(3,true,20)==SeatReply.Waiting&&!t.Participates(3)&&!t.ExpiredAbsence(1000,false).HasValue,"Seats: late arrival waits; seated turns have no clock");
            t.SetTurn(2,1000); check(!t.ExpiredAbsence(1060,false).HasValue&&t.ExpiredAbsence(1060.01,false)==2,"Seats: absence expires strictly after 60 seconds on turn");
            t.Sit(2,true,1050); t.LeaveSeat(2,1055); check(!t.ExpiredAbsence(1115,false).HasValue&&t.ExpiredAbsence(1116,false)==2,"Seats: return cancels clock and later exit restarts it");
            check(!t.ExpiredAbsence(2000,true).HasValue,"Seats: ongoing shot resolves before absence forfeiture");
            t.LeaveSeat(1,1116); check(!t.Organizer.HasValue,"Seats: all seats empty means no organizer");
            t.Sit(2,true,1117); check(t.Organizer==2,"Seats: remaining occupant becomes organizer");
            t.Finish(); t.Close(); t.LeaveSeat(2,1118); check(t.Sit(4,true,1119)==SeatReply.Rejected&&t.Sit(3,true,1119)==SeatReply.Seated,"Seats: waiting order has priority without teleporting anyone");
            t=new TableSession("preferred",2); t.Sit(1,true,0,1);
            check(t.SeatOf(1)==1&&t.Organizer==1,"Seats: nearest requested free seat preserves first-arrival organizer");
        }
    }
}
