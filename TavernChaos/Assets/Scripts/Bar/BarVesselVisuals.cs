using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // Provisional glasses for every peer, driven only by public snapshot data.
    public sealed class BarVesselVisuals
    {
        private sealed class Visual { public GameObject obj; public Vector3 target; public bool placed; }
        private readonly Dictionary<ulong, Visual> visuals = new();
        private readonly Dictionary<CocktailKind, Material> materials = new();
        private Material MaterialFor(CocktailKind kind)
        {
            if (materials.TryGetValue(kind, out var m)) return m;
            Color c = kind switch {
                CocktailKind.Blue => new Color(.2f, .55f, 1f), CocktailKind.Volcano => new Color(1f, .35f, .1f), CocktailKind.Unstable => new Color(.7f, 1f, .2f),
                CocktailKind.Helium => new Color(.9f, .8f, 1f), CocktailKind.Poison => new Color(.3f, .05f, .35f), _ => new Color(.9f, .8f, .5f)
            };
            return materials[kind] = TavernWorld.Mat("Cocktail " + kind, c, 0, true);
        }
        public int Count => visuals.Count;
        public void Apply(BarView bar)
        {
            var alive = new HashSet<ulong>();
            if (bar?.vessels != null) foreach (var v in bar.vessels) {
                if (v.phase == CocktailVesselPhase.Spent) continue; alive.Add(v.id);
                if (!visuals.TryGetValue(v.id, out var visual)) {
                    float h = v.container == 1 ? .11f : .08f;
                    var obj = TavernWorld.Cylinder("Cocktail " + v.id, v.position, new Vector3(.11f, h, .11f), MaterialFor(v.kind), null, false);
                    visual = new Visual { obj = obj }; visuals.Add(v.id, visual);
                }
                visual.target = v.position; if (!visual.placed) { visual.obj.transform.position = v.position; visual.placed = true; }
            }
            var gone = new List<ulong>(); foreach (var key in visuals.Keys) if (!alive.Contains(key)) gone.Add(key);
            foreach (ulong key in gone) { if (visuals[key].obj) Object.Destroy(visuals[key].obj); visuals.Remove(key); }
        }
        public void Tick(float dt)
        {
            foreach (var v in visuals.Values) if (v.obj) v.obj.transform.position = Vector3.Lerp(v.obj.transform.position, v.target, Mathf.Clamp01(dt * 18f));
        }
        public void Clear() { foreach (var v in visuals.Values) if (v.obj) Object.Destroy(v.obj); visuals.Clear(); }
    }
}
