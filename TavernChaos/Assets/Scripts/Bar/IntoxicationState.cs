using System;
namespace TavernChaos
{
    // Independent modifier: falling or clearing a cocktail effect does not erase sobriety state.
    public sealed class IntoxicationState
    {
        public int Level { get; private set; }
        private double soberAt=double.PositiveInfinity,nextTrip=double.PositiveInfinity,time;
        public const double SoberInterval=30,TripInterval=15;
        public void Add(int levels,double now)
        {
            if(levels<0||!double.IsFinite(now)||now<time)return;Tick(now);
            int before=Level;Level=(int)Math.Min(5,(long)Level+levels);
            if(levels>0&&Level>0)soberAt=now+SoberInterval;
            if(before<5&&Level==5)nextTrip=now+TripInterval;
        }
        public void Tick(double now)
        {
            if(!double.IsFinite(now)||now<time)return;time=now;
            while(Level>0&&now>=soberAt) {Level--;soberAt+=SoberInterval;}
            if(Level==0)soberAt=double.PositiveInfinity;
            if(Level<5)nextTrip=double.PositiveInfinity;
        }
        public bool ShouldTrip(double now,bool moving)
        {
            Tick(now);
            if(Level!=5||!moving||now<nextTrip)return false;
            nextTrip=now+TripInterval;return true;
        }
        public void Clear(double now) {Level=0;time=now;soberAt=nextTrip=double.PositiveInfinity;}
    }
}
