using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public readonly struct CocktailEffect
    {
        public readonly CocktailKind Kind;
        public readonly string Origin;
        public readonly double Start, End;
        public CocktailEffect(CocktailKind kind, string origin, double start, double end) { Kind = kind; Origin = origin; Start = start; End = end; }
    }

    // Authority clock and modifiers. Never mutates a Rigidbody, transform, global gravity or asset.
    public sealed class CocktailEffects
    {
        private readonly Dictionary<CocktailKind, CocktailEffect> active = new();
        private double time;
        private bool unstableFallPending;
        public bool Has(CocktailKind kind) => active.ContainsKey(kind);
        public double Remaining(CocktailKind kind) => active.TryGetValue(kind, out var effect) ? Math.Max(0, effect.End - time) : 0;
        public CocktailEffect[] Capture()
        {
            var result = new List<CocktailEffect>();
            for (var kind = CocktailKind.Blue; kind <= CocktailKind.Poison; kind++) if (active.TryGetValue(kind, out var effect)) result.Add(effect);
            return result.ToArray();
        }
        public bool Apply(CocktailRecipe recipe, string origin, double now)
        {
            if (recipe == null || string.IsNullOrWhiteSpace(origin) || !Valid(now)) return false;
            Tick(now);
            if (active.TryGetValue(recipe.Kind, out var before) && before.Origin == origin) return false;
            active[recipe.Kind] = new CocktailEffect(recipe.Kind, origin, now, now + recipe.Duration);
            return true;
        }
        public bool Tick(double now)
        {
            if (!Valid(now)) return false;
            time = now; bool unstableExpired = false;
            foreach (var effect in Capture()) if (now >= effect.End) {
                active.Remove(effect.Kind); unstableExpired |= effect.Kind == CocktailKind.Unstable;
            }
            unstableFallPending |= unstableExpired;
            return unstableExpired;
        }
        public bool TakeUnstableFall() { bool pending = unstableFallPending; unstableFallPending = false; return pending; }
        private bool Valid(double now) => double.IsFinite(now) && now >= time;
        public double SpeedMultiplier => Has(CocktailKind.Unstable) ? 1.5 : 1;
        public double GravityMultiplier => Has(CocktailKind.Helium) ? .2 : 1;
        public bool Frictionless => Has(CocktailKind.Blue);
        public void OnFall(double now)
        {
            if (!Valid(now)) return;
            Tick(now); active.Remove(CocktailKind.Blue); active.Remove(CocktailKind.Unstable); unstableFallPending = false;
        }
        public void Clear(double now)
        {
            if (!double.IsFinite(now)) return;
            active.Clear(); unstableFallPending = false; time = now;
        }
    }
}
