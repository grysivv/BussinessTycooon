using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zarządza stanem ekonomicznym gry.
/// Przechowuje aktualne ceny gracza i oblicza popyt co tick.
/// Komunikuje się przez EventBus — nie ma bezpośrednich referencji do UI.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    [Header("Referencje")]
    [SerializeField] private ProductDatabase _productDatabase;

    [Header("Stan (tylko odczyt)")]
    [SerializeField] private float _playerMoney = 10000f;

    // Ceny ustawione przez gracza: productId → cena
    private Dictionary<string, float> _playerPrices = new();

    // Aktualny popyt: productId → popyt
    private Dictionary<string, float> _currentDemand = new();

    public float PlayerMoney => _playerMoney;
    public ProductDatabase ProductDatabase => _productDatabase;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_productDatabase == null)
            Debug.LogError("[EconomyManager] Brak ProductDatabase!");
    }

    void OnEnable()
    {
        EventBus.Subscribe<TickEvent>(OnTick);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
    }

    // --- Tick ---

    private void OnTick(TickEvent e)
    {
        RecalculateDemand();
    }

    // --- Ceny ---

    /// <summary>
    /// Ustaw cenę sprzedaży produktu przez gracza.
    /// </summary>
    public void SetPlayerPrice(string productId, float price)
    {
        if (price <= 0f)
        {
            Debug.LogWarning($"[EconomyManager] Cena musi być > 0 dla '{productId}'");
            return;
        }

        float oldPrice = GetPlayerPrice(productId);
        _playerPrices[productId] = price;

        EventBus.Publish(new PriceChangedEvent
        {
            ProductId = productId,
            OldPrice = oldPrice,
            NewPrice = price
        });
    }

    /// <summary>
    /// Pobierz aktualną cenę gracza. 
    /// Jeśli gracz nie ustawił ceny — zwraca cenę bazową z ProductDatabase.
    /// </summary>
    public float GetPlayerPrice(string productId)
    {
        if (_playerPrices.TryGetValue(productId, out float price))
            return price;

        // Fallback: cena bazowa z bazy danych
        var product = _productDatabase?.GetById(productId);
        return product != null ? product.basePrice : 0f;
    }

    // --- Popyt ---

    /// <summary>
    /// Oblicz popyt dla wszystkich produktów na podstawie cen gracza.
    /// Wywoływane co tick.
    /// </summary>
    private void RecalculateDemand()
    {
        if (_productDatabase == null) return;

        foreach (var product in _productDatabase.AllProducts)
        {
            float playerPrice = GetPlayerPrice(product.id);
            float demand = product.CalculateDemand(playerPrice);
            _currentDemand[product.id] = demand;
        }
    }

    /// <summary>
    /// Pobierz aktualny popyt na produkt.
    /// </summary>
    public float GetDemand(string productId)
    {
        if (_currentDemand.TryGetValue(productId, out float demand))
            return demand;

        // Jeszcze nie obliczony — oblicz teraz
        var product = _productDatabase?.GetById(productId);
        if (product == null) return 0f;

        float price = GetPlayerPrice(productId);
        return product.CalculateDemand(price);
    }

    // --- Finanse gracza ---

    /// <summary>
    /// Dodaj lub odejmij pieniądze gracza.
    /// Używaj ujemnych wartości dla kosztów.
    /// </summary>
    public bool TrySpendMoney(float amount)
    {
        if (_playerMoney < amount)
        {
            Debug.Log($"[EconomyManager] Brak środków. Potrzeba: {amount}, Jest: {_playerMoney}");
            return false;
        }

        float oldAmount = _playerMoney;
        _playerMoney -= amount;

        EventBus.Publish(new PlayerMoneyChangedEvent
        {
            OldAmount = oldAmount,
            NewAmount = _playerMoney,
            Delta = -amount
        });

        return true;
    }

    public void AddMoney(float amount)
    {
        if (amount <= 0f) return;

        float oldAmount = _playerMoney;
        _playerMoney += amount;

        EventBus.Publish(new PlayerMoneyChangedEvent
        {
            OldAmount = oldAmount,
            NewAmount = _playerMoney,
            Delta = amount
        });
    }

    // --- Diagnostyka ---

    [ContextMenu("Log stan ekonomii")]
    public void LogEconomyState()
    {
        Debug.Log($"[EconomyManager] Pieniądze gracza: ${_playerMoney}");
        foreach (var kvp in _currentDemand)
            Debug.Log($"  {kvp.Key}: popyt={kvp.Value:F1}, cena={GetPlayerPrice(kvp.Key)}");
    }
}