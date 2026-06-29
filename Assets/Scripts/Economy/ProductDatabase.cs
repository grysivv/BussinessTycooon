using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralny rejestr wszystkich produktów w grze.
/// ScriptableObject — jeden asset w projekcie, referencja przez GameManager.
/// Używaj GetById() zamiast bezpośrednich referencji do ProductData.
/// </summary>
[CreateAssetMenu(fileName = "ProductDatabase", menuName = "Tycoon/Product Database")]
public class ProductDatabase : ScriptableObject
{
    [SerializeField] private List<ProductData> _products = new List<ProductData>();

    // Słownik do szybkiego wyszukiwania po id — budowany leniwie
    private Dictionary<string, ProductData> _index;

    /// <summary>
    /// Znajdź produkt po id. Zwraca null jeśli nie istnieje.
    /// </summary>
    public ProductData GetById(string id)
    {
        BuildIndexIfNeeded();

        if (_index.TryGetValue(id, out var product))
            return product;

        Debug.LogWarning($"[ProductDatabase] Produkt '{id}' nie istnieje!");
        return null;
    }

    /// <summary>
    /// Wszystkie produkty danej kategorii.
    /// </summary>
    public List<ProductData> GetByCategory(ProductCategory category)
    {
        var result = new List<ProductData>();
        foreach (var product in _products)
        {
            if (product.category == category)
                result.Add(product);
        }
        return result;
    }

    /// <summary>
    /// Sprawdź czy produkt istnieje.
    /// </summary>
    public bool Contains(string id)
    {
        BuildIndexIfNeeded();
        return _index.ContainsKey(id);
    }

    /// <summary>
    /// Wszystkie produkty (tylko odczyt).
    /// </summary>
    public IReadOnlyList<ProductData> AllProducts => _products;

    // --- Wewnętrzne ---

    private void BuildIndexIfNeeded()
    {
        if (_index != null) return;

        _index = new Dictionary<string, ProductData>();

        foreach (var product in _products)
        {
            if (product == null)
            {
                Debug.LogWarning("[ProductDatabase] Null w liście produktów!");
                continue;
            }

            if (string.IsNullOrEmpty(product.id))
            {
                Debug.LogWarning($"[ProductDatabase] Produkt '{product.name}' ma puste id!");
                continue;
            }

            if (_index.ContainsKey(product.id))
            {
                Debug.LogError($"[ProductDatabase] Duplikat id: '{product.id}'!");
                continue;
            }

            _index[product.id] = product;
        }

        Debug.Log($"[ProductDatabase] Zbudowano indeks: {_index.Count} produktów.");
    }

    /// <summary>
    /// Przebuduj indeks — wywołaj po dodaniu produktów w edytorze.
    /// </summary>
    public void RebuildIndex()
    {
        _index = null;
        BuildIndexIfNeeded();
    }

    private void OnValidate()
    {
        // Przebuduj indeks przy każdej zmianie w edytorze
        RebuildIndex();
    }
}