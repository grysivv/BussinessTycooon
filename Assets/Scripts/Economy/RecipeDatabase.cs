using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralny rejestr wszystkich receptur w grze.
/// </summary>
[CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Tycoon/Recipe Database")]
public class RecipeDatabase : ScriptableObject
{
    [SerializeField] private List<RecipeData> _recipes = new List<RecipeData>();

    private Dictionary<string, RecipeData> _index;

    public RecipeData GetById(string id)
    {
        BuildIndexIfNeeded();

        if (_index.TryGetValue(id, out var recipe))
            return recipe;

        Debug.LogWarning($"[RecipeDatabase] Receptura '{id}' nie istnieje!");
        return null;
    }

    public IReadOnlyList<RecipeData> AllRecipes => _recipes;

    private void BuildIndexIfNeeded()
    {
        if (_index != null) return;

        _index = new Dictionary<string, RecipeData>();

        foreach (var recipe in _recipes)
        {
            if (recipe == null || string.IsNullOrEmpty(recipe.id))
                continue;

            if (_index.ContainsKey(recipe.id))
            {
                Debug.LogError($"[RecipeDatabase] Duplikat id: '{recipe.id}'!");
                continue;
            }

            _index[recipe.id] = recipe;
        }

        Debug.Log($"[RecipeDatabase] Zbudowano indeks: {_index.Count} receptur.");
    }

    public void RebuildIndex()
    {
        _index = null;
        BuildIndexIfNeeded();
    }

    private void OnValidate() => RebuildIndex();
}