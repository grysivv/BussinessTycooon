
// --- Tick ---

public struct TickEvent
{
    public int TickNumber;
    public float GameTime;
}

// --- Budynki ---
public struct BuildingPlacedEvent
{
    public int GridX;
    public int GridY;
}

public struct BuildingRemovedEvent
{
    public int GridX;
    public int GridY;
}

public struct BuildingSelectedEvent
{
    public int GridX;
    public int GridY;
}

public struct BuildingDeselectedEvent { }

// --- Produkcja ---
public struct ProductionCompletedEvent
{
    public string ProductId;
    public float Amount;
}

public struct ProductionFailedEvent
{
    public string ProductId;
    public string Reason;
}

// --- Ekonomia ---
public struct PriceChangedEvent
{
    public string ProductId;
    public float OldPrice;
    public float NewPrice;
}

public struct PlayerMoneyChangedEvent
{
    public float OldAmount;
    public float NewAmount;
    public float Delta;
}