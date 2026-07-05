using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Obsługuje tryb stawiania budynków: podgląd podążający za kursorem,
/// walidacja pozycji, faktyczne postawienie budynku po kliknięciu.
/// Używa bezpośrednio klas Input System (Mouse.current) — bez Action Map.
/// </summary>
public class BuildingPlacer : MonoBehaviour
{
    public static BuildingPlacer Instance { get; private set; }

    [Header("Referencje")]
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private GridManager _gridManager;
    [SerializeField] private PlayerManager _playerManager;
    [SerializeField] private RecipeDatabase _recipeDatabase;
    [SerializeField] private ProductDatabase _productDatabase;

    [Header("Podgląd")]
    [SerializeField] private SpriteRenderer _previewRenderer;
    [SerializeField] private Color _validColor = new Color(0f, 1f, 0f, 0.5f);
    [SerializeField] private Color _invalidColor = new Color(1f, 0f, 0f, 0.5f);

    private BuildingData _selectedBuilding;
    private GameObject _previewInstance;
    private bool _isPlacing = false;

    public bool IsPlacing => _isPlacing;



    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (_mainCamera == null)
            _mainCamera = Camera.main;
    }

    void Update()
    {
        if (!_isPlacing) return;

        Vector3Int gridPos = GetGridPositionUnderCursor();
        UpdatePreview(gridPos);

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryPlaceBuilding(gridPos);
        }

        if (Mouse.current.rightButton.wasPressedThisFrame ||
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelPlacement();
        }
    }

    

    // --- Rozpoczęcie trybu stawiania ---

    public void StartPlacing(BuildingData buildingData)
    {
        if (buildingData == null || buildingData.prefab == null)
        {
            Debug.LogError("[BuildingPlacer] Brak danych budynku lub prefaba!");
            return;
        }

        _selectedBuilding = buildingData;
        _isPlacing = true;

        CreatePreview();

        Debug.Log($"[BuildingPlacer] Tryb stawiania: {buildingData.displayName}");
    }

    public void CancelPlacement()
    {
        _isPlacing = false;
        _selectedBuilding = null;

        if (_previewInstance != null)
            Destroy(_previewInstance);

        _gridManager?.ClearOverlay();

        Debug.Log("[BuildingPlacer] Stawianie anulowane.");
    }

    // --- Podgląd ---

    private void CreatePreview()
    {
        if (_previewInstance != null)
            Destroy(_previewInstance);

        _previewInstance = Instantiate(_selectedBuilding.prefab);

        var productionComponent = _previewInstance.GetComponent<ProductionBuilding>();
        if (productionComponent != null)
            productionComponent.enabled = false;

        var collider = _previewInstance.GetComponent<Collider2D>();
        if (collider != null)
            collider.enabled = false;
    }

    private void UpdatePreview(Vector3Int gridPos)
    {
        if (_previewInstance == null) return;


        Vector3 worldPos = _gridManager.GridToWorld(gridPos.x, gridPos.y);
        _previewInstance.transform.position = worldPos;

        bool canPlace = _gridManager.CanPlaceBuilding(gridPos.x, gridPos.y);

        var renderer = _previewInstance.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = canPlace ? _validColor : _invalidColor;

        _gridManager.HighlightCell(gridPos.x, gridPos.y, canPlace);
    }

    // --- Stawianie ---

    private void TryPlaceBuilding(Vector3Int gridPos)
    {
        Debug.Log($"[DEBUG] Próba postawienia na ({gridPos.x}, {gridPos.y}) | InBounds: {_gridManager.IsInBounds(gridPos.x, gridPos.y)}");

        if (!_gridManager.CanPlaceBuilding(gridPos.x, gridPos.y))
        {
            Debug.Log("[BuildingPlacer] Nie można postawić tutaj.");
            return;
        }

        if (!_playerManager.TrySpend(_selectedBuilding.constructionCost))
        {
            Debug.Log("[BuildingPlacer] Brak środków na budowę.");
            return;
        }

        _gridManager.OccupyCell(gridPos.x, gridPos.y);

        Vector3 worldPos = _gridManager.GridToWorld(gridPos.x, gridPos.y);
        GameObject buildingInstance = Instantiate(_selectedBuilding.prefab, worldPos, Quaternion.identity);
        buildingInstance.name = $"{_selectedBuilding.displayName}_{gridPos.x}_{gridPos.y}";
        
         if (_previewInstance != null)
            Destroy(_previewInstance);

        var productionBuilding = buildingInstance.GetComponent<ProductionBuilding>();
        if (productionBuilding != null)
        {
        productionBuilding.Initialize(
            _selectedBuilding.id,
            _selectedBuilding.displayName,
            gridPos.x,
            gridPos.y,
            _selectedBuilding.constructionCost,
            _selectedBuilding.demolitionCost,
            _selectedBuilding.monthlyMaintenance,
            _selectedBuilding.monthlyWages
        );
        if (_selectedBuilding.recipe != null)
            productionBuilding.InitializeProduction(_selectedBuilding.recipe, _productDatabase);
        }
else
{
    var building = buildingInstance.GetComponent<Building>();
    building?.Initialize(
        _selectedBuilding.id,
        _selectedBuilding.displayName,
        gridPos.x,
        gridPos.y,
        _selectedBuilding.constructionCost,
        _selectedBuilding.demolitionCost,
        _selectedBuilding.monthlyMaintenance,
        _selectedBuilding.monthlyWages
    );
}

        Debug.Log($"[BuildingPlacer] Postawiono: {_selectedBuilding.displayName} " +
                  $"na ({gridPos.x},{gridPos.y})");

        EventBus.Publish(new BuildingPlacedEvent
        {
            GridX = gridPos.x,
            GridY = gridPos.y
        });

         CreatePreview();

        UpdatePreview(gridPos);
    }

    // --- Pomocnicze ---

    private Vector3Int GetGridPositionUnderCursor()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, _mainCamera.nearClipPlane + 1f));
        mouseWorldPos.z = 0f;

        Vector3Int gridPos = _gridManager.WorldToGrid(mouseWorldPos);

        return _gridManager.WorldToGrid(mouseWorldPos);
    }
}