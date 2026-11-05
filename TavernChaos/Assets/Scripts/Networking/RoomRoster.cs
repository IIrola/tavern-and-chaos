using System;
using System.Collections.Generic;

namespace TavernChaos
{
    // Authority-only session data. IP identities and vote membership never come from a client payload.
    public sealed class RoomRoster
    {
        public const int Capacity=6;
        public sealed class Member
        {
            public ulong Id; public string Name; public int Slot, Chips; public ulong LastSequence;
            internal string Identity; internal double BudgetAt; internal float Tokens=80;
        }
        public readonly Dictionary<ulong,Member> Members=new();
        private readonly HashSet<string> credited=new();
        private readonly HashSet<ulong> electorate=new(), votes=new();
        public ulong? VoteTarget { get; private set; }
        public double VoteEnd { get; private set; }
        public int Rejected { get; private set; }
        public Member Add(ulong id,string name,string identity,double now)
        {
            if(Members.Count>=Capacity||Members.ContainsKey(id)) return null;
            int slot=0; while(SlotUsed(slot)) slot++;
            var m=new Member{Id=id,Name=CleanName(name),Identity=identity,Slot=slot,Chips=credited.Add(identity)?1000:0,BudgetAt=now}; Members.Add(id,m); return m;
        }
        private bool SlotUsed(int slot) { foreach(var m in Members.Values) if(m.Slot==slot) return true; return false; }
        public void Remove(ulong id)
        {
            Members.Remove(id); electorate.Remove(id); votes.Remove(id);
            if(VoteTarget==id) CancelVote();
        }
        public bool Accept(ulong id,ulong sequence,double now)
        {
            if(!Members.TryGetValue(id,out var m)||sequence<=m.LastSequence) { Rejected++; return false; }
            m.Tokens=Math.Min(80,m.Tokens+(float)Math.Max(0,now-m.BudgetAt)*50); m.BudgetAt=now;
            // Consume stale/rate-limited sequence numbers too: retrying cannot replay an action later.
            m.LastSequence=sequence;
            if(m.Tokens<1) { Rejected++; return false; } m.Tokens--; return true;
        }
        public bool Spend(ulong id,int amount)
        {
            if(amount<0||!Members.TryGetValue(id,out var m)||m.Chips<amount) return false; m.Chips-=amount; return true;
        }
        public bool BeginVote(ulong proposer,ulong target,double now)
        {
            if(VoteTarget.HasValue||proposer==target||!Members.ContainsKey(proposer)||!Members.ContainsKey(target)) return false;
            electorate.Clear(); votes.Clear(); foreach(ulong id in Members.Keys) electorate.Add(id);
            VoteTarget=target; VoteEnd=now+15; votes.Add(proposer); return true;
        }
        public bool Vote(ulong voter)
        {
            if(!VoteTarget.HasValue||voter==VoteTarget||!electorate.Contains(voter)||!Members.ContainsKey(voter)) return false; return votes.Add(voter);
        }
        public ulong? ResolveVote(double now)
        {
            if(!VoteTarget.HasValue) return null;
            if(votes.Count>electorate.Count/2) { ulong id=VoteTarget.Value; CancelVote(); return id; }
            if(now>=VoteEnd) CancelVote(); return null;
        }
        public int Votes=>votes.Count;
        public int VotesNeeded=>electorate.Count/2+1;
        public void CancelVote() { VoteTarget=null; electorate.Clear(); votes.Clear(); VoteEnd=0; }
        public static string CleanName(string name)
        {
            if(string.IsNullOrWhiteSpace(name)) return "Invitado";
            var chars=new List<char>(); foreach(char c in name.Trim()) if((char.IsLetterOrDigit(c)||c==' '||c=='_'||c=='-')&&chars.Count<24) chars.Add(c);
            return chars.Count==0?"Invitado":new string(chars.ToArray());
        }
    }
}
