using System;
using UnityEngine;

namespace TavernChaos
{
    [CreateAssetMenu(fileName = "CocktailCatalog", menuName = "Tavern/Cocktail catalog")]
    public sealed class CocktailCatalog : ScriptableObject
    {
        [Serializable] public sealed class Entry
        {
            public CocktailKind kind;
            public string name;
            public Ingredient[] ingredients;
            [Range(0, 1)] public float minimumShare;
            [Min(.1f)] public float duration;
            [Range(0, 5)] public int intoxication;
        }
        [SerializeField] private Entry[] entries = DefaultEntries();
        private static Entry[] DefaultEntries()
        {
            var recipes = CocktailRecipes.Default().Entries; var result = new Entry[recipes.Count];
            for (int i = 0; i < result.Length; i++) {
                var recipe = recipes[i]; var ingredients = new Ingredient[recipe.Ingredients.Count]; recipe.Ingredients.CopyTo(ingredients, 0);
                result[i] = new Entry { kind = recipe.Kind, name = recipe.Name, ingredients = ingredients, minimumShare = (float)recipe.MinimumShare, duration = (float)recipe.Duration, intoxication = recipe.Intoxication };
            }
            return result;
        }
        // Freeze authoring values once per room. No player, vessel, money or clock state lives in this asset.
        public CocktailRecipes Snapshot()
        {
            if (entries == null) throw new InvalidOperationException("Missing cocktail catalog");
            var result = new CocktailRecipe[entries.Length];
            for (int i = 0; i < result.Length; i++) {
                var entry = entries[i]; if (entry == null) throw new InvalidOperationException("Missing cocktail recipe");
                result[i] = new CocktailRecipe(entry.kind, entry.name, entry.ingredients, entry.minimumShare, entry.duration, entry.intoxication);
            }
            return new CocktailRecipes(result);
        }
    }
}
