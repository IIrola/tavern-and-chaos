using System;
namespace TavernChaos
{
    public static class BarServiceChecks
    {
        private static void Prepare(BarService bar, ulong id, CocktailRecipe recipe, ref double now)
        {
            bar.Begin(id, true, now); bar.Station.ChooseContainer(id, bar.Station.Revision, 0, now);
            bar.Station.Fill(id, bar.Station.Revision, recipe, now); bar.Station.Shake(id, bar.Station.Revision, now);
            now += BarStation.ShakeDuration; bar.Tick(now);
        }
        public static void Run(Action<bool, string> check)
        {
            var recipes = CocktailRecipes.Default(); var ledger = new SessionLedger(100);
            ledger.Join(0, "Host", "host"); ledger.Join(1, "Peer", "peer"); var bar = new BarService(ledger, recipes, 20); double now = 0;
            Prepare(bar, 0, recipes.Entries[0], ref now);
            check(ledger.Balance(0) == 100 && ledger.BarRevenue == 0 && ledger.Find(0).Drinks == 0, "Bar service: preparing and shaking neither charges nor records consumption");
            uint revision = bar.Station.Revision;
            check(bar.Serve(0, revision, false, now) == null && bar.Station.Phase == BarPhase.Ready && ledger.Balance(0) == 100, "Bar service: failed geometry/state or missing staged physical capacity costs nothing");
            var vessel = bar.Serve(0, revision, true, now);
            check(vessel != null && ledger.Balance(0) == 80 && ledger.BarRevenue == 20 && ledger.Find(0).Drinks == 0 && ledger.AuditDelta == 0, "Bar service: serving commits one vessel and twenty service chips, without inventing a consumed drink");
            check(bar.Serve(0, revision, true, now) == null && ledger.Balance(0) == 80 && !bar.Begin(0, true, now), "Bar service: repeated serve or preparation with a held glass cannot charge again");
            check(bar.Offer(0, 1, true, now) && !bar.Consume(1, true, now) && bar.Accept(1, vessel.Id, true, now), "Bar service: recipient accepts a held offer before gaining consumption authority");
            check(bar.Consume(1, true, now) && !bar.Consume(1, true, now) && ledger.Find(1).Drinks == 1 && ledger.Find(0).Drinks == 0 && ledger.Balance(1) == 100, "Bar service: recipient consumption records once without moving the original service charge");
            var uses = bar.TakeUses(); check(uses.Length == 1 && uses[0].Consumed && uses[0].Recipient == 1 && uses[0].Vessel.Id == vessel.Id && bar.TakeUses().Length == 0, "Bar service: committed consumption publishes one application event with its original vessel");
            check(ledger.RecordDrink(vessel.Origin + "/consume", 1) == LedgerResult.Replayed && ledger.RecordDrink(vessel.Origin + "/consume", 0) == LedgerResult.Rejected && ledger.Find(1).Drinks == 1, "Bar service: duplicate consumption cannot change recipient or increment statistics twice");
            bar.Tick(now); check(!bar.HasPending && bar.VesselCount == 0, "Bar service: used glass releases bounded capacity after the application event drains");
            Prepare(bar, 0, recipes.Entries[1], ref now); bar.Station.Cancel(0);
            check(ledger.Balance(0) == 80 && ledger.BarRevenue == 20 && bar.Station.Owner == null, "Bar service: cancelling a fully prepared second drink is free");

            var poor = new SessionLedger(5); poor.Join(0, "Poor", "p"); var poorBar = new BarService(poor, recipes, 20); now = 0;
            Prepare(poorBar, 0, recipes.Entries[0], ref now);
            check(poorBar.Serve(0, poorBar.Station.Revision, true, now) == null && poor.Balance(0) == 5 && poorBar.Station.Phase == BarPhase.Ready && poorBar.VesselCount == 0, "Bar service: insufficient funds preserves the quoted mixture without creating an unpaid vessel");

            ledger = new SessionLedger(1000); ledger.Join(0, "Host", "h"); ledger.Join(1, "Peer", "p"); bar = new BarService(ledger, recipes, 20); now = 0;
            for (int i = 0; i < BarService.Capacity; i++) { Prepare(bar, 0, recipes.Entries[i % 5], ref now); bar.Serve(0, bar.Station.Revision, true, now); bar.PutDown(0, true, now); }
            Prepare(bar, 0, recipes.Entries[0], ref now); long before = ledger.Balance(0);
            check(bar.VesselCount == 12 && bar.Serve(0, bar.Station.Revision, true, now) == null && ledger.Balance(0) == before, "Bar service: thirteenth live glass is rejected before any debit");
            now = 40; bar.Tick(now); check(bar.VesselCount == 0 && bar.Serve(0, bar.Station.Revision, true, now) != null && ledger.Balance(0) == before - 20, "Bar service: expiry releases capacity without refunding already-served drinks");
            bar.Dispose(); check(!bar.HasPending && bar.TakeUses().Length == 0 && !bar.Begin(0, true, now) && ledger.AuditDelta == 0, "Bar service: disposal clears objects and events while preserving balanced paid services");

            ledger = new SessionLedger(100); ledger.Join(0, "Host", "h"); ledger.Join(1, "Peer", "p"); bar = new BarService(ledger, recipes, 20); now = 0;
            Prepare(bar, 0, recipes.Entries[3], ref now); vessel = bar.Serve(0, bar.Station.Revision, true, now); bar.Launch(0, true, now);
            Prepare(bar, 1, recipes.Entries[4], ref now); var held = bar.Serve(1, bar.Station.Revision, true, now);
            bar.BeginClosing(now);
            check(bar.Closing && bar.Station.Phase == BarPhase.Available && bar.Find(vessel.Id)?.Phase == CocktailVesselPhase.Flying && bar.Find(held.Id) == null && ledger.BarRevenue == 40, "Bar service: closure discards held purchases without refund and preserves only released flight");
            check(!bar.Begin(0, true, now) && !bar.Consume(1, true, now) && !bar.Grab(1, vessel.Id, true, now), "Bar service: closing rejects new preparation, consumption and ownership changes");
            check(bar.Impact(vessel.Id, 1, now) && !bar.Impact(vessel.Id, 1, now) && bar.HasPending, "Bar service: released flight resolves once during draining and its pending application still blocks finalization");
            uses = bar.TakeUses(); bar.Tick(now);
            check(uses.Length == 1 && !uses[0].Consumed && uses[0].Recipient == 1 && ledger.Find(1).Drinks == 0 && !bar.HasPending && ledger.AuditDelta == 0, "Bar service: splash applies its recipe without inventing a voluntary drink or leaking chips");

            ledger = new SessionLedger(100); ledger.Join(0, "Host", "h"); ledger.Join(1, "Peer", "p");
            bar = new BarService(ledger, recipes, 20); now = 0; Prepare(bar, 0, recipes.Entries[0], ref now); vessel = bar.Serve(0, bar.Station.Revision, true, now);
            bar.Launch(0, true, now); bar.RemovePlayer(0); ledger.Leave(0);
            check(bar.Find(vessel.Id)?.Phase == CocktailVesselPhase.Flying && bar.Impact(vessel.Id, 1, now) && bar.TakeUses().Length == 1 && ledger.AuditDelta == 0, "Bar service: disconnecting thrower cannot erase a paid flight already released");
            ledger.Close(); check(!bar.Begin(1, true, now) && ledger.AuditDelta == 0 && ledger.Balance(1) == 0, "Bar service: closed ledger admits no new drinks and destroys remaining room funds consistently");
        }
    }
}
