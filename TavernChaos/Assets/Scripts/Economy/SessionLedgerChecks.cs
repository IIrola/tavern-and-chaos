using System;
using System.Collections.Generic;
namespace TavernChaos
{
    public static class SessionLedgerChecks
    {
        private static SessionLedger New(int people=2,long initial=1000)
        {
            var l=new SessionLedger(initial); for(ulong i=0;i<(ulong)people;i++) l.Join(i,"P"+i,"ip"+i); return l;
        }
        private static SessionLedger Active(long stake=100,long fee=10,bool house=false,int people=2)
        {
            var l=New(people); var ids=new List<ulong>(); for(ulong i=0;i<(ulong)people;i++) ids.Add(i);
            l.ReserveOpening("r","pool",0,ids,fee,stake,againstHouse:house); l.Activate("r"); return l;
        }
        public static void Run(Action<bool,string> check)
        {
            var ids=new ulong[]{0,1}; var l=New();
            check(l.Balance(0)==1000&&l.Balance(1)==1000&&l.AuditDelta==0,"Ledger: unique IPs receive initial grant with balanced book");
            check(l.Join(2,"same IP","ip1")&&l.Balance(2)==0&&!l.Join(0,"duplicate","other"),"Ledger: one grant per IP and no duplicate participant identifier");
            l.Leave(1); l.Leave(1); l.Join(3,"returned","ip1");
            check(l.Balance(3)==0&&l.Destroyed==1000&&l.Find(1)!=null&&!l.Find(1).Connected&&l.AuditDelta==0,"Ledger: exit destroys available balance exactly once and history survives return");
            check(New().Balance(0)==1000,"Ledger: another room starts a separate grant history");
            l=New();
            check(l.ReserveOpening("r","pool",0,ids,10,100)==LedgerResult.Applied&&l.Balance(0)==890&&l.Balance(1)==900&&l.Pot("r")==200&&l.BarRevenue==0&&l.AuditDelta==0,"Ledger: opening fee and equal stakes reserve atomically outside bar revenue");
            check(l.ReserveOpening("r","pool",0,ids,10,100)==LedgerResult.Replayed&&l.ReserveOpening("r","pool",0,ids,10,101)==LedgerResult.Rejected&&l.Balance(0)==890,"Ledger: exact opening replay is harmless and changed payload is rejected");
            check(l.Activate("r")==LedgerResult.Applied&&l.Activate("r")==LedgerResult.Replayed&&l.BarRevenue==10&&l.Find(0).Services==10&&l.Find(1).Staked==100&&l.AuditDelta==0,"Ledger: opening revenue and staking statistics commit once");
            check(l.AbortOpening("r")==LedgerResult.Rejected,"Ledger: started round cannot reclaim opening fee as a failed opening");
            check(l.SettleMatch("r",new ulong[]{0})==LedgerResult.Applied&&l.Balance(0)==1090&&l.Balance(1)==900&&l.Pot("r")==0,"Ledger: winner gets pot including own returned stake");
            check(l.Find(0).Wins==1&&l.Find(0).WagersWon==1&&l.Find(0).Won==100&&l.Find(1).Lost==100,"Ledger: table victory counts once and betting gain excludes own returned stake");
            check(l.SettleMatch("r",new ulong[]{0})==LedgerResult.Replayed&&l.SettleMatch("r",new ulong[]{1})==LedgerResult.Rejected&&l.Find(0).Wins==1&&l.AuditDelta==0,"Ledger: repeated settlement cannot mint a prize or change winner");
            l=New(); long before=l.Balance(0);
            check(l.ReserveOpening("a","pool",0,new ulong[]{0},10,50)==LedgerResult.Rejected&&l.Balance(0)==before,"Ledger: insufficient participant count costs nothing");
            check(l.ReserveOpening("b","pool",0,new ulong[]{0,0},10,50)==LedgerResult.Rejected&&l.ReserveOpening("c","pool",3,ids,10,50)==LedgerResult.Rejected,"Ledger: duplicate participants and absent organizer cannot reserve");
            check(l.ReserveOpening("d","pool",0,ids,10,1000)==LedgerResult.Rejected&&l.Balance(1)==1000&&l.AuditDelta==0,"Ledger: insufficient organizer total cannot partially debit the other side");
            check(l.ReserveOpening("e","pool",0,ids,-1,0)==LedgerResult.Rejected&&l.ReserveOpening("f","pool",0,ids,10,-1)==LedgerResult.Rejected,"Ledger: negative price or wager is rejected");
            l.ReserveOpening("r","pool",0,ids,10,100); l.AbortOpening("r");
            check(l.Balance(0)==1000&&l.Balance(1)==1000&&l.BarRevenue==0&&l.Find(0).Staked==0&&!l.HasPending&&l.AuditDelta==0,"Ledger: failed opening restores all reservations without service or game statistics");
            check(l.AbortOpening("r")==LedgerResult.Replayed&&l.Activate("r")==LedgerResult.Rejected,"Ledger: cancelled reservation cannot activate or refund twice");
            l=Active(); l.CancelMatch("r");
            check(l.Balance(0)==990&&l.Balance(1)==1000&&l.BarRevenue==10&&l.Find(0).Wins==0&&l.AuditDelta==0,"Ledger: cancellation after start refunds stakes but retains opening fee");
            l=Active(); l.SettleMatch("r",ids,true);
            check(l.Balance(0)==990&&l.Balance(1)==1000&&l.Find(0).Draws==1&&l.Find(1).Draws==1&&l.Find(0).Won==0&&l.Find(1).Lost==0&&l.AuditDelta==0,"Ledger: draw returns each stake and counts draws without false profits");
            l=Active(0); l.SettleMatch("r",new ulong[]{1});
            check(l.Find(1).Wins==1&&l.Find(1).WagersWon==0&&l.Find(1).Won==0,"Ledger: zero-bet match can award a victory without creating money");
            l=Active(); l.Leave(1); l.SettleMatch("r",new ulong[]{0});
            check(l.Balance(0)==1090&&l.Destroyed==900&&l.Find(1).Lost==100&&l.AuditDelta==0,"Ledger: departed stake remains in pot for the eligible winner");
            l=Active(); l.Leave(0); l.Leave(1); l.SettleMatch("r",Array.Empty<ulong>());
            check(l.Destroyed==1990&&l.BarRevenue==10&&!l.HasPending&&l.AuditDelta==0,"Ledger: no eligible winner returns stakes to closed accounts as destroyed funds");
            l=New(); l.ReserveOpening("r","pool",0,ids,10,100); l.Leave(0); l.AbortOpening("r");
            check(l.Destroyed==1000&&l.Balance(1)==1000&&l.BarRevenue==0&&l.AuditDelta==0,"Ledger: failed opening after departure refunds nobody an exited balance");
            l=Active(); check(l.SettleMatch("r",new ulong[]{99})==LedgerResult.Rejected&&l.Pot("r")==200,"Ledger: nonparticipant cannot receive the pot");
            l=New();
            check(l.Spend("drink1",0,20,"drink")==LedgerResult.Applied&&l.Spend("drink1",0,20,"drink")==LedgerResult.Replayed&&l.Balance(0)==980&&l.Find(0).Drinks==1,"Ledger: duplicate drink charge has one debit and one drink statistic");
            check(l.Spend("drink1",1,20,"drink")==LedgerResult.Rejected&&l.Spend("bad",0,-1,"drink")==LedgerResult.Rejected&&l.Spend("over",0,1000,"drink")==LedgerResult.Rejected&&l.AuditDelta==0,"Ledger: conflicting and unaffordable service requests leave the book intact");
            check(l.RecordFall("fall1",0)&&!l.RecordFall("fall1",0)&&l.Find(0).Falls==1,"Ledger: fall statistic accepts one authority event once");
            l=Active(26,10,true,4);
            var hands=new Dictionary<ulong,HouseOutcome>{{0,HouseOutcome.Loss},{1,HouseOutcome.Push},{2,HouseOutcome.Win},{3,HouseOutcome.Natural}};
            check(l.SettleHouse("r",hands)==LedgerResult.Applied&&l.Balance(0)==964&&l.Balance(1)==1000&&l.Balance(2)==1026&&l.Balance(3)==1039,"Ledger: bank loss/push/1:1/3:2 payments are exact integers");
            check(l.BankIssued==65&&l.BankReceived==26&&l.BarRevenue==10&&l.AuditDelta==0,"Ledger: infinite-bank profit emission is separate from returned stake and fees");
            check(l.SettleHouse("r",hands)==LedgerResult.Replayed&&l.Find(3).Wins==1,"Ledger: house result cannot pay twice");
            l=Active(27,10,true); var natural=new Dictionary<ulong,HouseOutcome>{{0,HouseOutcome.Natural},{1,HouseOutcome.Loss}};
            check(l.SettleHouse("r",natural)==LedgerResult.Rejected&&l.Pot("r")==54&&l.AuditDelta==0,"Ledger: fractional natural payment is rejected before mutation");
            l=Active(26,10,true); l.Leave(1); var closedWin=new Dictionary<ulong,HouseOutcome>{{0,HouseOutcome.Push},{1,HouseOutcome.Win}}; l.SettleHouse("r",closedWin);
            check(l.Find(1).Balance==0&&l.Find(1).Won==26&&l.BankIssued==26&&l.AuditDelta==0,"Ledger: house result for departed hand preserves statistics and destroys its payout");
            l=New(2,long.MaxValue); l.ReserveOpening("r","pool",0,ids,0,1); l.Activate("r");
            check(l.SettleMatch("r",new ulong[]{0})==LedgerResult.Rejected&&l.Pot("r")==2&&l.Balance(0)==long.MaxValue-1&&l.AuditDelta==0,"Ledger: balance overflow rejects payout atomically");
            l.CancelMatch("r"); check(l.Balance(0)==long.MaxValue&&l.AuditDelta==0,"Ledger: rejected overflow does not strand refundable stake");
            l=New(3,long.MaxValue); l.ReserveOpening("held","pool",0,ids,0,1); l.Activate("held"); l.ReserveOpening("house","bank",0,new ulong[]{0,2},0,1,againstHouse:true); l.Activate("house");
            var capacity=new Dictionary<ulong,HouseOutcome>{{0,HouseOutcome.Win},{2,HouseOutcome.Loss}};
            check(l.SettleHouse("house",capacity)==LedgerResult.Rejected,"Ledger: payout preserves capacity for refunds held in another round");
            l.CancelMatch("house"); l.CancelMatch("held"); check(l.Balance(0)==long.MaxValue&&l.AuditDelta==0,"Ledger: all simultaneous reservations remain safely refundable");
            Fuzz(check);
        }
        private static void Fuzz(Action<bool,string> check)
        {
            var random=new Random(1905); var l=New(6); bool conserved=true,nonnegative=true; int applied=0,rooms=1;
            for(int n=0;n<1000;n++) {
                var players=new List<ulong>(); foreach(var entry in l.Accounts) if(entry.Connected) players.Add(entry.Id);
                if(players.Count<2) {
                    l.Close(); conserved&=l.AuditDelta==0&&!l.HasPending; l=New(6); rooms++;
                    players=new List<ulong>{0,1,2,3,4,5};
                }
                int ai=random.Next(players.Count),bi=(ai+1+random.Next(players.Count-1))%players.Count; ulong a=players[ai],b=players[bi];
                long affordable=Math.Min(l.Balance(a)-10,l.Balance(b));
                if(affordable<0) continue;
                long stake=random.Next((int)Math.Min(50,affordable)+1); string key="fuzz"+n;
                if(l.ReserveOpening(key,"pool",a,new[]{a,b},10,stake)!=LedgerResult.Applied) { conserved=false; break; }
                if(n%9==0) l.AbortOpening(key);
                else { l.Activate(key); if(n%13==0) l.Leave(b); if(n%7==0) l.CancelMatch(key); else l.SettleMatch(key,new[]{a},n%11==0); }
                applied++; conserved&=l.AuditDelta==0; foreach(var account in l.Accounts) nonnegative&=account.Balance>=0;
            }
            check(conserved&&nonnegative&&applied>800,"Ledger: 1000 seeded attempts preserve all chips through reservations, results, cancellation and departure ["+applied+" rounds in "+rooms+" rooms]");
            l.Close(); l.Close(); bool zero=true; foreach(var a in l.Accounts) zero&=a.Balance==0;
            check(zero&&!l.HasPending&&l.Closed&&l.AuditDelta==0&&!l.Join(999,"new","new"),"Ledger: room close clears funds and pending pots once, without allowing a new grant");
        }
    }
}
