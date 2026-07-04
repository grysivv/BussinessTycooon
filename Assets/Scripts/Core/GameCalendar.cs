using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Śledzi czas w grze: godziny, dni, miesiące.
/// 1 tick = 1 godzina | 24 ticki = 1 dzień | 30 dni = 1 miesiąc
/// Co miesiąc pobiera koszty utrzymania od wszystkich budynków.
/// </summary>
public class GameCalendar : MonoBehaviour
{
    public static GameCalendar Instance { get; private set; }

    private const int HOURS_PER_DAY = 24;
    private const int DAYS_PER_MONTH = 30;
    private const int TICKS_PER_DAY = HOURS_PER_DAY;
    private const int TICKS_PER_MONTH = TICKS_PER_DAY * DAYS_PER_MONTH;

    [Header("Stan kalendarza (tylko odczyt)")]
    [SerializeField] private int _totalTicks = 0;
    [SerializeField] private int _hour = 0;
    [SerializeField] private int _day = 1;
    [SerializeField] private int _month = 1;
    [SerializeField] private int _year = 1;

    public int Hour => _hour;
    public int Day => _day;
    public int Month => _month;
    public int Year => _year;

    public string DateString =>
        $"Rok {_year} · Dzień {_day} · {_hour:D2}:00";

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
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<TickEvent>(OnTick);
    }

    private void OnTick(TickEvent e)
    {
        _totalTicks++;
        AdvanceTime();

        // Co miesiąc pobierz koszty utrzymania
        if (_totalTicks % TICKS_PER_MONTH == 0)
            ProcessMonthlyMaintenance();
    }

    private void AdvanceTime()
    {
        _hour++;

        if (_hour >= HOURS_PER_DAY)
        {
            _hour = 0;
            _day++;
        }

        if (_day > DAYS_PER_MONTH)
        {
            _day = 1;
            _month++;

            EventBus.Publish(new MonthPassedEvent
            {
                Month = _month,
                Year = _year
            });
        }

        if (_month > 12)
        {
            _month = 1;
            _year++;
        }

        EventBus.Publish(new TimeUpdatedEvent
        {
            Hour = _hour,
            Day = _day,
            Month = _month,
            Year = _year,
            DateString = DateString
        });
    }

    private void ProcessMonthlyMaintenance()
    {
        var buildings = FindObjectsByType<Building>(FindObjectsSortMode.None);
        float totalCost = 0f;

        foreach (var building in buildings)
        {
            totalCost += building.MonthlyMaintenance;
        }

        if (totalCost > 0f)
        {
            bool success = PlayerManager.Instance.TrySpend(totalCost);

            Debug.Log($"[GameCalendar] Koszty utrzymania miesiąc {_month}/{_year}: " +
                      $"${totalCost:F0} | Sukces: {success}");

            EventBus.Publish(new MaintenancePaidEvent
            {
                TotalCost = totalCost,
                Month = _month,
                Year = _year,
                Success = success
            });

        }
    }
}