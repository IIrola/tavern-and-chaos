using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public enum LedgerResult { Rejected, Applied, Replayed }
    public enum LedgerRoundState { Reserved, Active, Settled, Cancelled }
    public enum HouseOutcome { Loss, Push, Win, Natural }

    // Pure authority-only ledger. Assets configure prices; mutable money never lives in an asset.
    public sealed class SessionLedger
    {
        public sealed class Account
        {
            public ulong Id { get; internal set; }
            public string Name { get; internal set; }
            public bool Connected { get; internal set; }
            public long Balance { get; internal set; }
            public int Wins { get; internal set; }
            public int Draws { get; internal set; }
            public int WagersWon { get; internal set; }
            public int Falls { get; internal set; }
            public int Drinks { get; internal set; }
            public decimal Staked { get; internal set; }
            public decimal Won { get; internal set; }
            public decimal Lost { get; internal set; }
            public decimal Services { get; internal set; }
            public decimal Discarded { get; internal set; }
        }
        private sealed class Round
        {
            public string Kind; public ulong Organizer; public long Fee, Stake, Total;
            public bool AgainstHouse; public LedgerRoundState State;
            public string Outcome;
            public readonly List<ulong> Players=new();
            public readonly Dictionary<ulong,long> Payouts=new();
        }
        private readonly long initial;
        private readonly Dictionary<ulong,Account> accounts=new();
        private readonly Dictionary<string,Round> rounds=new();
        private readonly Dictionary<string,(ulong player,long amount,string service)> services=new();
        private readonly HashSet<string> credited=new(),fallEvents=new();
        public IEnumerable<Account> Accounts=>accounts.Values;
        public bool Closed { get; private set; }
        public decimal InitialIssued { get; private set; }
        public decimal BankIssued { get; private set; }
        public decimal BarRevenue { get; private set; }
        public decimal BankReceived { get; private set; }
        public decimal Destroyed { get; private set; }
        public SessionLedger(long initial=1000) { if(initial<0) throw new ArgumentOutOfRangeException(nameof(initial)); this.initial=initial; }
        public Account Find(ulong id)=>accounts.TryGetValue(id,out var a)?a:null;
        public long Balance(ulong id)=>Find(id)?.Balance??0;
        public bool IsConnected(ulong id)=>Find(id)?.Connected==true;
        public bool Join(ulong id,string name,string identity)
        {
            if(Closed||accounts.ContainsKey(id)||string.IsNullOrWhiteSpace(identity)) return false;
            long grant=credited.Add(identity)?initial:0;
            accounts.Add(id,new Account{Id=id,Name=name,Connected=true,Balance=grant}); InitialIssued+=grant; return true;
        }
        public void Leave(ulong id)
        {
            var a=Find(id); if(a==null||!a.Connected) return;
            a.Connected=false; Destroyed+=a.Balance; a.Discarded+=a.Balance; a.Balance=0;
        }
        public LedgerResult Spend(string operation,ulong id,long amount,string service)
        {
            if(string.IsNullOrEmpty(operation)||amount<0||string.IsNullOrEmpty(service)) return LedgerResult.Rejected;
            if(services.TryGetValue(operation,out var old)) return old==(id,amount,service)?LedgerResult.Replayed:LedgerResult.Rejected;
            var a=Find(id); if(Closed||a==null||!a.Connected||a.Balance<amount) return LedgerResult.Rejected;
            a.Balance-=amount; a.Services+=amount; BarRevenue+=amount;
            if(service=="drink") a.Drinks++;
            services.Add(operation,(id,amount,service)); return LedgerResult.Applied;
        }
        public LedgerResult ReserveOpening(string key,string kind,ulong organizer,IReadOnlyList<ulong> players,long fee,long stake,int minimum=2,bool againstHouse=false)
        {
            if(string.IsNullOrEmpty(key)||string.IsNullOrEmpty(kind)||players==null||fee<0||stake<0||minimum<2||minimum>6||players.Count<minimum||players.Count>6) return LedgerResult.Rejected;
            if(rounds.TryGetValue(key,out var old)) {
                bool same=old.Kind==kind&&old.Organizer==organizer&&old.Fee==fee&&old.Stake==stake&&old.AgainstHouse==againstHouse&&old.Players.Count==players.Count;
                for(int i=0;same&&i<players.Count;i++) same=old.Players[i]==players[i];
                return same?LedgerResult.Replayed:LedgerResult.Rejected;
            }
            if(Closed) return LedgerResult.Rejected;
            var unique=new HashSet<ulong>(); long total=0;
            foreach(ulong id in players) {
                if(!unique.Add(id)||!IsConnected(id)||!Add(stake,id==organizer?fee:0,out long due)||Balance(id)<due||!Add(total,stake,out total)) return LedgerResult.Rejected;
            }
            if(!unique.Contains(organizer)) return LedgerResult.Rejected;
            var r=new Round{Kind=kind,Organizer=organizer,Fee=fee,Stake=stake,Total=total,AgainstHouse=againstHouse,State=LedgerRoundState.Reserved};
            foreach(ulong id in players) { r.Players.Add(id); accounts[id].Balance-=stake+(id==organizer?fee:0); }
            rounds.Add(key,r); return LedgerResult.Applied;
        }
        public LedgerResult Activate(string key)
        {
            if(!rounds.TryGetValue(key,out var r)||Closed||r.State==LedgerRoundState.Cancelled) return LedgerResult.Rejected;
            if(r.State!=LedgerRoundState.Reserved) return LedgerResult.Replayed;
            r.State=LedgerRoundState.Active; BarRevenue+=r.Fee; accounts[r.Organizer].Services+=r.Fee;
            foreach(ulong id in r.Players) accounts[id].Staked+=r.Stake;
            return LedgerResult.Applied;
        }
        public LedgerResult AbortOpening(string key)
        {
            if(!rounds.TryGetValue(key,out var r)||r.State==LedgerRoundState.Active||r.State==LedgerRoundState.Settled) return LedgerResult.Rejected;
            if(r.State==LedgerRoundState.Cancelled) return LedgerResult.Replayed;
            foreach(ulong id in r.Players) if(!CanCredit(id,r.Stake+(id==r.Organizer?r.Fee:0),key)) return LedgerResult.Rejected;
            foreach(ulong id in r.Players) Credit(accounts[id],r.Stake+(id==r.Organizer?r.Fee:0));
            r.State=LedgerRoundState.Cancelled; return LedgerResult.Applied;
        }
        public LedgerResult SettleMatch(string key,IReadOnlyList<ulong> winners,bool draw=false)
        {
            if(!rounds.TryGetValue(key,out var r)||r.AgainstHouse||r.State==LedgerRoundState.Reserved||r.State==LedgerRoundState.Cancelled||winners==null) return LedgerResult.Rejected;
            string outcome="match/"+draw+"/"+string.Join(",",winners);
            if(r.State==LedgerRoundState.Settled) return r.Outcome==outcome?LedgerResult.Replayed:LedgerResult.Rejected;
            var unique=new HashSet<ulong>(); foreach(ulong id in winners) if(!unique.Add(id)||!r.Players.Contains(id)) return LedgerResult.Rejected;
            var payouts=new Dictionary<ulong,long>(); foreach(ulong id in r.Players) payouts[id]=0;
            if(draw||winners.Count==0) foreach(ulong id in r.Players) payouts[id]=r.Stake;
            else for(int i=0;i<winners.Count;i++) payouts[winners[i]]=r.Total/winners.Count+(i<r.Total%winners.Count?1:0);
            foreach(var p in payouts) if(!CanCredit(p.Key,p.Value,key)) return LedgerResult.Rejected;
            foreach(var p in payouts) {
                var a=accounts[p.Key]; Credit(a,p.Value); RecordNet(a,r.Stake,p.Value); r.Payouts[p.Key]=p.Value;
                if(unique.Contains(p.Key)) { if(draw) a.Draws++; else { a.Wins++; if(p.Value>r.Stake) a.WagersWon++; } }
            }
            r.State=LedgerRoundState.Settled; r.Outcome=outcome; return LedgerResult.Applied;
        }
        public LedgerResult CancelMatch(string key)
        {
            if(!rounds.TryGetValue(key,out var r)) return LedgerResult.Rejected;
            if(r.State==LedgerRoundState.Reserved) return AbortOpening(key);
            if(r.State==LedgerRoundState.Cancelled||r.State==LedgerRoundState.Settled) return LedgerResult.Replayed;
            foreach(ulong id in r.Players) if(!CanCredit(id,r.Stake,key)) return LedgerResult.Rejected;
            foreach(ulong id in r.Players) { Credit(accounts[id],r.Stake); r.Payouts[id]=r.Stake; }
            r.State=LedgerRoundState.Cancelled; return LedgerResult.Applied;
        }
        // Used by the later blackjack module; it never draws from a finite bar balance.
        public LedgerResult SettleHouse(string key,IReadOnlyDictionary<ulong,HouseOutcome> outcomes)
        {
            if(!rounds.TryGetValue(key,out var r)||!r.AgainstHouse||r.State==LedgerRoundState.Reserved||r.State==LedgerRoundState.Cancelled||outcomes==null) return LedgerResult.Rejected;
            if(outcomes.Count!=r.Players.Count) return LedgerResult.Rejected;
            string signature="house"; foreach(ulong id in r.Players) { if(!outcomes.ContainsKey(id)) return LedgerResult.Rejected; signature+="/"+id+":"+(int)outcomes[id]; }
            if(r.State==LedgerRoundState.Settled) return r.Outcome==signature?LedgerResult.Replayed:LedgerResult.Rejected;
            var payouts=new Dictionary<ulong,long>(); var profits=new Dictionary<ulong,long>();
            foreach(ulong id in r.Players) {
                if(!outcomes.TryGetValue(id,out var outcome)||outcome<HouseOutcome.Loss||outcome>HouseOutcome.Natural) return LedgerResult.Rejected;
                long profit=0,payout=0;
                if(outcome==HouseOutcome.Natural) { if(r.Stake%2!=0||!Add(r.Stake,r.Stake/2,out profit)) return LedgerResult.Rejected; }
                else if(outcome==HouseOutcome.Win) profit=r.Stake;
                if(outcome!=HouseOutcome.Loss&&!Add(r.Stake,profit,out payout)) return LedgerResult.Rejected;
                if(!CanCredit(id,payout,key)) return LedgerResult.Rejected;
                payouts[id]=payout; profits[id]=profit;
            }
            foreach(ulong id in r.Players) {
                var a=accounts[id]; long payout=payouts[id]; Credit(a,payout); RecordNet(a,r.Stake,payout); r.Payouts[id]=payout;
                BankIssued+=profits[id]; if(outcomes[id]==HouseOutcome.Loss) BankReceived+=r.Stake;
                if(outcomes[id]==HouseOutcome.Push) a.Draws++;
                if(profits[id]>0) { a.Wins++; a.WagersWon++; }
            }
            r.State=LedgerRoundState.Settled; r.Outcome=signature; return LedgerResult.Applied;
        }
        public bool RecordFall(string operation,ulong id)
        {
            var a=Find(id); if(Closed||a==null||!a.Connected||string.IsNullOrEmpty(operation)||!fallEvents.Add(operation)) return false;
            a.Falls++; return true;
        }
        public bool IsSettled(string key)=>rounds.TryGetValue(key??"",out var r)&&(r.State==LedgerRoundState.Settled||r.State==LedgerRoundState.Cancelled);
        public long Pot(string key)=>rounds.TryGetValue(key??"",out var r)&&!IsSettled(key)?r.Total:0;
        public long Payout(string key,ulong id)=>rounds.TryGetValue(key??"",out var r)&&r.Payouts.TryGetValue(id,out long value)?value:0;
        public bool HasPending { get { foreach(var r in rounds.Values) if(r.State==LedgerRoundState.Active||r.State==LedgerRoundState.Reserved) return true; return false; } }
        public decimal AuditDelta
        {
            get {
                decimal held=0,available=0; foreach(var a in accounts.Values) available+=a.Balance;
                foreach(var r in rounds.Values) if(r.State==LedgerRoundState.Reserved||r.State==LedgerRoundState.Active) held+=r.Total+(r.State==LedgerRoundState.Reserved?r.Fee:0);
                return InitialIssued+BankIssued-available-held-BarRevenue-BankReceived-Destroyed;
            }
        }
        public void Close()
        {
            if(Closed) return;
            foreach(string key in new List<string>(rounds.Keys)) if(!IsSettled(key)) CancelMatch(key);
            foreach(var a in accounts.Values) { Destroyed+=a.Balance; a.Discarded+=a.Balance; a.Balance=0; }
            Closed=true;
        }
        private static bool Add(long a,long b,out long sum) { sum=0; if(a<0||b<0||a>long.MaxValue-b) return false; sum=a+b; return true; }
        private bool CanCredit(ulong id,long value,string closingRound)
        {
            if(!IsConnected(id)||Closed) return true;
            long owned=Balance(id);
            foreach(var pair in rounds) if(pair.Key!=closingRound&&pair.Value.Players.Contains(id)) {
                var r=pair.Value;
                if(r.State==LedgerRoundState.Reserved||r.State==LedgerRoundState.Active) {
                    if(!Add(owned,r.Stake,out owned)) return false;
                    if(r.State==LedgerRoundState.Reserved&&r.Organizer==id&&!Add(owned,r.Fee,out owned)) return false;
                }
            }
            return Add(owned,value,out _);
        }
        private void Credit(Account a,long amount) { if(a.Connected&&!Closed) a.Balance+=amount; else { Destroyed+=amount; a.Discarded+=amount; } }
        private static void RecordNet(Account a,long stake,long payout) { if(payout>=stake) a.Won+=(decimal)payout-stake; else a.Lost+=(decimal)stake-payout; }
    }
}
