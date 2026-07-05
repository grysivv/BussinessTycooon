using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Budynek produkcyjny — dziedziczy z Building.
/// Pobiera surowce ze "wspólnej puli" połączonych budynków,
/// produkuje output do własnego magazynu.
/// Używa Dirty Flag — przelicza się tylko gdy coś się zmieniło.
/// </summary>
public class ProductionBuilding : Building
{
    [Header("Produkcja")]
    [SerializeField] private RecipeData _recipe;

    [Header("Magazyn")]
    [SerializeField] private float _storageCapacity = 100f;

    [Header("Sprzedaż")]
    [SerializeField] private float _sellingPrice = 0f;
     [SerializeField] private bool _autoSell = false;
     public float SellingPrice => _sellingPrice;
     public bool AutoSell => _autoSell;

    [Header("Połączenia (wypełniane przez BuildingConnector)")]
    [SerializeField] private List<ProductionBuilding> _connectedBuildings = new();

    // Magazyn: productId → ilość
    private Dictionary<string, float> _storage = new();

    // Dirty Flag — optymalizacja
    private bool _isDirty = true;

    // Postęp aktualnego cyklu produkcji (w tickach)
    private int _productionProgress = 0;

    // Referencje do baz danych
    private ProductDatabase _productDatabase;
    private StatisticsManager _statisticsManager;

    public RecipeData Recipe => _recipe;
    public float StorageCapacity => _storageCapacity;
    public IReadOnlyList<ProductionBuilding> ConnectedBuildings => _connectedBuildings;
    private List<float> _outputHistory = new ();
    private const int MAX_HISTORY = 12; //  ostatnie 12 tików 

    /// <summary>
    /// average output items/tick z ostatnich N tikców
    /// </summary>
  
    public float GetAverageThroughput(int tikcCount)
    {
        if (_outputHistory.Count == 0) return 0f;
        int count = Mathf.Min(tikcCount, _outputHistory.Count);
        float sum  = 0f;
        for (int i = _outputHistory.Count - count; i < _outputHistory.Count; i++)
        {
            sum += _outputHistory[i];
        }
        return sum / count;
    }

        ///<summary>
        /// Zapisz output do historii
        /// </summary>
        private void RecordOutput(float amount)
        {
            _outputHistory.Add(amount);
            if (_outputHistory.Count > MAX_HISTORY)
                _outputHistory.RemoveAt(0);
        }
     /// Average output items/tick z ostatnich N tikców


    private void OnAnyProductionCompleted(ProductionCompletedEvent e)
{
    // Gdy ktokolwiek coś wyprodukował, sprawdź ponownie czy możemy ruszyć
    if (!_isDirty && _recipe != null)
    {
        MarkDirty();
    }
}

    void OnEnable()
    {
        EventBus.Subscribe<TickEvent>(OnTick);
        EventBus.Subscribe<ProductionCompletedEvent>(OnAnyProductionCompleted);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<ProductionCompletedEvent>(OnAnyProductionCompleted);
    }

    /// <summary>
    /// Inicjalizacja budynku produkcyjnego.
    /// </summary>
    public void InitializeProduction(RecipeData recipe, ProductDatabase productDatabase)
    {
        _recipe = recipe;
        _productDatabase = productDatabase;
        _statisticsManager = StatisticsManager.Instance;
        _storage = new Dictionary<string, float>();
        _isDirty = true;

        if (_recipe != null && _productDatabase != null)
        {
            var productData = _productDatabase.GetById(_recipe.outputProductId);
            if (productData != null)
                _sellingPrice = productData.basePrice; // domyślna cena sprzedaży = cena bazowa produktu

        }
    }

    // --- Magazyn — dostęp publiczny ---

    /// <summary>
    /// Ile danego produktu jest w magazynie tego budynku.
    /// </summary>
    public float GetStorageAmount(string productId)
    {
        return _storage.TryGetValue(productId, out float amount) ? amount : 0f;
    }

    /// <summary>
    /// Dodaj produkt do magazynu (z ograniczeniem pojemności).
    /// </summary>
    public float AddToStorage(string productId, float amount)
    {
        float current = GetStorageAmount(productId);
        float totalUsed = GetTotalStorageUsed();
        float freeSpace = _storageCapacity - totalUsed;

        float actualAmount = Mathf.Min(amount, freeSpace);
        if (actualAmount <= 0f) return 0f;

        _storage[productId] = current + actualAmount;
        MarkDirty();

        return actualAmount; // ile faktycznie dodano
    }

    /// <summary>
    /// Pobierz produkt z magazynu. Zwraca faktycznie pobraną ilość.
    /// </summary>
    public float TakeFromStorage(string productId, float amount)
    {
        float current = GetStorageAmount(productId);
        float actualAmount = Mathf.Min(amount, current);

        if (actualAmount <= 0f) return 0f;

        _storage[productId] = current - actualAmount;
        MarkDirty();

        return actualAmount;
    }

    private float GetTotalStorageUsed()
    {
        float total = 0f;
        foreach (var amount in _storage.Values)
            total += amount;
        return total;
    }

    // --- Wspólna pula zasobów ---

    /// <summary>
    /// Ile danego produktu jest dostępne łącznie:
    /// we własnym magazynie + we wszystkich połączonych budynkach.
    /// </summary>
    public float GetAvailableFromPool(string productId)
    {
        float total = GetStorageAmount(productId);

        foreach (var connected in _connectedBuildings)
        {
            if (connected != null)
                total += connected.GetStorageAmount(productId);
        }

        return total;
    }

    /// <summary>
    /// Pobierz produkt ze wspólnej puli — najpierw z własnego magazynu,
    /// potem z połączonych budynków (w kolejności listy).
    /// </summary>
    private float TakeFromPool(string productId, float amount)
    {
        float remaining = amount;
        float taken = 0f;

        // Najpierw własny magazyn
        float fromSelf = TakeFromStorage(productId, remaining);
        taken += fromSelf;
        remaining -= fromSelf;

        // Potem połączone budynki
        foreach (var connected in _connectedBuildings)
        {
            if (remaining <= 0f) break;
            if (connected == null) continue;

            float fromConnected = connected.TakeFromStorage(productId, remaining);
            taken += fromConnected;
            remaining -= fromConnected;
        }

        return taken;
    }

    // --- Produkcja ---

    private void OnTick(TickEvent e)
    {
        if (!_isDirty) return;
        if (_recipe == null) return;

        ProcessProduction();
    }

    private void ProcessProduction()
    {
        // Sprawdź czy mamy wystarczająco surowców do rozpoczęcia/kontynuacji cyklu
        if (_productionProgress == 0)
        {
            if (!HasEnoughInputs())
            {
                _isDirty = false; // czekamy na surowce, nic się nie zmienia
                return;
            }

            ConsumeInputs();
        }

        _productionProgress++;

        if (_productionProgress >= _recipe.productionTimeTicks)
        {
            CompleteProductionCycle();
            _productionProgress = 0;
        }

        // Jeśli po zakończeniu cyklu nadal mamy surowce na kolejny — zostań dirty
        _isDirty = HasEnoughInputs();
    }

    private bool HasEnoughInputs()
    {
        foreach (var ingredient in _recipe.inputs)
        {
            if (GetAvailableFromPool(ingredient.productId) < ingredient.amount)
                return false;
        }
        return true;
    }

    private void ConsumeInputs()
    {
        foreach (var ingredient in _recipe.inputs)
        {
            float taken = TakeFromPool(ingredient.productId, ingredient.amount);
            _statisticsManager?.RecordConsumption(ingredient.productId, taken);
        }
    }

    private void CompleteProductionCycle()
    {
        float actualOutput = AddToStorage(_recipe.outputProductId, _recipe.outputAmount);
        RecordOutput(actualOutput);

        EventBus.Publish(new ProductionCompletedEvent
        {
            ProductId = _recipe.outputProductId,
            Amount = actualOutput
        });

        _statisticsManager?.RecordSale(_recipe.outputProductId, 0f, 0f);
        if(_autoSell)
            SellAll();

        // RecordSale tu tylko jako placeholder do produkcji — 
        // faktyczna sprzedaż będzie osobnym wywołaniem z systemu rynku
    }

    // --- Dirty Flag ---

    public void MarkDirty()
    {
        _isDirty = true;
    }

    // --- Połączenia (wypełniane przez BuildingConnector) ---

    public void SetConnectedBuildings(List<ProductionBuilding> connections)
    {
        _connectedBuildings = connections;
        MarkDirty();
    }

    public override void OnDemolish()
    {
        base.OnDemolish();
        _connectedBuildings.Clear();
        _storage.Clear();
    }

    // --- Diagnostyka ---

    [ContextMenu("Log stan produkcji")]
    public void LogProductionState()
    {
        Debug.Log($"[ProductionBuilding] {DisplayName} ({Id})");
        Debug.Log($"  Receptura: {_recipe?.displayName ?? "BRAK"}");
        Debug.Log($"  Postęp: {_productionProgress}/{_recipe?.productionTimeTicks}");
        Debug.Log($"  Dirty: {_isDirty}");
        Debug.Log($"  Połączone budynki: {_connectedBuildings.Count}");

        foreach (var kvp in _storage)
            Debug.Log($"  Magazyn: {kvp.Key} = {kvp.Value:F1}");
    }
/// <summary>
/// Ustaw cenę sprzedaży produktu.
/// </summary>
public void SetSellingPrice(float price)
{
    _sellingPrice = Mathf.Max(0f, price);
}

/// <summary>
/// Włącz/wyłącz auto-sprzedaż.
/// </summary>
public void SetAutoSell(bool enabled)
{
    _autoSell = enabled;
}

/// <summary>
/// Sprzedaj całą zawartość magazynu po aktualnej cenie.
/// Zwraca przychód ze sprzedaży.
/// </summary>
public float SellAll()
    {
        Debug.Log($"[SellAll] {_displayName}: _sellingPrice={_sellingPrice}");
        if (_recipe == null) return 0f;
        if (_sellingPrice <= 0f)
        {
            Debug.LogWarning($"[ProductionBuilding] {_displayName}: cena sprzedaży = 0!");
            return 0f;
        }

        string productId = _recipe.outputProductId;
        float available = GetStorageAmount(productId);
        if (available <= 0f) return 0f;

        // Ogranicz sprzedaż do popytu
        float demand = EconomyManager.Instance?.GetDemand(productId) ?? available;
        float toSell = Mathf.Min(available, demand);

        float sold = TakeFromStorage(productId, toSell);
        float revenue = sold * _sellingPrice;

        PlayerManager.Instance?.AddCapital(revenue);
        StatisticsManager.Instance?.RecordSale(productId, sold, revenue);

        EventBus.Publish(new SaleCompletedEvent
        {
            ProductId = productId,
            Amount = sold,
            Revenue = revenue,
            Price = _sellingPrice
        });

        Debug.Log($"[ProductionBuilding] {_displayName}: sprzedano {sold:F1} x {productId} " +
                $"@ ${_sellingPrice} = ${revenue:F2}");

        return revenue;
    }
}