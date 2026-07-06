
// --- Tick ---

/// <summary>
/// Zdarzenie wysyłane przy każdym tyknięciu (tick) gry.
/// Służy do aktualizacji systemów zależnych od czasu.
/// </summary>
public struct TickEvent
{
    public int TickNumber;
    public float GameTime;
}

// --- Budynki ---

/// <summary>
/// Zdarzenie wysyłane po postawieniu budynku na siatce.
/// </summary>
public struct BuildingPlacedEvent
{
    public int GridX;
    public int GridY;
}

/// <summary>
/// Zdarzenie wysyłane po usunięciu (zburzeniu) budynku z siatki.
/// </summary>
public struct BuildingRemovedEvent
{
    public int GridX;
    public int GridY;
}

/// <summary>
/// Zdarzenie odznaczenia jakiegokolwiek budynku (czyszczenie selekcji).
/// </summary>
public struct BuildingDeselectedEvent { }

// --- Produkcja ---

/// <summary>
/// Zdarzenie wysyłane, gdy produkcja danego produktu zostanie zakończona.
/// </summary>
public struct ProductionCompletedEvent
{
    public string ProductId;
    public float Amount;
}

/// <summary>
/// Zdarzenie wysyłane, gdy produkcja danego produktu nie powiedzie się
/// (np. z braku surowców).
/// </summary>
public struct ProductionFailedEvent
{
    public string ProductId;
    public string Reason;
}

// --- Ekonomia ---

/// <summary>
/// Zdarzenie wysyłane po zmianie ceny produktu przez gracza.
/// </summary>
public struct PriceChangedEvent
{
    public string ProductId;
    public float OldPrice;
    public float NewPrice;
}

/// <summary>
/// Zdarzenie wysyłane przy zmianie stanu konta (kapitału) gracza.
/// </summary>
public struct PlayerMoneyChangedEvent
{
    public float OldAmount;
    public float NewAmount;
    public float Delta;
}

/// <summary>
/// Zdarzenie wysyłane, gdy połączono dwa budynki produkcyjne (przesył zasobów).
/// </summary>
public struct BuildingsConnectedEvent
{
    public string SourceId;
    public string TargetId;
    public string ProductId;
}

/// <summary>
/// Zdarzenie wysyłane, gdy wybrano konkretny budynek, co zwykle skutkuje
/// pokazaniem dla niego UI (np. panelu informacyjnego).
/// </summary>
public struct BuildingSelectedEvent
{
    public int GridX;
    public int GridY;
    public Building Building;
}
// --- Czas ---

/// <summary>
/// Zdarzenie wysyłane po upływie wirtualnego czasu w grze (np. nowa godzina/dzień).
/// </summary>
public struct TimeUpdatedEvent
{
    public int Hour;
    public int Day;
    public int Month;
    public int Year;
    public string DateString;
}

/// <summary>
/// Zdarzenie wysyłane na koniec miesiąca, używane np. do rozliczeń opłat.
/// </summary>
public struct MonthPassedEvent
{
    public int Month;
    public int Year;
}

/// <summary>
/// Zdarzenie płatności miesięcznych kosztów utrzymania za wszystkie budynki.
/// </summary>
public struct MaintenancePaidEvent
{
    public float TotalCost;
    public int Month;
    public int Year;
    public bool Success;
}

/// <summary>
/// Zdarzenie wysyłane po udanej sprzedaży produktu (przychód).
/// </summary>
public struct SaleCompletedEvent
{
    public string ProductId;
    public float Amount;
    public float Revenue;
    public float Price;
}