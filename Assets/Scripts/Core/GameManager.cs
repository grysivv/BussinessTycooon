using UnityEngine;

/// <summary>
/// Centralny koordynator gry.
/// Inicjalizuje systemy i zarządza stanem gry.
/// NIE zawiera logiki biznesowej — deleguje do odpowiednich managerów.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencje do systemów")]
    [SerializeField] private TickManager _tickManager;

    [Header("Stan gry")]
    [SerializeField] private GameState _currentState = GameState.Running;

    public GameState CurrentState => _currentState;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSystems();
    }

    void OnEnable()
    {
        EventBus.Subscribe<TickEvent>(OnTick);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
    }

    // --- Inicjalizacja ---

    private void InitializeSystems()
    {
        // Znajdź TickManager jeśli nie przypisany w Inspectorze
        if (_tickManager == null)
            _tickManager = FindAnyObjectByType<TickManager>();

        if (_tickManager == null)
            Debug.LogError("[GameManager] Brak TickManager w scenie!");
        else
            Debug.Log("[GameManager] Systemy zainicjalizowane.");
    }

    // --- Kontrola stanu gry ---

    /// <summary>
    /// Zatrzymuje czas w grze (pauzuje).
    /// </summary>
    public void PauseGame()
    {
        if (_currentState == GameState.Running)
        {
            _currentState = GameState.Paused;
            _tickManager?.SetPaused(true);
            Debug.Log("[GameManager] Gra wstrzymana.");
        }
    }

    /// <summary>
    /// Wznawia czas w grze.
    /// </summary>
    public void ResumeGame()
    {
        if (_currentState == GameState.Paused)
        {
            _currentState = GameState.Running;
            _tickManager?.SetPaused(false);
            Debug.Log("[GameManager] Gra wznowiona.");
        }
    }

    /// <summary>
    /// Zmienia mnożnik prędkości upływu czasu (ticków).
    /// </summary>
    public void SetGameSpeed(float speed)
    {
        _tickManager?.SetSpeed(speed);
    }

    // --- Handlery zdarzeń ---

    private void OnTick(TickEvent e)
    {
        // Na razie tylko log — docelowo koordynacja systemów
        if (e.TickNumber % 10 == 0)
            Debug.Log($"[GameManager] Tick #{e.TickNumber}");
    }
}

public enum GameState
{
    Running,
    Paused,
    Loading
}