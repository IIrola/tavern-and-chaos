using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public readonly struct CocktailUse
    {
        public readonly ulong Recipient;
        public readonly ServedCocktail Vessel;
        public readonly bool Consumed;
        public CocktailUse(ulong recipient, ServedCocktail vessel, bool consumed) { Recipient = recipient; Vessel = vessel; Consumed = consumed; }
    }

    // Authority domain service. Its caller validates geometry/state and stages physical vessel capacity before serving.
    public sealed class BarService
    {
        public const int Capacity = 12;
        public BarStation Station { get; } = new();
        public CocktailRecipes Recipes { get; }
        public long Price { get; }
        public bool Closing { get; private set; }
        public bool HasPending => vessels.Count > 0 || uses.Count > 0;
        public int VesselCount => vessels.Count;
        private readonly SessionLedger ledger;
        private readonly Dictionary<ulong, ServedCocktail> vessels = new();
        private readonly Queue<CocktailUse> uses = new();
        private ulong serial;
        private double time;

        public BarService(SessionLedger ledger, CocktailRecipes recipes, long price)
        {
            this.ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            Recipes = recipes ?? throw new ArgumentNullException(nameof(recipes));
            if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price)); Price = price;
        }
        private bool Advance(double now)
        {
            if (!double.IsFinite(now) || now < time) return false;
            Tick(now); return true;
        }
        public void Tick(double now)
        {
            if (!double.IsFinite(now) || now < time) return;
            time = now; Station.Tick(now); var spent = new List<ulong>();
            foreach (var pair in vessels) if (!pair.Value.Tick(now)) spent.Add(pair.Key);
            foreach (ulong id in spent) vessels.Remove(id);
        }
        public ServedCocktail Find(ulong id) => vessels.TryGetValue(id, out var vessel) ? vessel : null;
        public ServedCocktail HeldBy(ulong id)
        {
            foreach (var vessel in vessels.Values) if (vessel.Phase == CocktailVesselPhase.Held && vessel.Holder == id) return vessel;
            return null;
        }
        public ServedCocktail[] Capture() => new List<ServedCocktail>(vessels.Values).ToArray();
        private bool Admitted(ulong id, bool eligible) => !Closing && !ledger.Closed && eligible && ledger.IsConnected(id);
        public bool Begin(ulong id, bool eligible, double now)
        {
            return Advance(now) && Admitted(id, eligible) && HeldBy(id) == null && Station.Begin(id, true, now);
        }
        public ServedCocktail Serve(ulong id, uint revision, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible) || HeldBy(id) != null || vessels.Count >= Capacity
                || serial == ulong.MaxValue || !Station.CanServe(id, revision, now)) return null;
            var mix = Station.Mix;
            var vessel = new ServedCocktail(serial + 1, id, Recipes.Match(mix), mix.FreeMixIntoxication, Station.Container, now);
            if (ledger.Spend(vessel.Origin + "/serve", id, Price, "cocktail") != LedgerResult.Applied) return null;
            // Everything above is staged and synchronous; there is no yield or external callback in this commit.
            if (!Station.CompleteServe(id, revision, now)) throw new InvalidOperationException("Validated cocktail preparation changed during commit");
            serial++; vessels.Add(vessel.Id, vessel); return vessel;
        }
        public bool Consume(ulong id, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible)) return false;
            var vessel = HeldBy(id); if (vessel == null || !vessel.Consume(id, now)) return false;
            ledger.RecordDrink(vessel.Origin + "/consume", id); uses.Enqueue(new CocktailUse(id, vessel, true)); return true;
        }
        public bool Offer(ulong id, ulong target, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible) || !ledger.IsConnected(target)) return false;
            return HeldBy(id)?.Offer(id, target, now) == true;
        }
        public bool Accept(ulong id, ulong vesselId, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible) || HeldBy(id) != null || Station.Owner == id) return false;
            return Find(vesselId)?.Accept(id, now) == true;
        }
        public bool PutDown(ulong id, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible)) return false;
            return HeldBy(id)?.PutDown(id, now) == true;
        }
        public bool Grab(ulong id, ulong vesselId, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible) || HeldBy(id) != null || Station.Owner == id) return false;
            return Find(vesselId)?.Grab(id, now) == true;
        }
        public bool Launch(ulong id, bool eligible, double now)
        {
            if (!Advance(now) || !Admitted(id, eligible)) return false;
            return HeldBy(id)?.Launch(id, now) == true;
        }
        public bool Impact(ulong vesselId, ulong? recipient, double now)
        {
            // A flight already released can complete during room draining, including after its thrower disconnects.
            if (!Advance(now) || ledger.Closed) return false;
            var vessel = Find(vesselId); if (vessel == null || !vessel.Impact(now)) return false;
            if (recipient.HasValue) uses.Enqueue(new CocktailUse(recipient.Value, vessel, false));
            return true;
        }
        public CocktailUse[] TakeUses()
        {
            var result = uses.ToArray(); uses.Clear(); return result;
        }
        public void RemovePlayer(ulong id)
        {
            Station.Cancel(id); foreach (var vessel in vessels.Values) if (vessel.Holder == id) vessel.Discard();
        }
        public void BeginClosing(double now)
        {
            if (!Advance(now) || Closing) return;
            Closing = true; Station.Cancel();
            foreach (var vessel in vessels.Values) if (vessel.Phase != CocktailVesselPhase.Flying) vessel.Discard();
            Tick(now);
        }
        public void Dispose()
        {
            Closing = true; Station.Cancel(); foreach (var vessel in vessels.Values) vessel.Discard(); vessels.Clear(); uses.Clear();
        }
    }
}
