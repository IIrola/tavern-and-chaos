using System;
using System.Collections;
using UnityEngine;

namespace TavernChaos
{
    // Drives the real intent protocol of the bar station for native-process checks (client or host peer standing at the bar).
    public static class BarFlowChecks
    {
        private static IEnumerator Until(Func<bool> condition, float seconds = 4)
        {
            double until = Time.realtimeSinceStartupAsDouble + seconds;
            while (Time.realtimeSinceStartupAsDouble < until && !condition()) yield return null;
        }
        public static BarVesselView Mine(DemoGame g) => g.Lan.View?.bar?.HeldBy(g.Lan.LocalId);
        // Opens the station, mixes a charter recipe, shakes and serves. Leaves the vessel in hand.
        public static IEnumerator Serve(DemoGame g, int recipe)
        {
            var lan = g.Lan; lan.Send(LanAction.Interact, yaw: 270, pitch: 0);
            yield return Until(() => lan.LocalBarOpen);
            if (!lan.LocalBarOpen) yield break;
            lan.Send(LanAction.BarContainer, choice: 0, proposal: lan.View.bar.revision);
            yield return Until(() => lan.View.bar.phase == BarPhase.Mixing);
            lan.Send(LanAction.BarRecipe, choice: recipe, proposal: lan.View.bar.revision);
            yield return Until(() => lan.View.bar.match.Length > 0);
            lan.Send(LanAction.BarShake, proposal: lan.View.bar.revision);
            yield return Until(() => lan.View.bar.phase == BarPhase.Ready, 5);
            lan.Send(LanAction.BarServe, proposal: lan.View.bar.revision);
            yield return Until(() => Mine(g) != null);
        }
        // Serve then drink at once.
        public static IEnumerator ServeAndDrink(DemoGame g, int recipe)
        {
            yield return Serve(g, recipe);
            if (Mine(g) == null) yield break;
            g.Lan.Send(LanAction.Interact, yaw: 270, pitch: 0);
            yield return Until(() => Mine(g) == null);
            yield return new WaitForSecondsRealtime(.15f);
        }
    }
}
