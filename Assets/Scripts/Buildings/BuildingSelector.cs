using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Obsługuje zaznaczanie budynków przez kliknięcie.
/// Zaznaczony budynek dostaje kolorową obramówkę.
/// Kliknięcie w pustą przestrzeń odznacza.
/// </summary>
public class BuildingSelector : MonoBehaviour
{
    public static BuildingSelector Instance { get; private set; }

    [Header("Referencje")]
    [SerializeField] private Camera _mainCamera;

    [Header("Zaznaczenie")]
    [SerializeField] private Color _selectionColor = new Color(1f, 0.85f, 0f, 1f);

    private Building _selectedBuilding;
    private SpriteRenderer _selectedRenderer;
    private Color _originalColor;

    public Building SelectedBuilding => _selectedBuilding;

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

        if (_mainCamera == null)
        Debug.LogError("[BuildingSelector] Brak Main Camera!");
    }

    void Update()
    {
        if (_mainCamera == null) return;  // ← DODAJ TĘ LINIĘ
        // Ignoruj kliknięcia gdy jesteśmy w trybie stawiania
        if (BuildingPlacer.Instance != null && BuildingPlacer.Instance.IsPlacing)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, -_mainCamera.transform.position.z));

        RaycastHit2D hit = Physics2D.Raycast(worldPos, Vector2.zero);

        if (hit.collider != null)
        {
            var building = hit.collider.GetComponent<Building>();
            if (building != null)
            {
                SelectBuilding(building);
                return;
            }
        }

        // Kliknięcie w pustą przestrzeń — odznacz
        DeselectBuilding();
    }

    private void SelectBuilding(Building building)
    {
        // Odznacz poprzedni
        DeselectBuilding();

        _selectedBuilding = building;

        // Dodaj obramówkę przez zmianę koloru SpriteRenderer
        _selectedRenderer = building.GetComponentInChildren<SpriteRenderer>();
        if (_selectedRenderer != null)
        {
            _originalColor = _selectedRenderer.color;
            _selectedRenderer.color = _selectionColor;
        }

        Debug.Log($"[BuildingSelector] Zaznaczono: {building.DisplayName}");

        EventBus.Publish(new BuildingSelectedEvent
        {
            GridX = building.GridX,
            GridY = building.GridY,
            Building = building
        });
    }

    private void DeselectBuilding()
    {
        if (_selectedBuilding == null) return;

        // Przywróć oryginalny kolor
        if (_selectedRenderer != null)
            _selectedRenderer.color = _originalColor;

        _selectedBuilding = null;
        _selectedRenderer = null;

        EventBus.Publish(new BuildingDeselectedEvent());
    }

    public void DeselectAll() => DeselectBuilding();
}