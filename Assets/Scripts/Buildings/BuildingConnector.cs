using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Zarządza ręcznymi, jednokierunkowymi połączeniami między budynkami produkcyjnymi.
/// Gracz wybiera dwa budynki — system sam określa kierunek na podstawie
/// dopasowania output źródła do inputu celu.
/// </summary>
public class BuildingConnector : MonoBehaviour
{
    public static BuildingConnector Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Spróbuj połączyć dwa budynki. System sam ustala kierunek
    /// na podstawie tego, czy output jednego pasuje do inputu drugiego.
    /// </summary>
    public ConnectionResult TryConnect(ProductionBuilding buildingA, ProductionBuilding buildingB)
    {
        if (buildingA == null || buildingB == null)
            return ConnectionResult.Fail("Jeden z budynków nie istnieje.");

        if (buildingA == buildingB)
            return ConnectionResult.Fail("Nie można połączyć budynku z samym sobą.");

        bool aFeedsB = OutputMatchesInput(buildingA, buildingB);
        bool bFeedsA = OutputMatchesInput(buildingB, buildingA);

        if (aFeedsB && bFeedsA)
        {
            // Rzadki przypadek — oba pasują. Domyślnie wybieramy A → B.
            return Connect(buildingA, buildingB);
        }
        else if (aFeedsB)
        {
            return Connect(buildingA, buildingB);
        }
        else if (bFeedsA)
        {
            return Connect(buildingB, buildingA);
        }
        else
        {
            return ConnectionResult.Fail(
                $"Brak dopasowania receptur między " +
                $"'{buildingA.DisplayName}' a '{buildingB.DisplayName}'.");
        }
    }

    /// <summary>
    /// Czy output źródła (source) jest jednym ze składników receptury celu (target)?
    /// </summary>
    private bool OutputMatchesInput(ProductionBuilding source, ProductionBuilding target)
    {
        if (source.Recipe == null || target.Recipe == null)
            return false;

        string sourceOutput = source.Recipe.outputProductId;

        foreach (var ingredient in target.Recipe.inputs)
        {
            if (ingredient.productId == sourceOutput)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Wykonaj faktyczne połączenie: source → target (jednokierunkowo).
    /// </summary>
    private ConnectionResult Connect(ProductionBuilding source, ProductionBuilding target)
    {
        var currentConnections = target.ConnectedBuildings.ToList();

        if (currentConnections.Contains(source))
            return ConnectionResult.Fail("Połączenie już istnieje.");

        currentConnections.Add(source);
        target.SetConnectedBuildings(currentConnections);

        Debug.Log($"[BuildingConnector] Połączono: " +
                  $"{source.DisplayName} → {target.DisplayName} " +
                  $"({source.Recipe.outputProductId})");

        EventBus.Publish(new BuildingsConnectedEvent
        {
            SourceId = source.Id,
            TargetId = target.Id,
            ProductId = source.Recipe.outputProductId
        });

        return ConnectionResult.Ok(source, target);
    }

    /// <summary>
    /// Rozłącz dwa budynki (jeśli połączenie istnieje w dowolnym kierunku).
    /// </summary>
    public bool Disconnect(ProductionBuilding buildingA, ProductionBuilding buildingB)
    {
        bool removed = false;

        var aConnections = buildingA.ConnectedBuildings.ToList();
        if (aConnections.Remove(buildingB))
        {
            buildingA.SetConnectedBuildings(aConnections);
            removed = true;
        }

        var bConnections = buildingB.ConnectedBuildings.ToList();
        if (bConnections.Remove(buildingA))
        {
            buildingB.SetConnectedBuildings(bConnections);
            removed = true;
        }

        if (removed)
            Debug.Log($"[BuildingConnector] Rozłączono: " +
                      $"{buildingA.DisplayName} ↔ {buildingB.DisplayName}");

        return removed;
    }

    /// <summary>
    /// Usuń wszystkie połączenia danego budynku — wywołaj przy rozbiórce.
    /// </summary>
    public void DisconnectAll(ProductionBuilding building)
    {
        // Usuń jako źródło u wszystkich, którzy go mają na liście
        var allBuildings = FindObjectsByType<ProductionBuilding>(FindObjectsInactive.Exclude);

        foreach (var other in allBuildings)
        {
            if (other == building) continue;

            var connections = other.ConnectedBuildings.ToList();
            if (connections.Remove(building))
                other.SetConnectedBuildings(connections);
        }

        // Wyczyść własne połączenia
        building.SetConnectedBuildings(new List<ProductionBuilding>());
    }
}

/// <summary>
/// Wynik próby połączenia — informacja zwrotna dla UI.
/// </summary>
public class ConnectionResult
{
    public bool Success;
    public string Message;
    public ProductionBuilding Source;
    public ProductionBuilding Target;

    public static ConnectionResult Fail(string message)
        => new ConnectionResult { Success = false, Message = message };

    public static ConnectionResult Ok(ProductionBuilding source, ProductionBuilding target)
        => new ConnectionResult
        {
            Success = true,
            Message = $"Połączono {source.DisplayName} → {target.DisplayName}",
            Source = source,
            Target = target
        };
}