using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace TavernChaos.Editor
{
    public static class BarValidation
    {
        [Serializable] private sealed class Report { public bool passed; public List<string> checks = new(), failures = new(); }
        [MenuItem("Tavern/Validate cocktail rules")]
        public static void Run()
        {
            var report = new Report(); CocktailChecks.Run((ok, name) => (ok ? report.checks : report.failures).Add(name));
            BarServiceChecks.Run((ok, name) => (ok ? report.checks : report.failures).Add(name));
            void Check(bool ok, string name) => (ok ? report.checks : report.failures).Add(name);
            const string catalogPath = "Assets/Resources/CocktailCatalog.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CocktailCatalog>(catalogPath);
            if (!asset) {
                Directory.CreateDirectory("Assets/Resources");
                asset = ScriptableObject.CreateInstance<CocktailCatalog>();
                AssetDatabase.CreateAsset(asset, catalogPath); AssetDatabase.SaveAssets();
            }
            Check(asset.Snapshot().Entries.Count == 5, "Bar: project owns an editable catalog asset with all original recipes");
            var transient = ScriptableObject.CreateInstance<CocktailCatalog>();
            try {
                var frozen = transient.Snapshot(); var mix = new CocktailMix();
                mix.Adjust((int)Ingredient.Vodka, 8); mix.Adjust((int)Ingredient.Curacao, 1); mix.Adjust((int)Ingredient.Ice, 1);
                Check(frozen.Match(mix)?.Kind == CocktailKind.Blue, "Bar: serialized float accepts an exact ten-percent recipe boundary");
                mix.Clear(); mix.Adjust((int)Ingredient.Gin, 1); mix.Adjust((int)Ingredient.MysterySoda, 4);
                Check(frozen.Match(mix)?.Kind == CocktailKind.Helium, "Bar: serialized float accepts an exact twenty-percent recipe boundary");
                var serialized = new SerializedObject(transient);
                serialized.FindProperty("entries").GetArrayElementAtIndex(0).FindPropertyRelative("duration").floatValue = 9;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Check(frozen.Entries[0].Duration == 8 && transient.Snapshot().Entries[0].Duration == 9, "Bar: a room snapshot remains fixed when the authoring asset changes");
            } finally { UnityEngine.Object.DestroyImmediate(transient); }
            report.passed = report.failures.Count == 0; Directory.CreateDirectory("../Evidence/bar");
            File.WriteAllText("../Evidence/bar/rules.json", JsonUtility.ToJson(report, true));
            var ledger = new Report(); SessionLedgerChecks.Run((ok, name) => (ok ? ledger.checks : ledger.failures).Add(name));
            ledger.passed = ledger.failures.Count == 0;
            File.WriteAllText("../Evidence/bar/ledger-regression.json", JsonUtility.ToJson(ledger, true));
            if (!ledger.passed) throw new Exception("Ledger regression failed: " + string.Join("; ", ledger.failures));
            if (!report.passed) throw new Exception("Cocktail checks failed: " + string.Join("; ", report.failures));
            Debug.Log("BAR_RULES_OK: " + report.checks.Count);
        }
    }
}
