using UnityEngine;

/// <summary>
/// Przechowuje dane gracza niezależne od miasta.
/// Kapitał, statystyki globalne, postęp.
/// Istnieje przez całą sesję — nie resetuje się przy zmianie miasta.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Dane gracza")]
    [SerializeField] private string _playerName = "Gracz";
    [SerializeField] private float _capital = 50000f;

    [Header("Statystyki globalne (tylko odczyt)")]
    [SerializeField] private int _totalBuildingsBuilt = 0;
    [SerializeField] private int _totalTicksPlayed = 0;
    [SerializeField] private float _totalMoneyEarned = 0f;
    [SerializeField] private float _totalMoneySpent = 0f;

    // Właściwości publiczne
    public string PlayerName => _playerName;
    public float Capital => _capital;
    public int TotalBuildingsBuilt => _totalBuildingsBuilt;
    public int TotalTicksPlayed => _totalTicksPlayed;

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
        EventBus.Subscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
    }

    // --- Finanse ---

    /// <summary>
    /// Próba wydania pieniędzy. Zwraca false jeśli brak środków.
    /// </summary>
    public bool TrySpend(float amount)
    {
        if (amount <= 0f) return false;

        if (_capital < amount)
        {
            Debug.Log($"[PlayerManager] Brak środków. " +
                      $"Potrzeba: ${amount:F0}, Jest: ${_capital:F0}");
            return false;
        }

        _capital -= amount;
        _totalMoneySpent += amount;

        EventBus.Publish(new PlayerMoneyChangedEvent
        {
            OldAmount = _capital + amount,
            NewAmount = _capital,
            Delta = -amount
        });

        return true;
    }

    /// <summary>
    /// Dodaj pieniądze do kapitału gracza.
    /// </summary>
    public void AddCapital(float amount)
    {
        if (amount <= 0f) return;

        float old = _capital;
        _capital += amount;
        _totalMoneyEarned += amount;

        EventBus.Publish(new PlayerMoneyChangedEvent
        {
            OldAmount = old,
            NewAmount = _capital,
            Delta = amount
        });
    }

    // --- Handlery ---

    private void OnTick(TickEvent e)
    {
        _totalTicksPlayed++;
    }

    private void OnMoneyChanged(PlayerMoneyChangedEvent e)
    {
        // Na razie tylko odbiór — docelowo synchronizacja z UI
    }

    // --- Diagnostyka ---

    [ContextMenu("Log dane gracza")]
    public void LogPlayerState()
    {
        Debug.Log($"[PlayerManager] Gracz: {_playerName}");
        Debug.Log($"[PlayerManager] Kapitał: ${_capital:F0}");
        Debug.Log($"[PlayerManager] Zarobiono łącznie: ${_totalMoneyEarned:F0}");
        Debug.Log($"[PlayerManager] Wydano łącznie: ${_totalMoneySpent:F0}");
        Debug.Log($"[PlayerManager] Ticki: {_totalTicksPlayed}");
        Debug.Log($"[PlayerManager] Budynki postawione: {_totalBuildingsBuilt}");
    }
}