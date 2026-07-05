using UnityEngine;

/// <summary>
/// Dane typu budynku — szablon do stawiania.
/// ScriptableObject, edytowalny w Inspectorze.
/// Jeden plik .asset = jeden typ budynku (np. "Farma Pszenicy").
/// </summary>
[CreateAssetMenu(fileName = "Building_", menuName = "Tycoon/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Identyfikacja")]
    public string id;
    public string displayName;

    [Header("Wizualizacja")]
    public Sprite icon;           // ikona w Build Menu
    public GameObject prefab;     // prefab do Instantiate na scenie

    [Header("Rozmiar")]
    public Vector2Int footprint = Vector2Int.one;

    [Header("Ekonomia")]
    public float constructionCost;
    public float demolitionCost;
    public float monthlyMaintenance;
    public float monthlyWages = 100f; // jeśli budynek wymaga pracowników

    [Header("Produkcja (jeśli dotyczy)")]
    public RecipeData recipe;
    public float storageCapacity = 100f;

    [Header("Kategoria")]
    public BuildingCategory category;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            Debug.LogWarning($"[BuildingData] '{name}': brak id!");

        if (prefab == null)
            Debug.LogWarning($"[BuildingData] '{id}': brak prefab!");
    }
}

public enum BuildingCategory
{
    Agriculture,
    Mining,
    Industry,
    Infrastructure
}