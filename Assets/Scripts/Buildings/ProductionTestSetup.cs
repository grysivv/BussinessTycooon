using UnityEngine;

/// <summary>
/// Tymczasowy skrypt do ręcznego testu produkcji.
/// Usuń po zakończeniu testów.
/// </summary>
public class ProductionTestSetup : MonoBehaviour
{
    [Header("Referencje do testowych budynków")]
    [SerializeField] private ProductionBuilding _farm;
    [SerializeField] private ProductionBuilding _mill;

    [Header("Dane")]
    [SerializeField] private RecipeData _farmRecipe;
    [SerializeField] private RecipeData _millRecipe;
    [SerializeField] private ProductDatabase _productDatabase;

    void Start()
    {
    _farm.InitializeProduction(_farmRecipe, _productDatabase);
    _mill.InitializeProduction(_millRecipe, _productDatabase);

    // Zamiast ręcznego SetConnectedBuildings — użyj BuildingConnector
    var result = BuildingConnector.Instance.TryConnect(_farm, _mill);
    Debug.Log($"[Test] Wynik połączenia: {result.Message}");
    }
}