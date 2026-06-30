using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Definicja receptury produkcji. ScriptableObject, edytowalny w Inspectorze.
/// Jeden plik .asset = jedna receptura.
/// </summary>
[CreateAssetMenu(fileName = "Recipe_", menuName = "Tycoon/Recipe Data")]
public class RecipeData : ScriptableObject
{
    [Header("Identyfikacja")]
    [Tooltip("Unikalny identyfikator, np. 'recipe_bread'")]
    public string id;

    [Tooltip("Nazwa wyświetlana w UI")]
    public string displayName;

    [Header("Składniki (input)")]
    public List<RecipeIngredient> inputs = new List<RecipeIngredient>();

    [Header("Produkt wyjściowy (output)")]
    [Tooltip("ID produktu który powstaje z tej receptury")]
    public string outputProductId;

    [Tooltip("Ile jednostek produktu powstaje na jeden cykl produkcji")]
    public float outputAmount = 1f;

    [Header("Czas produkcji")]
    [Tooltip("Ile ticków trwa jeden cykl produkcji")]
    public int productionTimeTicks = 1;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            Debug.LogWarning($"[RecipeData] '{name}': brak id!");

        if (string.IsNullOrEmpty(outputProductId))
            Debug.LogWarning($"[RecipeData] '{id}': brak outputProductId!");

        if (inputs.Count == 0)
            Debug.LogWarning($"[RecipeData] '{id}': brak składników!");
    }
}

/// <summary>
/// Pojedynczy składnik receptury — produkt + wymagana ilość.
/// </summary>
[System.Serializable]
public class RecipeIngredient
{
    public string productId;
    public float amount;
}