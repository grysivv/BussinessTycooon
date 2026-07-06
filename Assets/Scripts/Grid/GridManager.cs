using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Zarządza siatką gry 20x20.
/// Wie które kafelki są zajęte, obsługuje stawianie i usuwanie budynków.
/// Komunikacja z tilemapą Unity przez referencje w Inspectorze.
/// </summary>
public class GridManager : MonoBehaviour
{
    public static GridManager Instance { get; private set; }

    [Header("Konfiguracja siatki")]
    [SerializeField] private int _gridWidth  = 20;
    [SerializeField] private int _gridHeight = 20;

    [Header("Referencje Tilemap")]
    [SerializeField] private Tilemap _terrainTilemap;
    [SerializeField] private Tilemap _buildingsTilemap;
    [SerializeField] private Tilemap _overlayTilemap;

    [Header("Kafelki")]
    [SerializeField] private TileBase _highlightTile;
    [SerializeField] private TileBase _invalidTile;

    // Stan siatki — które kafelki są zajęte
    private bool[,] _occupiedCells;

    // Rozmiary
    public int GridWidth  => _gridWidth;
    public int GridHeight => _gridHeight;

    private int _gridOffsetX = 0;
    private int _gridOffsetY = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeGrid();
    }

    // --- Inicjalizacja ---

        private void InitializeGrid()
    {
        _terrainTilemap.CompressBounds();
        BoundsInt bounds = _terrainTilemap.cellBounds;

        _gridOffsetX = bounds.xMin;
        _gridOffsetY = bounds.yMin;
        _gridWidth = bounds.size.x;
        _gridHeight = bounds.size.y;

        _occupiedCells = new bool[_gridWidth, _gridHeight];

        Debug.Log($"[GridManager] Siatka wykryta: " +
                 $"offset({_gridOffsetX},{_gridOffsetY}) " +
                $"rozmiar({_gridWidth}x{_gridHeight})");
    }

    // --- Walidacja pozycji ---

    /// <summary>
    /// Zwraca granice (Bounds) lokalnego układu współrzędnych tilemapy terenu.
    /// Przydatne m.in. dla kamery, by nie wyjeżdżała poza planszę.
    /// </summary>
    public Bounds GetTerrainBounds()
    {
        if (_terrainTilemap == null) return new Bounds();

        _terrainTilemap.CompressBounds();
        return _terrainTilemap.localBounds;
    } 
     
    /// <summary>
    /// Sprawdza, czy podane współrzędne (x, y) mieszczą się w granicach siatki.
    /// </summary>
    public bool IsInBounds(int x, int y)
    {
        return x >= _gridOffsetX && x < _gridOffsetX + _gridWidth &&
        y >= _gridOffsetY && y < _gridOffsetY + _gridHeight;
    }

    /// <summary>
    /// Czy kafelek jest wolny?
    /// </summary>
    public bool IsEmpty(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        return !_occupiedCells[x - _gridOffsetX, y - _gridOffsetY];
    }

    /// <summary>
    /// Czy można postawić budynek na tej pozycji?
    /// </summary>
    public bool CanPlaceBuilding(int x, int y)
    {
        if (!IsInBounds(x, y)) return false;
        if (!IsEmpty(x, y)) return false;

        TileBase tille = _terrainTilemap.GetTile(new Vector3Int(x, y, 0));
        return tille != null; // Można stawiać tylko na kafelkach terenu
    }

    // --- Zajmowanie i zwalnianie kafelków ---

    /// <summary>
    /// Zajmij kafelek. Wywołaj gdy budynek zostaje postawiony.
    /// </summary>
    public bool OccupyCell(int x, int y)
    {
        if (!CanPlaceBuilding(x, y))
        {
            Debug.LogWarning($"[GridManager] Nie można zająć ({x},{y})");
            return false;
        }
        _occupiedCells[x - _gridOffsetX, y - _gridOffsetY] = true;
        EventBus.Publish(new BuildingPlacedEvent { GridX = x, GridY = y });
        return true;
    }

    /// <summary>
    /// Zwolnij kafelek. Wywołaj gdy budynek zostaje usunięty.
    /// </summary>
    public bool FreeCell(int x, int y)
    {
        if (!IsInBounds(x, y))
        {
        Debug.LogWarning($"[GridManager] Pozycja ({x},{y}) poza siatką");
        return false;
        }
        _occupiedCells[x - _gridOffsetX, y - _gridOffsetY] = false;
        EventBus.Publish(new BuildingRemovedEvent { GridX = x, GridY = y });
        return true;
    }
    // --- Konwersja współrzędnych ---

    /// <summary>
    /// Zamień współrzędne siatki na pozycję w świecie Unity.
    /// </summary>
    public Vector3 GridToWorld(int x, int y)
    {
        if (_terrainTilemap == null) return Vector3.zero;
        return _terrainTilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
    }

    /// <summary>
    /// Zamień pozycję w świecie na współrzędne siatki.
    /// </summary>
    public Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        if (_terrainTilemap == null) return Vector3Int.zero;
        return _terrainTilemap.WorldToCell(worldPosition);
    }

    // --- Overlay (podświetlanie kafelków) ---

    /// <summary>
    /// Podświetl kafelek pod kursorem podczas stawiania budynku.
    /// </summary>
    public void HighlightCell(int x, int y, bool isValid)
    {
        if (_overlayTilemap == null) return;

        ClearOverlay();

        TileBase tile = isValid ? _highlightTile : _invalidTile;
        if (tile != null)
            _overlayTilemap.SetTile(new Vector3Int(x, y, 0), tile);
    }

    /// <summary>
    /// Wyczyść overlay.
    /// </summary>
    public void ClearOverlay()
    {
        _overlayTilemap?.ClearAllTiles();
    }

    // --- Diagnostyka ---

    [ContextMenu("Log stan siatki")]
    public void LogGridState()
    {
        int occupied = 0;
        for (int x = 0; x < _gridWidth; x++)
            for (int y = 0; y < _gridHeight; y++)
                if (_occupiedCells[x, y]) occupied++;

        Debug.Log($"[GridManager] Zajęte: {occupied}/{_gridWidth * _gridHeight}");
    }

    private void OnDrawGizmos()
    {
        if (_occupiedCells == null) return;

        for (int x = 0; x < _gridWidth; x++)
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                if (_occupiedCells[x, y])
                {
                    Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                    Vector3 pos = GridToWorld(x, y);
                    Gizmos.DrawCube(pos, Vector3.one * 0.5f);
                }
            }
        }
    }
}


