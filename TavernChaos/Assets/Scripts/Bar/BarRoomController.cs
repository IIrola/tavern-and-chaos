using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // Host-side bridge between the pure BarService and the physical room: validates proximity and aim,
    // resolves thrown vessels and applies committed uses to actors. Clients only send intents.
    public sealed class BarRoomController
    {
        public const float Reach = 2.6f, GiveReach = 3f, MaxRange = 9f, FlightSpeed = 14f, VesselRestReach = 2.4f;
        private sealed class Flight { public ulong vessel; public Vector3 from, to; public double start, end; public ulong? target; }
        public BarService Service { get; }
        public bool HasFlights => flights.Count > 0;
        public event Action<CocktailUse, TavernActor> Used;
        public event Action<string, ulong> Notice;
        private readonly Func<ulong, TavernActor> actorFor;
        private readonly Func<IEnumerable<KeyValuePair<ulong, TavernActor>>> people;
        private readonly Dictionary<ulong, Vector3> rest = new();
        private readonly List<Flight> flights = new();

        public BarRoomController(SessionLedger ledger, CocktailRecipes recipes, long price,
            Func<ulong, TavernActor> actorFor, Func<IEnumerable<KeyValuePair<ulong, TavernActor>>> people)
        {
            Service = new BarService(ledger, recipes, price); this.actorFor = actorFor; this.people = people;
        }
        public static bool Near(Vector3 position) { var d = position - TavernWorld.BarPoint; d.y = 0; return d.magnitude < Reach; }
        private static bool Eligible(TavernActor actor) => actor != null && !actor.Fallen && !actor.Seated;

        // Returns true when the interact press was consumed by the bar (drink, accept, grab or start preparing).
        public bool TryInteract(ulong id, TavernActor actor, double now)
        {
            if (!Eligible(actor)) return false;
            if (Service.HeldBy(id) != null) return Service.Consume(id, true, now);
            foreach (var vessel in Service.Capture())
                if (vessel.Phase == CocktailVesselPhase.Held && vessel.OfferedTo == id) return Service.Accept(id, vessel.Id, true, now);
            ServedCocktail best = null; float bestDistance = VesselRestReach;
            foreach (var vessel in Service.Capture()) {
                if (vessel.Phase != CocktailVesselPhase.Resting || !rest.TryGetValue(vessel.Id, out var at)) continue;
                float d = Vector3.Distance(actor.transform.position, at); if (d < bestDistance) { best = vessel; bestDistance = d; }
            }
            if (best != null) return Service.Grab(id, best.Id, true, now);
            if (!Near(actor.transform.position)) return false;
            if (Service.Station.Owner == id) return true;
            if (Service.Station.Owner.HasValue) { Notice?.Invoke("La barra está ocupada por otra persona.", id); return true; }
            return Service.Begin(id, true, now);
        }

        public bool Handle(ulong id, LanIntent intent, TavernActor actor, Vector3 aim, double now)
        {
            bool eligible = Eligible(actor); var station = Service.Station;
            switch (intent.action) {
                case LanAction.BarContainer: return eligible && Near(actor.transform.position) && station.ChooseContainer(id, intent.proposal, intent.choice, now);
                case LanAction.BarAdjust: return eligible && Near(actor.transform.position) && station.Adjust(id, intent.proposal, intent.choice, intent.page, now);
                case LanAction.BarRecipe: {
                    var entries = Service.Recipes.Entries;
                    return eligible && Near(actor.transform.position) && intent.choice >= 0 && intent.choice < entries.Count && station.Fill(id, intent.proposal, entries[intent.choice], now);
                }
                case LanAction.BarShake: return eligible && Near(actor.transform.position) && station.Shake(id, intent.proposal, now);
                case LanAction.BarServe: {
                    if (!eligible || !Near(actor.transform.position)) return false;
                    var vessel = Service.Serve(id, intent.proposal, true, now);
                    if (vessel == null) { Notice?.Invoke("No se pudo servir: comprueba tu saldo o el espacio de la barra.", id); return false; }
                    Notice?.Invoke("Servido por " + Service.Price + " fichas. E para beber, F para ofrecer, clic para lanzar.", id); return true;
                }
                case LanAction.BarCancel: return station.Cancel(id);
                case LanAction.BarPutDown: return PutDown(id, actor, now);
                case LanAction.BarGrab: return Service.Find(intent.target) is { } v && v.Phase == CocktailVesselPhase.Resting
                    && rest.TryGetValue(v.Id, out var at) && Vector3.Distance(actor.transform.position, at) < VesselRestReach && Service.Grab(id, v.Id, eligible, now);
                case LanAction.BarAccept: return Service.Find(intent.target) is { } o && o.Holder is { } holder && actorFor(holder) is { } h
                    && Vector3.Distance(h.transform.position, actor.transform.position) < GiveReach && Service.Accept(id, o.Id, eligible, now);
                case LanAction.BarOffer: {
                    if (!eligible) return false;
                    ulong? target = Pick(id, actor, aim, GiveReach, 50f, includeNpc: false); if (!target.HasValue) { Notice?.Invoke("Nadie al alcance delante de ti.", id); return false; }
                    return Service.Offer(id, target.Value, true, now);
                }
            }
            return false;
        }

        public bool PutDown(ulong id, TavernActor actor, double now)
        {
            var vessel = Service.HeldBy(id); if (vessel == null || actor == null || actor.Seated) return false;
            if (!Service.PutDown(id, true, now)) return false;
            bool bar = Near(actor.transform.position);
            rest[vessel.Id] = actor.transform.position + actor.transform.forward * .7f + Vector3.up * (bar ? 1.52f : .95f); return true;
        }

        private ulong? Pick(ulong self, TavernActor actor, Vector3 aim, float range, float maxAngle, bool includeNpc)
        {
            ulong? best = null; float bestAngle = maxAngle;
            foreach (var pair in people()) {
                if (pair.Key == self || pair.Value == null || pair.Value.Fallen && range > GiveReach) continue;
                if (!includeNpc && pair.Key == ulong.MaxValue) continue;
                Vector3 d = pair.Value.Focus - actor.Focus; float dist = d.magnitude; if (dist > range) continue;
                float angle = Vector3.Angle(aim, d); if (angle < bestAngle) { best = pair.Key; bestAngle = angle; }
            }
            return best;
        }

        public bool Launch(ulong id, TavernActor actor, Vector3 aim, double now)
        {
            var vessel = Service.HeldBy(id); if (vessel == null || !Eligible(actor)) return false;
            ulong? target = Pick(id, actor, aim, MaxRange, 20f, includeNpc: true);
            Vector3 from = actor.Focus + aim * .4f, to = target.HasValue && actorFor(target.Value) is { } t ? t.Focus : actor.Focus + aim * 7f;
            if (!Service.Launch(id, true, now)) return false;
            double travel = Math.Max(.15, Vector3.Distance(from, to) / FlightSpeed);
            flights.Add(new Flight { vessel = vessel.Id, from = from, to = to, start = now, end = now + travel, target = target }); return true;
        }

        public void Fell(ulong id, double now) { Service.Station.Cancel(id); var held = Service.HeldBy(id); if (held != null) { var actor = actorFor(id); if (actor != null) PutDown(id, actor, now); else Service.RemovePlayer(id); } }
        public void Remove(ulong id) { Service.RemovePlayer(id); }

        public void Tick(double now)
        {
            Service.Tick(now);
            var owner = Service.Station.Owner;
            if (owner.HasValue) { var actor = actorFor(owner.Value); if (actor == null || actor.Fallen || actor.Seated || !Near(actor.transform.position)) Service.Station.Cancel(owner.Value); }
            for (int i = flights.Count - 1; i >= 0; i--) if (now >= flights[i].end) { Service.Impact(flights[i].vessel, flights[i].target, now); flights.RemoveAt(i); }
            foreach (var use in Service.TakeUses()) {
                var actor = actorFor(use.Recipient);
                if (actor != null) actor.ApplyCocktail(use.Vessel.Recipe, use.Vessel.Origin, use.Vessel.Intoxication);
                Used?.Invoke(use, actor);
            }
            var stale = new List<ulong>(); foreach (var key in rest.Keys) if (Service.Find(key) == null) stale.Add(key);
            foreach (ulong key in stale) rest.Remove(key);
        }

        public BarView Capture(double now)
        {
            var station = Service.Station; var mix = station.Mix; var measures = new int[11];
            for (int i = 0; i < measures.Length; i++) measures[i] = mix.Measures((Ingredient)i);
            var list = new List<BarVesselView>();
            foreach (var v in Service.Capture()) {
                if (v.Phase == CocktailVesselPhase.Spent) continue;
                Vector3 position = Vector3.zero;
                if (v.Phase == CocktailVesselPhase.Held && v.Holder is { } holder && actorFor(holder) is { } a) position = a.transform.position + Vector3.up * 1.15f + a.transform.forward * .45f;
                else if (v.Phase == CocktailVesselPhase.Resting && rest.TryGetValue(v.Id, out var at)) position = at;
                else if (v.Phase == CocktailVesselPhase.Flying) foreach (var f in flights) if (f.vessel == v.Id) {
                    float t = Mathf.Clamp01((float)((now - f.start) / Math.Max(.01, f.end - f.start)));
                    position = Vector3.Lerp(f.from, f.to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .5f; break;
                }
                list.Add(new BarVesselView {
                    id = v.Id, hasHolder = v.Holder.HasValue, holder = v.Holder ?? 0, hasOffer = v.OfferedTo.HasValue, offeredTo = v.OfferedTo ?? 0,
                    phase = v.Phase, kind = v.Kind, name = v.Recipe?.Name ?? "Mezcla libre", intoxication = v.Intoxication, container = v.Container,
                    remaining = (float)Math.Max(0, v.Expires - now), position = position
                });
            }
            return new BarView {
                hasOwner = station.Owner.HasValue, owner = station.Owner ?? 0, phase = station.Phase, revision = station.Revision, container = station.Container,
                measures = measures, match = mix.Count == 0 ? "" : Service.Recipes.Match(mix)?.Name ?? "Mezcla libre", readyAt = station.ReadyAt, price = Service.Price, vessels = list.ToArray()
            };
        }
        public void BeginClosing(double now) => Service.BeginClosing(now);
        public void Dispose() { flights.Clear(); rest.Clear(); Service.Dispose(); }
    }
}
