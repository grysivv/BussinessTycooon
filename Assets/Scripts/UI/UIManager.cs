using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Centralny zarządca UI. Jedyny punkt otwierania i zamykania paneli.
/// Buduje całe UI programowo przez kod C# — bez plików UXML/USS.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Referencje")]
    [SerializeField] private UIDocument _uiDocument;
    [SerializeField] private BuildingDatabase _buildingDatabase;
    [SerializeField] private BuildingPlacer _buildingPlacer;

    // Root element całego UI
    private VisualElement _root;

    // Panele
    private VisualElement _hudPanel;
    private VisualElement _buildMenuPanel;
    private VisualElement _buildingInfoPanel;

    // Elementy HUD
    private Label _moneyLabel;
    private Label _incomeLabel;
    private Label _tickLabel;

    // Aktualnie wybrany budynek (do panelu info)
    private Building _currentSelectedBuilding;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

    // Znajdź UIDocument automatycznie na tym samym obiekcie
        if (_uiDocument == null)
            _uiDocument = GetComponent<UIDocument>();

        if (_uiDocument == null)
        {
            Debug.LogError("[UIManager] Brak UIDocument na tym obiekcie!");
        }
    }

    void Start()
    {
        _root = _uiDocument.rootVisualElement;
        _root.Clear();

        BuildStyles();
        BuildHUD();
        BuildBuildMenu();
        BuildBuildingInfoPanel();

        Debug.Log("[UIManager] UI zbudowane.");
    }

    void OnEnable()
    {
        EventBus.Subscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Subscribe<TickEvent>(OnTick);
        EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
        EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        EventBus.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Subscribe<TimeUpdatedEvent>(OnTimeUpdated);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
        EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        EventBus.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Unsubscribe<TimeUpdatedEvent>(OnTimeUpdated);
    }

    // --- Budowanie stylów globalnych ---

    private void BuildStyles()
    {
        // Globalne style przez USS string — aplikowane do roota
        _root.style.width = Length.Percent(100);
        _root.style.height = Length.Percent(100);
        _root.style.flexDirection = FlexDirection.Column;
        _root.style.justifyContent = Justify.SpaceBetween;
    }

    // --- HUD (górny pasek) ---

    private void BuildHUD()
    {
        _hudPanel = new VisualElement();
        _hudPanel.style.flexDirection = FlexDirection.Row;
        _hudPanel.style.justifyContent = Justify.SpaceBetween;
        _hudPanel.style.alignItems = Align.Center;
        _hudPanel.style.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.92f);
        _hudPanel.style.paddingLeft = 16;
        _hudPanel.style.paddingRight = 16;
        _hudPanel.style.paddingTop = 8;
        _hudPanel.style.paddingBottom = 8;
        _hudPanel.style.height = 48;

        // Lewa strona: pieniądze
        var leftGroup = new VisualElement();
        leftGroup.style.flexDirection = FlexDirection.Row;
        leftGroup.style.alignItems = Align.Center;

        _moneyLabel = new Label("$50 000");
        _moneyLabel.style.color = new Color(0.9f, 0.95f, 1f);
        _moneyLabel.style.fontSize = 18;
        _moneyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _moneyLabel.style.marginRight = 24;

        _incomeLabel = new Label("+$0/tick");
        _incomeLabel.style.color = new Color(0.3f, 0.85f, 0.5f);
        _incomeLabel.style.fontSize = 13;

        leftGroup.Add(_moneyLabel);
        leftGroup.Add(_incomeLabel);

        // Prawa strona: czas i kontrola prędkości
        var rightGroup = new VisualElement();
        rightGroup.style.flexDirection = FlexDirection.Row;
        rightGroup.style.alignItems = Align.Center;

        _tickLabel = new Label("Tick: 0");
        _tickLabel.style.color = new Color(0.6f, 0.7f, 0.8f);
        _tickLabel.style.fontSize = 13;
        _tickLabel.style.marginRight = 16;

        // Przyciski prędkości
        var speedGroup = new VisualElement();
        speedGroup.style.flexDirection = FlexDirection.Row;

        foreach (var (label, speed) in new (string, float)[]
            { ("⏸", 0f), ("×1", 1f), ("×3", 3f) })
        {
            var btn = CreateSpeedButton(label, speed);
            speedGroup.Add(btn);
        }

        rightGroup.Add(_tickLabel);
        rightGroup.Add(speedGroup);

        _hudPanel.Add(leftGroup);
        _hudPanel.Add(rightGroup);
        _root.Add(_hudPanel);
    }

    private Button CreateSpeedButton(string label, float speed)
    {
        var btn = new Button(() => OnSpeedButtonClicked(speed));
        btn.text = label;
        btn.style.backgroundColor = new Color(0.12f, 0.16f, 0.25f);
        btn.style.color = new Color(0.8f, 0.85f, 0.9f);
        btn.style.borderTopWidth = 1;
        btn.style.borderBottomWidth = 1;
        btn.style.borderLeftWidth = 1;
        btn.style.borderRightWidth = 1;
        btn.style.borderTopColor = new Color(0.25f, 0.32f, 0.48f);
        btn.style.borderBottomColor = new Color(0.25f, 0.32f, 0.48f);
        btn.style.borderLeftColor = new Color(0.25f, 0.32f, 0.48f);
        btn.style.borderRightColor = new Color(0.25f, 0.32f, 0.48f);
        btn.style.marginLeft = 4;
        btn.style.paddingLeft = 10;
        btn.style.paddingRight = 10;
        btn.style.height = 28;
        btn.style.borderTopLeftRadius = 4;
        btn.style.borderTopRightRadius = 4;
        btn.style.borderBottomLeftRadius = 4;
        btn.style.borderBottomRightRadius = 4;
        return btn;
    }

    // --- Build Menu (dolny pasek) ---

    private void BuildBuildMenu()
    {
        _buildMenuPanel = new VisualElement();
        _buildMenuPanel.style.flexDirection = FlexDirection.Row;
        _buildMenuPanel.style.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.92f);
        _buildMenuPanel.style.paddingLeft = 12;
        _buildMenuPanel.style.paddingRight = 12;
        _buildMenuPanel.style.paddingTop = 8;
        _buildMenuPanel.style.paddingBottom = 8;
        _buildMenuPanel.style.height = 90;
        _buildMenuPanel.style.alignItems = Align.Center;

        if (_buildingDatabase == null)
        {
            Debug.LogWarning("[UIManager] Brak BuildingDatabase!");
            _root.Add(_buildMenuPanel);
            return;
        }

        foreach (var buildingData in _buildingDatabase.AllBuildings)
        {
            var btn = CreateBuildingButton(buildingData);
            _buildMenuPanel.Add(btn);
        }

        _root.Add(_buildMenuPanel);
    }

    private VisualElement CreateBuildingButton(BuildingData data)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.alignItems = Align.Center;
        container.style.backgroundColor = new Color(0.12f, 0.16f, 0.25f);
        container.style.borderTopWidth = 1;
        container.style.borderBottomWidth = 1;
        container.style.borderLeftWidth = 1;
        container.style.borderRightWidth = 1;
        container.style.borderTopColor = new Color(0.25f, 0.32f, 0.48f);
        container.style.borderBottomColor = new Color(0.25f, 0.32f, 0.48f);
        container.style.borderLeftColor = new Color(0.25f, 0.32f, 0.48f);
        container.style.borderRightColor = new Color(0.25f, 0.32f, 0.48f);
        container.style.borderTopLeftRadius = 6;
        container.style.borderTopRightRadius = 6;
        container.style.borderBottomLeftRadius = 6;
        container.style.borderBottomRightRadius = 6;
        container.style.paddingLeft = 10;
        container.style.paddingRight = 10;
        container.style.paddingTop = 6;
        container.style.paddingBottom = 6;
        container.style.marginRight = 8;
        container.style.width = 90;

        var nameLabel = new Label(data.displayName);
        nameLabel.style.color = new Color(0.9f, 0.95f, 1f);
        nameLabel.style.fontSize = 12;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        nameLabel.style.marginTop = 6;

        var costLabel = new Label($"${data.constructionCost:F0}");
        costLabel.style.color = new Color(0.3f, 0.85f, 0.5f);
        costLabel.style.fontSize = 11;
        costLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

        container.Add(nameLabel);
        container.Add(costLabel);

        // Kliknięcie uruchamia tryb stawiania
        container.RegisterCallback<ClickEvent>(evt =>
        {
            _buildingPlacer?.StartPlacing(data);
        });

        // Hover effect
        container.RegisterCallback<MouseEnterEvent>(evt =>
        {
            container.style.backgroundColor = new Color(0.18f, 0.24f, 0.38f);
        });
        container.RegisterCallback<MouseLeaveEvent>(evt =>
        {
            container.style.backgroundColor = new Color(0.12f, 0.16f, 0.25f);
        });

        return container;
    }

    // --- Building Info Panel (prawy panel) ---

    private void BuildBuildingInfoPanel()
    {
        _buildingInfoPanel = new VisualElement();
        _buildingInfoPanel.style.position = Position.Absolute;
        _buildingInfoPanel.style.right = 0;
        _buildingInfoPanel.style.top = 48;
        _buildingInfoPanel.style.bottom = 90;
        _buildingInfoPanel.style.width = 220;
        _buildingInfoPanel.style.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.92f);
        _buildingInfoPanel.style.paddingLeft = 14;
        _buildingInfoPanel.style.paddingRight = 14;
        _buildingInfoPanel.style.paddingTop = 14;
        _buildingInfoPanel.style.display = DisplayStyle.None;

        _root.Add(_buildingInfoPanel);
    }

    // --- Aktualizacja paneli ---

    private void OnMoneyChanged(PlayerMoneyChangedEvent e)
    {
        if (_moneyLabel == null) return;
        _moneyLabel.text = $"${e.NewAmount:F0}";

        // Kolor czerwony gdy mało pieniędzy
        _moneyLabel.style.color = e.NewAmount < 5000
            ? new Color(0.9f, 0.3f, 0.3f)
            : new Color(0.9f, 0.95f, 1f);
    }

    private void OnTick(TickEvent e)
    {
        // Aktualizacja przychodów
        if (_currentSelectedBuilding != null)
            UpdateBuildingInfoPanel(_currentSelectedBuilding);
    }

    private void OnBuildingSelected(BuildingSelectedEvent e)
    {
        _currentSelectedBuilding = e.Building;
        _buildingInfoPanel.style.display = DisplayStyle.Flex;
        UpdateBuildingInfoPanel(e.Building);
    }

    private void UpdateBuildingInfoPanel(Building building)
    {
        _buildingInfoPanel.Clear();

        if (building == null) return;

        // Nagłówek
        var header = new VisualElement();
        header.style.marginBottom = 10;

        var typeLabel = new Label(GetBuildingType(building));
        typeLabel.style.color = new Color(0.5f, 0.65f, 0.8f);
        typeLabel.style.fontSize = 11;

        var nameLabel = new Label(building.DisplayName);
        nameLabel.style.color = new Color(0.9f, 0.95f, 1f);
        nameLabel.style.fontSize = 16;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        var posLabel = new Label($"({building.GridX}, {building.GridY})");
        posLabel.style.color = new Color(0.5f, 0.6f, 0.7f);
        posLabel.style.fontSize = 11;

        header.Add(typeLabel);
        header.Add(nameLabel);
        header.Add(posLabel);
        _buildingInfoPanel.Add(header);

        AddSeparator();

    // Dane produkcji
        var pb = building as ProductionBuilding;
        if (pb != null && pb.Recipe != null)
        {
            AddSectionLabel("PRODUKCJA");
            AddInfoRow("Produkt", pb.Recipe.outputProductId);
            AddInfoRow("Tempo", $"{pb.Recipe.outputAmount} j / {pb.Recipe.productionTimeTicks} tick");
            AddInfoRow("Magazyn", $"{pb.GetStorageAmount(pb.Recipe.outputProductId):F0} / {pb.StorageCapacity:F0} j");

            if (pb.Recipe.inputs.Count > 0)
            {
                AddSeparator();
                AddSectionLabel("SUROWCE");
                foreach (var input in pb.Recipe.inputs)
                    AddInfoRow(input.productId, $"{pb.GetAvailableFromPool(input.productId):F0} dostępne");
            }    

            AddSeparator();
            AddInfoRow("Połączenia", $"{pb.ConnectedBuildings.Count}");
        }

        AddSeparator();

    // Ekonomia
        AddSectionLabel("EKONOMIA");
        AddInfoRow("Koszt budowy", $"${building.ConstructionCost:F0}");
        AddInfoRow("Utrzymanie", $"${building.MonthlyMaintenance:F0}/mies.");
        AddInfoRow("Poziom", $"{building.Level}");

    // Przycisk zamknięcia
        var closeBtn = new Button(() => {
            _buildingInfoPanel.style.display = DisplayStyle.None;
        });
        closeBtn.text = "✕ Zamknij";
        closeBtn.style.marginTop = 12;
        closeBtn.style.backgroundColor = new Color(0.15f, 0.2f, 0.32f);
        closeBtn.style.color = new Color(0.7f, 0.75f, 0.85f);
        closeBtn.style.borderTopWidth = 0;
        closeBtn.style.borderBottomWidth = 0;
        closeBtn.style.borderLeftWidth = 0;
        closeBtn.style.borderRightWidth = 0;
        closeBtn.style.height = 28;
        _buildingInfoPanel.Add(closeBtn);
    }

    private string GetBuildingType(Building building)
    {
        if (building is ProductionBuilding pb && pb.Recipe != null)
        {
            var category = pb.Recipe.outputProductId;
            return "Budynek produkcyjny";
        }
        return "Budynek";
    }

    private void AddSeparator()
    {
        var sep = new VisualElement();
        sep.style.height = 1;
        sep.style.backgroundColor = new Color(0.2f, 0.27f, 0.4f);
        sep.style.marginTop = 8;
        sep.style.marginBottom = 8;
        _buildingInfoPanel.Add(sep);
    }

    private void AddSectionLabel(string text)
    {
        var label = new Label(text);
        label.style.color = new Color(0.45f, 0.58f, 0.75f);
        label.style.fontSize = 10;
        label.style.marginBottom = 5;
        _buildingInfoPanel.Add(label);
    }

    private void AddInfoRow(string labelText, string valueText)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var lbl = new Label(labelText);
        lbl.style.color = new Color(0.55f, 0.65f, 0.75f);
        lbl.style.fontSize = 12;

        var val = new Label(valueText);
        val.style.color = new Color(0.9f, 0.95f, 1f);
        val.style.fontSize = 12;
        val.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(lbl);
        row.Add(val);
        _buildingInfoPanel.Add(row);

    }

    private void OnTimeUpdated(TimeUpdatedEvent e)
    {
        if (_tickLabel == null) return;
        _tickLabel.text = e.DateString;
    }

    private void OnBuildingDeselected(BuildingDeselectedEvent e)
    {
        _currentSelectedBuilding = null;
        _buildingInfoPanel.style.display = DisplayStyle.None;
    }

    private void OnSpeedButtonClicked(float speed)
    {
        GameManager.Instance?.SetGameSpeed(speed);
    }
    private void OnProductionCompleted(ProductionCompletedEvent e)
    {
        if (_currentSelectedBuilding == null) return;
        UpdateBuildingInfoPanel(_currentSelectedBuilding);
    }

    // --- Publiczne metody ---

    public void ShowBuildingInfo(Building building)
    {
        // Wypełnimy szczegółami w kolejnym kroku
        _buildingInfoPanel.style.display = DisplayStyle.Flex;
    }

    public void HideBuildingInfo()
    {
        _buildingInfoPanel.style.display = DisplayStyle.None;
    }
    
}