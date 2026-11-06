using System;

namespace TavernChaos
{
    public enum CocktailVesselPhase { Held, Resting, Flying, Spent }
    // Immutable contents plus exclusive server ownership. Geometry and proximity stay in the room controller.
    public sealed class ServedCocktail
    {
        public const double Lifetime = 30, FlightLimit = 6, OfferLifetime = 3;
        public ulong Id { get; }
        public string Origin => "cocktail/" + Id;
        public CocktailRecipe Recipe { get; }
        public CocktailKind Kind => Recipe?.Kind ?? CocktailKind.FreeMix;
        public int Intoxication { get; }
        public int Container { get; }
        public double Created { get; }
        public double Expires => Created + Lifetime;
        public double FlightEnds { get; private set; }
        public ulong? Holder { get; private set; }
        public ulong? OfferedTo { get; private set; }
        public ulong? Thrower { get; private set; }
        public CocktailVesselPhase Phase { get; private set; }
        private double offerEnds, time;

        public ServedCocktail(ulong id, ulong holder, CocktailRecipe recipe, int freeMixIntoxication, int container, double now)
        {
            if (id == 0 || container < 0 || container > 1 || !double.IsFinite(now) || now < 0 || freeMixIntoxication < 0 || freeMixIntoxication > 1)
                throw new ArgumentException("Invalid served cocktail");
            Id = id; Holder = holder; Recipe = recipe; Intoxication = recipe?.Intoxication ?? freeMixIntoxication;
            Container = container; Created = time = now; Phase = CocktailVesselPhase.Held;
        }
        public bool Tick(double now)
        {
            if (!double.IsFinite(now) || now < time) return false;
            time = now;
            if (OfferedTo.HasValue && now >= offerEnds) OfferedTo = null;
            if (Phase != CocktailVesselPhase.Spent && (now >= Expires || Phase == CocktailVesselPhase.Flying && now >= FlightEnds)) Discard();
            return Phase != CocktailVesselPhase.Spent;
        }
        private bool Owned(ulong id, double now) => Tick(now) && Phase == CocktailVesselPhase.Held && Holder == id;
        public bool Offer(ulong id, ulong target, double now)
        {
            if (!Owned(id, now) || id == target) return false;
            OfferedTo = target; offerEnds = now + OfferLifetime; return true;
        }
        public bool Accept(ulong id, double now)
        {
            if (!Tick(now) || Phase != CocktailVesselPhase.Held || OfferedTo != id) return false;
            Holder = id; OfferedTo = null; return true;
        }
        public bool PutDown(ulong id, double now)
        {
            if (!Owned(id, now)) return false;
            Holder = OfferedTo = null; Phase = CocktailVesselPhase.Resting; return true;
        }
        public bool Grab(ulong id, double now)
        {
            if (!Tick(now) || Phase != CocktailVesselPhase.Resting) return false;
            Holder = id; Phase = CocktailVesselPhase.Held; return true;
        }
        public bool Launch(ulong id, double now)
        {
            if (!Owned(id, now)) return false;
            Thrower = id; Holder = OfferedTo = null; Phase = CocktailVesselPhase.Flying; FlightEnds = Math.Min(Expires, now + FlightLimit); return true;
        }
        public bool Consume(ulong id, double now)
        {
            if (!Owned(id, now)) return false;
            Discard(); return true;
        }
        public bool Impact(double now)
        {
            if (!Tick(now) || Phase != CocktailVesselPhase.Flying) return false;
            Discard(); return true;
        }
        public void Discard() { Holder = OfferedTo = null; Phase = CocktailVesselPhase.Spent; }
    }
}
