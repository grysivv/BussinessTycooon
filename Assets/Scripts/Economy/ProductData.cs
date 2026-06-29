using UnityEngine;

/// <summary>
/// Dane pojedynczego produktu. ScriptableObject — edytowalny w Inspectorze.
/// Jeden plik .asset = jeden produkt (pszenica, mąka, chleb itd.)
/// Nie zawiera sprite'a — grafika jako osobny Addressable (optymalizacja pamięci).
/// </summary>
[CreateAssetMenu(fileName = "Product_", menuName = "Tycoon/Product Data")]
public class ProductData : ScriptableObject
{
    [Header("Identyfikacja")]
    [Tooltip("Unikalny identyfikator. Używaj snake_case, np. 'wheat', 'iron_ore'")]
    public string id;

    [Tooltip("Nazwa wyświetlana w UI")]
    public string displayName;

    [Tooltip("Kategoria produktu")]
    public ProductCategory category;

    [Header("Ekonomia")]
    [Tooltip("Cena bazowa — punkt odniesienia dla krzywej popytu")]
    public float basePrice;

    [Tooltip("Elastyczność cenowa popytu. Typowe wartości: 0.5–2.0")]
    [Range(0.1f, 5f)]
    public float elasticity = 1f;

    [Tooltip("Popyt bazowy przy cenie równej cenie bazowej")]
    public float baseDemand = 100f;

    [Header("Opis")]
    [TextArea(2, 4)]
    public string description;

    /// <summary>
    /// Oblicza popyt przy danej cenie gracza.
    /// Wzór: popyt = popyt_bazowy * (cena_bazowa / cena_gracza) ^ elastycznosc
    /// </summary>
    public float CalculateDemand(float playerPrice)
    {
        if (playerPrice <= 0f)
        {
            Debug.LogWarning($"[ProductData] {id}: cena musi być > 0");
            return 0f;
        }

        return baseDemand * Mathf.Pow(basePrice / playerPrice, elasticity);
    }

    /// <summary>
    /// Walidacja danych — wywoływana automatycznie w edytorze przy zapisie.
    /// </summary>
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(id))
            Debug.LogWarning($"[ProductData] '{name}': brak id!");

        if (basePrice <= 0f)
            Debug.LogWarning($"[ProductData] '{id}': basePrice musi być > 0");

        if (baseDemand <= 0f)
            Debug.LogWarning($"[ProductData] '{id}': baseDemand musi być > 0");
    }
}

public enum ProductCategory
{
    Agriculture,    // Rolnictwo: pszenica, kukurydza
    Mining,         // Wydobycie: ruda żelaza, węgiel
    Food,           // Przetwórstwo spożywcze: mąka, chleb
    Industry,       // Przemysł: stal, części
    Consumer        // Dobra konsumpcyjne: na przyszłość
}