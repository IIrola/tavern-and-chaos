using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public enum TablePhase { Available, Preparation, Playing, Result }
    public enum SeatReply { Rejected, Seated, Waiting }

    // Engine-independent authority data. The caller verifies world proximity and actor eligibility.
    public sealed class TableSession
    {
        public string Id { get; }
        public int Capacity { get; }
        public TablePhase Phase { get; private set; }
        public ulong? Organizer { get; private set; }
        public ulong? Turn { get; private set; }
        public double? AbsentSince { get; private set; }
        public IReadOnlyList<ulong> Participants => participants;
        public IReadOnlyList<ulong> Waiting => waiting;
        private readonly ulong?[] seats;
        private readonly List<ulong> participants=new(), waiting=new();
        public TableSession(string id,int capacity)
        {
            if(capacity<2||capacity>6) throw new ArgumentOutOfRangeException(nameof(capacity));
            Id=id; Capacity=capacity; seats=new ulong?[capacity];
        }
        public int SeatOf(ulong id) { for(int i=0;i<seats.Length;i++) if(seats[i]==id) return i; return -1; }
        public ulong? Occupant(int seat) => seat>=0&&seat<Capacity?seats[seat]:null;
        public bool Participates(ulong id) => participants.Contains(id);
        public SeatReply Sit(ulong id,bool eligible,double now,int preferredSeat=-1)
        {
            if(!eligible) return SeatReply.Rejected;
            if(SeatOf(id)>=0) return SeatReply.Seated;
            if((Phase==TablePhase.Playing||Phase==TablePhase.Result)&&!Participates(id)) {
                if(!waiting.Contains(id)) waiting.Add(id); return SeatReply.Waiting;
            }
            if(Phase!=TablePhase.Playing&&waiting.Count>0&&!waiting.Contains(id)) return SeatReply.Rejected;
            if(waiting.Contains(id)&&waiting[0]!=id) return SeatReply.Rejected;
            int free=preferredSeat>=0&&preferredSeat<Capacity&&!seats[preferredSeat].HasValue?preferredSeat:Array.FindIndex(seats,s=>!s.HasValue);
            if(free<0) return SeatReply.Rejected;
            seats[free]=id; waiting.Remove(id); RefreshOrganizer();
            if(Phase==TablePhase.Available) Phase=TablePhase.Preparation;
            if(Turn==id) AbsentSince=null;
            return SeatReply.Seated;
        }
        public bool LeaveSeat(ulong id,double now)
        {
            int seat=SeatOf(id); if(seat<0) { waiting.Remove(id); return false; }
            seats[seat]=null; RefreshOrganizer();
            if(Turn==id&&Phase==TablePhase.Playing) AbsentSince=now;
            if(Phase==TablePhase.Preparation&&!Organizer.HasValue) Phase=TablePhase.Available;
            return true;
        }
        private void RefreshOrganizer()
        {
            if(Organizer.HasValue&&SeatOf(Organizer.Value)>=0) return;
            Organizer=null; foreach(var seat in seats) if(seat.HasValue) { Organizer=seat; break; }
        }
        public bool CanStart(ulong organizer,int minimum)
        {
            if(Phase!=TablePhase.Preparation||Organizer!=organizer) return false;
            int occupied=0; foreach(var s in seats) if(s.HasValue) occupied++;
            return occupied>=minimum;
        }
        public bool Start(ulong organizer,int minimum)
        {
            if(!CanStart(organizer,minimum)) return false;
            participants.Clear(); foreach(var s in seats) if(s.HasValue) participants.Add(s.Value);
            Phase=TablePhase.Playing; Turn=null; AbsentSince=null; return true;
        }
        public void SetTurn(ulong id,double now)
        {
            if(Phase!=TablePhase.Playing||!Participates(id)) return;
            if(Turn!=id) { Turn=id; AbsentSince=SeatOf(id)<0?now:null; }
            else if(SeatOf(id)>=0) AbsentSince=null;
            else if(!AbsentSince.HasValue) AbsentSince=now;
        }
        public ulong? ExpiredAbsence(double now,bool resolvingAction)
        {
            if(Phase!=TablePhase.Playing||resolvingAction||!Turn.HasValue||!AbsentSince.HasValue) return null;
            return now-AbsentSince.Value>60?Turn:null;
        }
        public float AbsenceRemaining(double now) => AbsentSince.HasValue?(float)Math.Max(0,60-(now-AbsentSince.Value)):60;
        public void Finish() { Phase=TablePhase.Result; Turn=null; AbsentSince=null; }
        internal void AbortStart() { participants.Clear(); Turn=null; AbsentSince=null; Phase=Organizer.HasValue?TablePhase.Preparation:TablePhase.Available; }
        public void Close()
        {
            participants.Clear(); Turn=null; AbsentSince=null;
            // Queue is an invitation order, never a remote teleport: a waiting person must still sit nearby.
            Phase=Organizer.HasValue?TablePhase.Preparation:TablePhase.Available;
        }
        public void Remove(ulong id,double now) { waiting.Remove(id); LeaveSeat(id,now); }
    }
}
