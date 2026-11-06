using System;
namespace TavernChaos
{
    public static class CocktailChecks
    {
        public static void Run(Action<bool, string> check)
        {
            var catalog = CocktailRecipes.Default(); var mix = new CocktailMix();
            check(catalog.Entries.Count == 5 && catalog.Match(mix) == null, "Bar: all five charter recipes exist and an empty container has no recipe");
            foreach (var recipe in catalog.Entries) {
                mix.Fill(recipe); check(catalog.Match(mix) == recipe, "Bar: original ingredients identify " + recipe.Name);
                var reversed = new CocktailMix(); for (int i = recipe.Ingredients.Count - 1; i >= 0; i--) reversed.Adjust((int)recipe.Ingredients[i], 1);
                check(catalog.Match(reversed) == recipe, "Bar: ingredient order cannot change " + recipe.Name);
            }
            mix.Clear(); check(!mix.Adjust(-1, 1) && !mix.Adjust(99, 1) && !mix.Adjust(0, 11) && mix.Count == 0, "Bar: invalid ingredients and quantities leave mixture unchanged");
            mix.Fill(catalog.Entries[0]); mix.Adjust((int)Ingredient.Vodka, 9);
            check(catalog.Match(mix) == null && mix.FreeMixIntoxication == 1, "Bar: missing minimum proportion is an explicitly alcoholic free mix");
            check(mix.Adjust((int)Ingredient.Curacao, 1) && mix.Adjust((int)Ingredient.Ice, 1) && catalog.Match(mix)?.Kind == CocktailKind.Blue, "Bar: changing proportions can restore the recipe without charging");
            mix.Fill(catalog.Entries[4]); mix.Adjust((int)Ingredient.Vodka, 9);
            check(catalog.Match(mix)?.Kind == CocktailKind.Poison, "Bar: four strong liquors cause K.O. at any nonzero proportions");
            check(!mix.Adjust((int)Ingredient.Ice, 1) && mix.Count == 4, "Bar: fifth distinct ingredient is rejected atomically");
            mix.Adjust((int)Ingredient.Gin, -1); check(mix.Count == 3 && mix.Adjust((int)Ingredient.Ice, 1), "Bar: removing an ingredient releases one slot");
            var copy = mix.Copy(); copy.Clear(); check(mix.Count == 4, "Bar: editing a captured mixture cannot mutate authority state");
            mix.Clear(); mix.Adjust((int)Ingredient.Ice, 1); check(mix.FreeMixIntoxication == 0 && catalog.Match(mix) == null, "Bar: alcohol-free mix invents no intoxication or special effect");
            var source = new[] { Ingredient.Gin, Ingredient.MysterySoda }; var immutable = new CocktailRecipe(CocktailKind.Helium, "Test", source, .2, 15, 1); source[0] = Ingredient.Ice;
            check(immutable.Ingredients[0] == Ingredient.Gin, "Bar: recipe freezes authoring ingredient arrays");
            bool rejected = false; try { new CocktailRecipe(CocktailKind.Unstable, "Bad", new[] { Ingredient.Absinthe }, .1, 6, 1); } catch (ArgumentException) { rejected = true; }
            check(rejected, "Bar: catalog cannot silently replace the required five-second instability");

            var station = new BarStation(); check(station.Begin(0, true, 0) && !station.Begin(1, true, 0), "Bar: station has one exclusive owner, including host identifier zero");
            check(!station.ChooseContainer(1, station.Revision, 0, .1) && station.Phase == BarPhase.Container, "Bar: another participant cannot choose the owner's container");
            check(station.ChooseContainer(0, station.Revision, 1, .1) && !station.Shake(0, station.Revision, .2), "Bar: container precedes mixture and empty shaking is rejected");
            uint old = station.Revision; check(station.Fill(0, old, catalog.Entries[0], .2) && !station.Adjust(0, old, 0, 1, .3), "Bar: an old proposal cannot alter a replaced mixture");
            check(station.Shake(0, station.Revision, .3), "Bar: nonempty mixture begins timed shaking");
            check(!station.Adjust(0, station.Revision, 0, 1, .4) && !station.CanServe(0, station.Revision, 1.049), "Bar: shaking cannot be edited or served before its endpoint");
            old = station.Revision; station.Tick(1.05); check(station.Phase == BarPhase.Ready && station.Revision != old && !station.CanServe(0, old, 1.05), "Bar: completed shaking publishes a new quote before serving");
            check(station.Adjust(0, station.Revision, 0, 1, 1.1) && station.Phase == BarPhase.Mixing && !station.CanServe(0, station.Revision, 1.1), "Bar: editing a shaken mixture requires shaking again");
            station.Shake(0, station.Revision, 1.2); station.Tick(2); old = station.Revision;
            check(station.CompleteServe(0, old, 2) && !station.CompleteServe(0, old, 2) && station.Owner == null && station.Mix.Count == 0, "Bar: serving consumes exactly one prepared mixture and frees the station");
            station.Begin(4, true, 3); check(!station.Cancel(3) && station.Cancel(4) && station.Phase == BarPhase.Available, "Bar: cancellation is free and only owner or authority can cancel");
            check(!station.Begin(4, true, double.NaN) && !station.Begin(4, true, 1), "Bar: nonfinite or backward station clocks are rejected");

            var effects = new CocktailEffects(); var blue = catalog.Entries[0]; var unstable = catalog.Entries[2]; var helium = catalog.Entries[3]; var poison = catalog.Entries[4];
            check(effects.Apply(blue, "vessel/1", 0) && !effects.Apply(blue, "vessel/1", 1) && effects.Remaining(CocktailKind.Blue) == 7, "Bar: duplicate active origin cannot extend its own effect");
            check(effects.Apply(blue, "vessel/2", 2) && effects.Remaining(CocktailKind.Blue) == 8 && effects.Capture().Length == 1, "Bar: another blue cocktail renews rather than stacks");
            effects.Apply(unstable, "vessel/3", 3); effects.Apply(helium, "vessel/4", 3); effects.Apply(poison, "vessel/5", 3);
            check(effects.Capture().Length == 4 && effects.SpeedMultiplier == 1.5 && effects.GravityMultiplier == .2 && effects.Frictionless, "Bar: different effects coexist with fixed bounded modifiers");
            effects.OnFall(4); check(!effects.Frictionless && effects.SpeedMultiplier == 1 && effects.GravityMultiplier == .2 && effects.Has(CocktailKind.Poison), "Bar: falling clears sliding and instability while preserving gravity and smoke effects");
            effects.Tick(9); check(!effects.TakeUnstableFall(), "Bar: cancelled instability cannot schedule a second fall");
            effects.Tick(13); check(!effects.Has(CocktailKind.Poison) && effects.GravityMultiplier == .2, "Bar: smoke expiry restores only its own effect");
            effects.Tick(18); check(effects.GravityMultiplier == 1 && effects.Capture().Length == 0, "Bar: helium expires without global or permanent gravity changes");
            effects.Apply(unstable, "vessel/6", 19); effects.Apply(blue, "vessel/7", 24);
            check(effects.TakeUnstableFall() && !effects.TakeUnstableFall(), "Bar: applying another effect at expiry cannot swallow the scheduled instability fall");
            check(effects.Frictionless && !effects.Apply(helium, "vessel/8", 23) && !effects.Apply(helium, "vessel/8", double.PositiveInfinity), "Bar: backward and nonfinite effect clocks do not mutate modifiers");
            effects.Clear(0); check(effects.Capture().Length == 0 && !effects.TakeUnstableFall(), "Bar: another room clears timers and delayed falls together");

            var vessel = new ServedCocktail(1, 0, blue, 0, 0, 0);
            check(!vessel.Consume(1, 1) && vessel.Holder == 0, "Bar: only the authoritative holder can consume a vessel");
            check(vessel.Offer(0, 1, 1) && !vessel.Accept(2, 1) && vessel.Accept(1, 2) && vessel.Holder == 1, "Bar: offering requires the selected recipient to accept ownership");
            check(!vessel.Consume(0, 2) && vessel.Consume(1, 2) && !vessel.Consume(1, 2) && !vessel.Impact(2), "Bar: consumed vessel cannot also apply an impact or a second consumption");
            vessel = new ServedCocktail(2, 0, blue, 0, 0, 0); vessel.Offer(0, 1, 1);
            check(!vessel.Accept(1, 4) && vessel.Holder == 0, "Bar: expired offer cannot transfer a vessel");
            check(vessel.PutDown(0, 4) && vessel.Grab(2, 5) && !vessel.Grab(1, 5), "Bar: resting vessel can be picked up once without another service charge");
            check(vessel.Launch(2, 6) && vessel.Thrower == 2 && !vessel.Consume(2, 6) && vessel.Impact(7) && !vessel.Impact(7), "Bar: launched vessel has one impact, including simultaneous collision callbacks");
            vessel = new ServedCocktail(3, 0, null, 1, 1, 0); check(vessel.Kind == CocktailKind.FreeMix && vessel.Intoxication == 1, "Bar: free mixture freezes alcohol result without inventing a recipe");
            vessel.Launch(0, 28); check(vessel.FlightEnds == 30 && !vessel.Tick(30) && !vessel.Impact(30), "Bar: launch cannot extend the thirty-second vessel lifetime");
            vessel = new ServedCocktail(4, 0, poison, 0, 0, 0); vessel.Launch(0, 1);
            check(!vessel.Tick(7) && vessel.Phase == CocktailVesselPhase.Spent, "Bar: unfinished flight is bounded to six seconds");
            vessel = new ServedCocktail(5, 0, blue, 0, 0, 0); vessel.Discard();
            check(!vessel.Grab(0, 1) && !vessel.Consume(0, 1) && !vessel.Launch(0, 1), "Bar: discarded room vessel cannot be revived or reused");
        }
    }
}
