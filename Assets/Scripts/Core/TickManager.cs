using UnityEngine;

/// <summary>
/// Zarządza upływem czasu w grze.
/// Publikuje TickEvent co określony interwał czasu rzeczywistego.
/// Jeden tick = jedna jednostka czasu gry.
/// </summary>
public class TickManager : MonoBehaviour
{
    [Header("Konfiguracja")]
    [SerializeField] private float _tickInterval = 1f;  // sekundy rzeczywiste na tick
    [SerializeField] private bool _startOnAwake = true;

    [Header("Stan (tylko odczyt)")]
    [SerializeField] private int _currentTick = 0;
    [SerializeField] private float _gameTime = 0f;
    [SerializeField] private bool _isRunning = false;
    [SerializeField] private float _timeScale = 1f;

    private float _tickTimer = 0f;

    // Stałe prędkości
    public static readonly float[] SpeedPresets = { 0f, 1f, 3f, 6f };

    // Właściwości publiczne (odczyt)
    public int CurrentTick => _currentTick;
    public float GameTime => _gameTime;
    public bool IsRunning => _isRunning;
    public float TimeScale => _timeScale;

    void Awake()
    {
        if (_startOnAwake)
            StartTicking();
    }

    void Update()
    {
        if (!_isRunning) return;

        _tickTimer += Time.deltaTime * _timeScale;

        if (_tickTimer >= _tickInterval)
        {
            _tickTimer -= _tickInterval;
            ProcessTick();
        }
    }

    // --- Publiczne metody kontroli ---

    public void StartTicking()
    {
        _isRunning = true;
        Debug.Log("[TickManager] Start.");
    }

    public void StopTicking()
    {
        _isRunning = false;
        Debug.Log("[TickManager] Stop.");
    }

    public void SetSpeed(float speed)
    {
        _timeScale = Mathf.Max(0f, speed);

        if (_timeScale == 0f)
            StopTicking();
        else
        {
            _isRunning = true;
            Debug.Log($"[TickManager] Prędkość: x{_timeScale}");
        }
    }

    public void SetPaused(bool paused)
    {
        _isRunning = !paused;
    }

    // --- Wewnętrzna logika ---

    private void ProcessTick()
    {
        _currentTick++;
        _gameTime += _tickInterval;

        EventBus.Publish(new TickEvent
        {
            TickNumber = _currentTick,
            GameTime = _gameTime
        });
    }
}