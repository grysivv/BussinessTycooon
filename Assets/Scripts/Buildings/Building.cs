using UnityEngine;

/// <summary>
/// Bazowa klasa dla wszystkich budynków w grze.
/// Zawiera dane wspólne: pozycję, koszty, poziom.
/// ProductionBuilding i inne typy dziedziczą z tej klasy.
/// </summary>
public class Building : MonoBehaviour
{
    [Header("Identyfikacja")]
    [SerializeField] protected string _id;
    [SerializeField] protected string _displayName;

    [Header("Pozycja na siatce")]
    [SerializeField] protected int _gridX;
    [SerializeField] protected int _gridY;
    [SerializeField] protected Vector2Int _footprint = Vector2Int.one; // np. 1x1, 2x2

    [Header("Ekonomia budynku")]
    [SerializeField] protected float _constructionCost;
    [SerializeField] protected float _demolitionCost;
    [SerializeField] protected float _monthlyMaintenance;
    [SerializeField] protected float _monthlyWages = 100f; // jeśli budynek wymaga pracowników

    [Header("Poziom")]
    [SerializeField] protected int _level = 1;

    // Właściwości publiczne (odczyt)
    public string Id => _id;
    public string DisplayName => _displayName;
    public int GridX => _gridX;
    public int GridY => _gridY;
    public Vector2Int Footprint => _footprint;
    public float ConstructionCost => _constructionCost;
    public float DemolitionCost => _demolitionCost;
    public float MonthlyMaintenance => _monthlyMaintenance;
    public float MonthlyWages => _monthlyWages;
    public int Level => _level;
    public float GetMonthlyCost() => _monthlyMaintenance + _monthlyWages; // maintance + wages (monthly)

    /// <summary>
    /// Inicjalizacja budynku przy stawianiu. Wywoływane przez system budowania.
    /// </summary>
    public virtual void Initialize(string id, string displayName, int gridX, int gridY,
                                    float constructionCost, float demolitionCost,
                                    float monthlyMaintenance)
    {
        _id = id;
        _displayName = displayName;
        _gridX = gridX;
        _gridY = gridY;
        _constructionCost = constructionCost;
        _demolitionCost = demolitionCost;
        _monthlyMaintenance = monthlyMaintenance;
        _monthlyWages = monthlyWages; // domyślnie brak pracowników, można ustawić w klasach pochodnych
        _level = 1;
    }

    /// <summary>
    /// Wywoływane przy usuwaniu budynku — nadpisz w klasach pochodnych
    /// żeby wyczyścić połączenia, magazyn itd.
    /// </summary>
    public virtual void OnDemolish()
    {
        Debug.Log($"[Building] {_displayName} ({_id}) zburzony.");
    }

    /// <summary>
    /// Podstawowy upgrade poziomu. Klasy pochodne mogą rozszerzyć efekt.
    /// </summary>
    public virtual bool TryUpgrade(float upgradeCost, PlayerManager playerManager)
    {
        if (!playerManager.TrySpend(upgradeCost))
            return false;

        _level++;
        Debug.Log($"[Building] {_displayName} ulepszony do poziomu {_level}");
        return true;
    }
}