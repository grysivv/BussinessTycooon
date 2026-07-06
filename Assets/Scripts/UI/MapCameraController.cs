using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Sterowanie kamerą izometryczną.
/// Przesuwanie: WASD/strzałki + środkowy przycisk myszy (przeciąganie)
/// Zoom: scroll myszy
/// Granice: kamera nie może wyjechać poza obszar tilemapa terenu
/// </summary>
public class MapCameraController : MonoBehaviour
{
    [Header("Referencje")]
    [SerializeField] private Camera _camera;
    [SerializeField] private GridManager _gridManager;

    [Header("Przesuwanie klawiaturą")]
    [SerializeField] private float _keyboardSpeed = 5f;

    [Header("Przesuwanie myszką (środkowy przycisk)")]
    [SerializeField] private float _dragSpeed = 1f;

    [Header("Zoom")]
    [SerializeField] private float _zoomSpeed = 1f;
    [SerializeField] private float _minZoom = 3f;
    [SerializeField] private float _maxZoom = 15f;

    [Header("Granice (wypełniane automatycznie)")]
    [SerializeField] private Bounds _mapBounds;

    private Vector3 _dragOrigin;
    private bool _isDragging = false;

    void Awake()
    {
        if (_camera == null)
            _camera = Camera.main;
    }

    void Start()
    {
        CalculateMapBounds();
        ClampCamera();
    }

    void Update()
    {
        HandleKeyboardMovement();
        HandleMouseDrag();
        HandleZoom();
        ClampCamera();
    }

    // --- Granice mapy ---

    private void CalculateMapBounds()
    {
        if (_gridManager == null)
        {
            _gridManager = FindAnyObjectByType<GridManager>();
            if (_gridManager == null)
            {
                Debug.LogWarning("[MapCameraController] Brak GridManager!");
                return;
            }
        }

        // Pobierz granice z tilemapa terenu przez GridManager
        var terrainBounds = _gridManager.GetTerrainBounds();
        _mapBounds = terrainBounds;

        Debug.Log($"[MapCameraController] Granice mapy: {_mapBounds}");
    }

    private void ClampCamera()
    {
        if (_mapBounds.size == Vector3.zero) return;

        float halfHeight = _camera.orthographicSize;
        float halfWidth = _camera.orthographicSize * _camera.aspect;

        float minX = _mapBounds.min.x + halfWidth;
        float maxX = _mapBounds.max.x - halfWidth;
        float minY = _mapBounds.min.y + halfHeight;
        float maxY = _mapBounds.max.y - halfHeight;

        // Jeśli mapa jest mniejsza niż ekran — centruj
        float x = (minX > maxX)
            ? _mapBounds.center.x
            : Mathf.Clamp(_camera.transform.position.x, minX, maxX);

        float y = (minY > maxY)
            ? _mapBounds.center.y
            : Mathf.Clamp(_camera.transform.position.y, minY, maxY);

        _camera.transform.position = new Vector3(x, y, _camera.transform.position.z);
    }

    // --- Klawiatura ---

    private void HandleKeyboardMovement()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            vertical += 1f;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            vertical -= 1f;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            horizontal -= 1f;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            horizontal += 1f;

        if (horizontal == 0f && vertical == 0f) return;

        float speed = _keyboardSpeed * _camera.orthographicSize * Time.deltaTime;
        _camera.transform.position += new Vector3(horizontal, vertical, 0f) * speed;
    }

    // --- Przeciąganie myszką ---

    private void HandleMouseDrag()
    {
        // Środkowy przycisk myszy — rozpocznij przeciąganie
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            _isDragging = true;
            _dragOrigin = GetMouseWorldPosition();
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
            _isDragging = false;

        if (!_isDragging) return;

        Vector3 currentMousePos = GetMouseWorldPosition();
        Vector3 delta = _dragOrigin - currentMousePos;
        _camera.transform.position += delta;
    }

    // --- Zoom ---

    private void HandleZoom()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsPointerOverUI())
            return;
            
        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll == 0f) return;

        float newSize = _camera.orthographicSize - scroll * _zoomSpeed;
        _camera.orthographicSize = Mathf.Clamp(newSize, _minZoom, _maxZoom);
    }

    // --- Pomocnicze ---

    private Vector3 GetMouseWorldPosition()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        return _camera.ScreenToWorldPoint(
            new Vector3(mouseScreenPos.x, mouseScreenPos.y, -_camera.transform.position.z));
    }

    public void RefreshBounds()
    {
        CalculateMapBounds();
    }
}