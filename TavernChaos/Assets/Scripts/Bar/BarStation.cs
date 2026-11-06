using System;

namespace TavernChaos
{
    public enum BarPhase { Available, Container, Mixing, Shaking, Ready }
    // Pure preparation only. The room commits money and a staged vessel before CompleteServe.
    public sealed class BarStation
    {
        public const double ShakeDuration = .75;
        public ulong? Owner { get; private set; }
        public BarPhase Phase { get; private set; }
        public uint Revision { get; private set; } = 1;
        public int Container { get; private set; }
        public double ReadyAt { get; private set; }
        private double time;
        private readonly CocktailMix mix = new();
        public CocktailMix Mix => mix.Copy();
        private void Changed() => Revision = Revision == uint.MaxValue ? 1 : Revision + 1;
        private bool Valid(ulong id, uint revision, double now) => Owner == id && revision == Revision && double.IsFinite(now) && now >= time;
        public bool Begin(ulong id, bool eligible, double now)
        {
            if (!eligible || Owner.HasValue || !double.IsFinite(now) || now < time) return false;
            time = now; Owner = id; Phase = BarPhase.Container; mix.Clear(); Container = 0; ReadyAt = 0; Changed(); return true;
        }
        public bool ChooseContainer(ulong id, uint revision, int container, double now)
        {
            if (!Valid(id, revision, now) || Phase != BarPhase.Container || container < 0 || container > 1) return false;
            time = now; Container = container; Phase = BarPhase.Mixing; Changed(); return true;
        }
        public bool Adjust(ulong id, uint revision, int ingredient, int delta, double now)
        {
            if (!Valid(id, revision, now) || Phase != BarPhase.Mixing && Phase != BarPhase.Ready || !mix.Adjust(ingredient, delta)) return false;
            time = now; Phase = BarPhase.Mixing; ReadyAt = 0; Changed(); return true;
        }
        public bool Fill(ulong id, uint revision, CocktailRecipe recipe, double now)
        {
            if (!Valid(id, revision, now) || recipe == null || Phase != BarPhase.Mixing && Phase != BarPhase.Ready) return false;
            time = now; mix.Fill(recipe); Phase = BarPhase.Mixing; ReadyAt = 0; Changed(); return true;
        }
        public bool Shake(ulong id, uint revision, double now)
        {
            if (!Valid(id, revision, now) || Phase != BarPhase.Mixing || mix.Count == 0) return false;
            time = now; ReadyAt = now + ShakeDuration; Phase = BarPhase.Shaking; Changed(); return true;
        }
        public void Tick(double now)
        {
            if (!double.IsFinite(now) || now < time) return;
            time = now; if (Phase == BarPhase.Shaking && now >= ReadyAt) { Phase = BarPhase.Ready; Changed(); }
        }
        public bool CanServe(ulong id, uint revision, double now)
        {
            if (!Valid(id, revision, now)) return false;
            Tick(now); return revision == Revision && Phase == BarPhase.Ready && mix.Count > 0;
        }
        public bool CompleteServe(ulong id, uint revision, double now)
        {
            if (!CanServe(id, revision, now)) return false;
            Cancel(); return true;
        }
        public bool Cancel(ulong id) { if (Owner != id) return false; Cancel(); return true; }
        public void Cancel() { Owner = null; mix.Clear(); ReadyAt = 0; Phase = BarPhase.Available; Changed(); }
    }
}
