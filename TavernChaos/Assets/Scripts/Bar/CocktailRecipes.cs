using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TavernChaos
{
    public enum CocktailKind { FreeMix, Blue, Volcano, Unstable, Helium, Poison }
    public enum Ingredient { Vodka, Curacao, Ice, Tequila, HotSauce, Tabasco, Absinthe, Energy, Beer, Gin, MysterySoda }

    public sealed class CocktailRecipe
    {
        public CocktailKind Kind { get; }
        public string Name { get; }
        public ReadOnlyCollection<Ingredient> Ingredients { get; }
        public double MinimumShare { get; }
        public double Duration { get; }
        public int Intoxication { get; }

        public CocktailRecipe(CocktailKind kind, string name, Ingredient[] ingredients, double minimumShare, double duration, int intoxication)
        {
            if (kind <= CocktailKind.FreeMix || kind > CocktailKind.Poison || string.IsNullOrWhiteSpace(name)
                || ingredients == null || ingredients.Length < 1 || ingredients.Length > 4
                || !double.IsFinite(minimumShare) || minimumShare < 0 || minimumShare * ingredients.Length > 1 + 1e-6
                || !double.IsFinite(duration) || duration <= 0 || duration > 60 || intoxication < 0 || intoxication > 5)
                throw new ArgumentException("Invalid cocktail recipe");
            var unique = new HashSet<Ingredient>();
            foreach (var ingredient in ingredients)
                if (!CocktailMix.ValidIngredient((int)ingredient) || !unique.Add(ingredient)) throw new ArgumentException("Invalid or repeated ingredient");
            // These durations are explicit requirements, unlike the tunable Blue/Volcano durations.
            if (kind == CocktailKind.Unstable && duration != 5 || kind == CocktailKind.Helium && duration != 15
                || kind == CocktailKind.Poison && (duration != 10 || minimumShare != 0 || ingredients.Length != 4))
                throw new ArgumentException("Recipe contradicts the charter");
            if (kind == CocktailKind.Poison) foreach (var ingredient in ingredients)
                if (!CocktailMix.Strong(ingredient)) throw new ArgumentException("Poison requires four distinct strong liquors");
            Kind = kind; Name = name.Trim(); MinimumShare = minimumShare; Duration = duration; Intoxication = intoxication;
            Ingredients = Array.AsReadOnly((Ingredient[])ingredients.Clone());
        }
    }

    public sealed class CocktailMix
    {
        private readonly Dictionary<Ingredient, int> measures = new();
        public int Count => measures.Count;
        public int Total { get { int total = 0; foreach (int value in measures.Values) total += value; return total; } }
        public static bool ValidIngredient(int value) => value >= 0 && value <= (int)Ingredient.MysterySoda;
        public static bool Strong(Ingredient value) => value == Ingredient.Vodka || value == Ingredient.Tequila || value == Ingredient.Absinthe || value == Ingredient.Gin;
        public static bool Alcohol(Ingredient value) => Strong(value) || value == Ingredient.Curacao || value == Ingredient.Beer;
        public int Measures(Ingredient ingredient) => measures.TryGetValue(ingredient, out int value) ? value : 0;
        public double Share(Ingredient ingredient) => Total == 0 ? 0 : (double)Measures(ingredient) / Total;
        public bool Adjust(int ingredient, int delta)
        {
            if (!ValidIngredient(ingredient) || delta == 0 || delta < -10 || delta > 10) return false;
            var key = (Ingredient)ingredient; int before = Measures(key), after = before + delta;
            if (after < 0 || after > 10 || before == 0 && after > 0 && Count >= 4) return false;
            if (after == 0) measures.Remove(key); else measures[key] = after;
            return true;
        }
        public void Clear() => measures.Clear();
        public void Fill(CocktailRecipe recipe)
        {
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            Clear(); foreach (var ingredient in recipe.Ingredients) measures.Add(ingredient, 1);
        }
        public CocktailMix Copy()
        {
            var result = new CocktailMix(); foreach (var pair in measures) result.measures.Add(pair.Key, pair.Value); return result;
        }
        public int FreeMixIntoxication { get { foreach (var key in measures.Keys) if (Alcohol(key)) return 1; return 0; } }
    }

    public sealed class CocktailRecipes
    {
        public ReadOnlyCollection<CocktailRecipe> Entries { get; }
        public CocktailRecipes(IEnumerable<CocktailRecipe> entries)
        {
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var list = new List<CocktailRecipe>(entries); var kinds = new HashSet<CocktailKind>(); var names = new HashSet<string>();
            foreach (var recipe in list) {
                if (recipe == null || !names.Add(recipe.Name)) throw new ArgumentException("Invalid recipe catalog");
                kinds.Add(recipe.Kind);
                foreach (var other in list) if (other != null && other != recipe && SameIngredients(recipe, other))
                    throw new ArgumentException("Ambiguous recipe ingredients");
            }
            if (kinds.Count != 5 || list.Count > 32) throw new ArgumentException("Catalog must include all five charter effects");
            for (var kind = CocktailKind.Blue; kind <= CocktailKind.Poison; kind++) {
                var required = RequiredIngredients(kind); bool found = false;
                foreach (var recipe in list) if (recipe.Kind == kind && recipe.Ingredients.Count == required.Length) {
                    bool original = true; foreach (var ingredient in required) original &= recipe.Ingredients.Contains(ingredient);
                    found |= original;
                }
                if (!found) throw new ArgumentException("Catalog is missing an original charter combination: " + kind);
            }
            Entries = list.AsReadOnly();
        }
        private static Ingredient[] RequiredIngredients(CocktailKind kind) => kind switch {
            CocktailKind.Blue => new[] { Ingredient.Vodka, Ingredient.Curacao, Ingredient.Ice },
            CocktailKind.Volcano => new[] { Ingredient.Tequila, Ingredient.HotSauce, Ingredient.Tabasco },
            CocktailKind.Unstable => new[] { Ingredient.Absinthe, Ingredient.Energy, Ingredient.Beer },
            CocktailKind.Helium => new[] { Ingredient.Gin, Ingredient.MysterySoda },
            _ => new[] { Ingredient.Vodka, Ingredient.Tequila, Ingredient.Absinthe, Ingredient.Gin }
        };
        private static bool SameIngredients(CocktailRecipe a, CocktailRecipe b)
        {
            if (a.Ingredients.Count != b.Ingredients.Count) return false;
            foreach (var ingredient in a.Ingredients) if (!b.Ingredients.Contains(ingredient)) return false;
            return true;
        }
        public CocktailRecipe Match(CocktailMix mix)
        {
            if (mix == null || mix.Count == 0) return null;
            // Any four distinct strong liquors cause K.O., irrespective of their nonzero proportions.
            foreach (var recipe in Entries) {
                if (recipe.Ingredients.Count != mix.Count) continue;
                bool match = true;
                foreach (var ingredient in recipe.Ingredients)
                    match &= mix.Measures(ingredient) > 0 && mix.Share(ingredient) + 1e-6 >= recipe.MinimumShare;
                if (match) return recipe;
            }
            return null;
        }
        public static CocktailRecipes Default() => new(new[] {
            new CocktailRecipe(CocktailKind.Blue,"Laguna Azul",new[]{Ingredient.Vodka,Ingredient.Curacao,Ingredient.Ice},.1,8,1),
            new CocktailRecipe(CocktailKind.Volcano,"El Volcánico",new[]{Ingredient.Tequila,Ingredient.HotSauce,Ingredient.Tabasco},.1,3,1),
            new CocktailRecipe(CocktailKind.Unstable,"Mezcla Inestable",new[]{Ingredient.Absinthe,Ingredient.Energy,Ingredient.Beer},.1,5,2),
            new CocktailRecipe(CocktailKind.Helium,"Helio",new[]{Ingredient.Gin,Ingredient.MysterySoda},.2,15,1),
            new CocktailRecipe(CocktailKind.Poison,"Veneno / K.O.",new[]{Ingredient.Vodka,Ingredient.Tequila,Ingredient.Absinthe,Ingredient.Gin},0,10,5)
        });
    }
}
