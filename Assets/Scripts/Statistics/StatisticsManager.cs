using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zbiera i przechowuje statystyki gry.
/// Działa niezależnie od Dirty Flag — rejestruje dane co tick.
/// Liczniki skumulowane: nigdy nie maleją, skalują się do 160h+ sesji.
/// Komunikacja wyłącznie przez EventBus.
/// </summary>
public class StatisticsManager : MonoBehaviour
{
    public static StatisticsManager Instance { get; private set; }

    // Liczniki skumulowane per produkt
    private Dictionary<string, float> _totalProduced  = new();
    private Dictionary<string, float> _totalConsumed  = new();
    private Dictionary<string, float> _totalSold      = new();
    private Dictionary<string, float> _totalRevenue   = new();

    // Hourly price history (per product, max 30 days = 720 ticki)
    private Dictionary<string, List<float>> _hourlyPriceHistory = new();
    private Dictionary<string, float> _hourlyRevenue = new();
    private Dictionary<string, float> _hourlyAmount = new();
    private const int TICKS_PER_HOUR = 24;
    private const int MAX_HISTORY_POINTS = 30; // days

    // Liczniki globalne
    private int   _totalTicks          = 0;
    private float _totalMoneyEarned    = 0f;
    private float _totalMoneySpent     = 0f;
    private int   _totalBuildingsBuilt = 0;
    private int   _totalBuildingsDestroyed = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void OnEnable()
    {
        EventBus.Subscribe<TickEvent>(OnTick);
        EventBus.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Subscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Subscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        EventBus.Subscribe<BuildingRemovedEvent>(OnBuildingRemoved);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Unsubscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Unsubscribe<BuildingPlacedEvent>(OnBuildingPlaced);
        EventBus.Unsubscribe<BuildingRemovedEvent>(OnBuildingRemoved);
    }

    // --- Handlery EventBus ---

    private void OnTick(TickEvent e)
    {
        _totalTicks++;

        // Co 24 ticki (1h ingame) — aggregate hourly prices
        if (_totalTicks % TICKS_PER_HOUR == 0)
        {
            AggregateHourlyPrices();
        }
    }

    private void OnProductionCompleted(ProductionCompletedEvent e)
    {
        AddToCounter(_totalProduced, e.ProductId, e.Amount);
    }

    private void OnMoneyChanged(PlayerMoneyChangedEvent e)
    {
        if (e.Delta > 0f)
            _totalMoneyEarned += e.Delta;
        else
            _totalMoneySpent += Mathf.Abs(e.Delta);
    }

    private void OnBuildingPlaced(BuildingPlacedEvent e)
    {
        _totalBuildingsBuilt++;
    }

    private void OnBuildingRemoved(BuildingRemovedEvent e)
    {
        _totalBuildingsDestroyed++;
    }

    // --- Publiczne metody rejestracji ---

    /// <summary>
    /// Zarejestruj sprzedaż produktu.
    /// </summary>
    public void RecordSale(string productId, float amount, float revenue)
    {
        AddToCounter(_totalSold, productId, amount);
        AddToCounter(_totalRevenue, productId, revenue);

        // Akumuluj do hourly aggregate
        AddToCounter(_hourlyRevenue, productId, revenue);
        AddToCounter(_hourlyAmount, productId, amount);
    }

    /// <summary>
    /// Zarejestruj zużycie produktu (jako surowiec).
    /// </summary>
    public void RecordConsumption(string productId, float amount)
    {
        AddToCounter(_totalConsumed, productId, amount);
    }

    // --- Publiczne metody odczytu ---

    /// <summary>
    /// Zwraca łączną historyczną ilość wyprodukowanego produktu danego typu.
    /// </summary>
    public float GetTotalProduced(string productId)
        => GetCounter(_totalProduced, productId);

    /// <summary>
    /// Zwraca łączną historyczną ilość zużytego produktu danego typu (jako surowiec).
    /// </summary>
    public float GetTotalConsumed(string productId)
        => GetCounter(_totalConsumed, productId);

    /// <summary>
    /// Zwraca łączną historyczną ilość sprzedanego produktu danego typu.
    /// </summary>
    public float GetTotalSold(string productId)
        => GetCounter(_totalSold, productId);

    /// <summary>
    /// Zwraca łączny historyczny przychód ze sprzedaży produktu danego typu.
    /// </summary>
    public float GetTotalRevenue(string productId)
        => GetCounter(_totalRevenue, productId);

    /// <summary>
    /// Zwraca historię cen godzinowych produktu (ostatnie 30 dni, max 30 punktów).
    /// </summary>
    public List<float> GetHourlyPriceHistory(string productId)
    {
        if (_hourlyPriceHistory.TryGetValue(productId, out var history))
            return new List<float>(history);
        return new List<float>();
    }

    /// <summary>
    /// Zwraca statystyki produkcji dla produktu (wyprodukowana, zużyta, średnia cena).
    /// </summary>
    public (float produced, float consumed, float avgPrice) GetProductionStats(string productId)
    {
        float produced = GetTotalProduced(productId);
        float consumed = GetTotalConsumed(productId);
        float totalRevenue = GetTotalRevenue(productId);
        float totalSold = GetTotalSold(productId);
        float avgPrice = totalSold > 0 ? totalRevenue / totalSold : 0f;

        return (produced, consumed, avgPrice);
    }

    public int TotalTicks             => _totalTicks;
    public float TotalMoneyEarned     => _totalMoneyEarned;
    public float TotalMoneySpent      => _totalMoneySpent;
    public int TotalBuildingsBuilt    => _totalBuildingsBuilt;
    public int TotalBuildingsDestroyed => _totalBuildingsDestroyed;

    // --- Pomocnicze ---

    private void AddToCounter(Dictionary<string, float> dict,
                               string key, float amount)
    {
        if (!dict.ContainsKey(key))
            dict[key] = 0f;
        dict[key] += amount;
    }

    private float GetCounter(Dictionary<string, float> dict, string key)
    {
        return dict.TryGetValue(key, out float val) ? val : 0f;
    }

    // --- Agregacja danych ---

    private void AggregateHourlyPrices()
    {
        foreach (var productId in _hourlyRevenue.Keys)
        {
            if (_hourlyAmount.TryGetValue(productId, out float amount) && amount > 0)
            {
                float revenue = _hourlyRevenue[productId];
                float hourlyPrice = revenue / amount;

                // Inicjalizuj listę jeśli nie istnieje
                if (!_hourlyPriceHistory.ContainsKey(productId))
                    _hourlyPriceHistory[productId] = new List<float>();

                // Dodaj nową cenę godzinową
                _hourlyPriceHistory[productId].Add(hourlyPrice);

                // Przechowaj tylko ostatnie 30 dni (720 ticki / 24 = 30 punktów)
                if (_hourlyPriceHistory[productId].Count > MAX_HISTORY_POINTS)
                    _hourlyPriceHistory[productId].RemoveAt(0);
            }
        }

        // Wyzeruj akumulatory na następną godzinę
        _hourlyRevenue.Clear();
        _hourlyAmount.Clear();
    }

    // --- Diagnostyka ---

    [ContextMenu("Log statystyki")]
    public void LogStatistics()
    {
        Debug.Log("=== STATYSTYKI ===");
        Debug.Log($"Łączne ticki: {_totalTicks}");
        Debug.Log($"Zarobiono: ${_totalMoneyEarned:F0}");
        Debug.Log($"Wydano: ${_totalMoneySpent:F0}");
        Debug.Log($"Budynki postawione: {_totalBuildingsBuilt}");
        Debug.Log($"Budynki zburzone: {_totalBuildingsDestroyed}");

        Debug.Log("--- Produkcja ---");
        foreach (var kvp in _totalProduced)
            Debug.Log($"  {kvp.Key}: " +
                      $"wyprodukowano={kvp.Value:F0}, " +
                      $"sprzedano={GetTotalSold(kvp.Key):F0}, " +
                      $"zużyto={GetTotalConsumed(kvp.Key):F0}");
    }
}