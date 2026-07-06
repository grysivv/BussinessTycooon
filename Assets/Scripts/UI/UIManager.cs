using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

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
    private VisualElement _buildingInfoWindow;
    private ScrollView _infoBody;
    private VisualElement _activeColumn;
    private VisualElement _dashboardView;
    private bool _isDashboardOpen = false;
    private Dictionary<string, LineChart> _dashboardCharts = new();

    // Elementy HUD
    private Label _moneyLabel;
    private Label _incomeLabel;
    private Label _tickLabel;

    // Aktualnie wybrany budynek (do panelu info)
    private Building _currentSelectedBuilding;
    private float _incomeThisTick = 0f;

    private Label _panelStorageLabel;
    private Label _panelMarginLabel;
    private Label _panelPaybackLabel;
    private Label _panelMonthlyProfitLabel;
    private Label _panelAvgThroughputLabel;
    private Slider _panelPriceSlider;
    private UnityEngine.UIElements.TextField _panelPriceValueLabel;

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
        BuildBuildingInfoWindow();
        BuildDashboard();

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
        EventBus.Subscribe<TickEvent>(OnDashboardTick);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
        EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        EventBus.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Unsubscribe<TimeUpdatedEvent>(OnTimeUpdated);
        EventBus.Unsubscribe<TickEvent>(OnDashboardTick);
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

        // Przyciski prędkości i dashboard
        var speedGroup = new VisualElement();
        speedGroup.style.flexDirection = FlexDirection.Row;

        foreach (var (label, speed) in new (string, float)[]
            { ("⏸", 0f), ("×1", 1f), ("×3", 3f) })
        {
            var btn = CreateSpeedButton(label, speed);
            speedGroup.Add(btn);
        }

        var economyBtn = new Button(() => ToggleDashboard());
        economyBtn.text = "[Economy]";
        economyBtn.style.backgroundColor = new Color(0.12f, 0.16f, 0.25f);
        economyBtn.style.color = new Color(0.8f, 0.85f, 0.9f);
        economyBtn.style.borderTopWidth = 1;
        economyBtn.style.borderBottomWidth = 1;
        economyBtn.style.borderLeftWidth = 1;
        economyBtn.style.borderRightWidth = 1;
        economyBtn.style.borderTopColor = new Color(0.25f, 0.32f, 0.48f);
        economyBtn.style.borderBottomColor = new Color(0.25f, 0.32f, 0.48f);
        economyBtn.style.borderLeftColor = new Color(0.25f, 0.32f, 0.48f);
        economyBtn.style.borderRightColor = new Color(0.25f, 0.32f, 0.48f);
        economyBtn.style.marginLeft = 8;
        economyBtn.style.paddingLeft = 10;
        economyBtn.style.paddingRight = 10;
        economyBtn.style.height = 28;
        economyBtn.style.borderTopLeftRadius = 4;
        economyBtn.style.borderTopRightRadius = 4;
        economyBtn.style.borderBottomLeftRadius = 4;
        economyBtn.style.borderBottomRightRadius = 4;
        speedGroup.Add(economyBtn);

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

    // --- Dashboard ---

    private void ToggleDashboard()
    {
        _isDashboardOpen = !_isDashboardOpen;
        Debug.Log($"[Dashboard] Toggle: {_isDashboardOpen}");

        if (_isDashboardOpen)
        {
            _hudPanel.style.display = DisplayStyle.None;
            _buildMenuPanel.style.display = DisplayStyle.None;
            _buildingInfoWindow.style.display = DisplayStyle.None;
            _dashboardView.style.display = DisplayStyle.Flex;
        }
        else
        {
            _dashboardView.style.display = DisplayStyle.None;
            _hudPanel.style.display = DisplayStyle.Flex;
            _buildMenuPanel.style.display = DisplayStyle.Flex;
        }
    }

    private void BuildDashboard()
    {
        _dashboardView = new VisualElement();
        _dashboardView.style.position = Position.Absolute;
        _dashboardView.style.left = 0;
        _dashboardView.style.top = 0;
        _dashboardView.style.width = Length.Percent(100);
        _dashboardView.style.height = Length.Percent(100);
        _dashboardView.style.display = DisplayStyle.None;
        _dashboardView.style.flexDirection = FlexDirection.Column;
        _dashboardView.style.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.98f);

        // Top bar: Close button
        var topBar = new VisualElement();
        topBar.style.height = 48;
        topBar.style.backgroundColor = new Color(0.1f, 0.12f, 0.18f, 0.95f);
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.alignItems = Align.Center;
        topBar.style.paddingLeft = 16;
        topBar.style.paddingRight = 16;

        var titleLabel = new Label("ECONOMY DASHBOARD");
        titleLabel.style.fontSize = 18;
        titleLabel.style.color = new Color(0.45f, 0.58f, 0.75f);
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.flexGrow = 1;
        topBar.Add(titleLabel);

        var closeBtn = new Button(() => ToggleDashboard());
        closeBtn.text = "✕";
        closeBtn.style.width = 40;
        closeBtn.style.height = 28;
        closeBtn.style.backgroundColor = new Color(0.15f, 0.2f, 0.32f);
        closeBtn.style.color = new Color(0.7f, 0.75f, 0.85f);
        topBar.Add(closeBtn);

        _dashboardView.Add(topBar);

        // Content area with two columns
        var content = new VisualElement();
        content.style.flexGrow = 1;
        content.style.flexDirection = FlexDirection.Row;
        content.style.paddingLeft = 16;
        content.style.paddingRight = 16;
        content.style.paddingTop = 16;
        content.style.paddingBottom = 16;

        // Left column: Product selector
        var leftCol = new VisualElement();
        leftCol.style.flexBasis = 0;
        leftCol.style.flexGrow = 1;
        leftCol.style.marginRight = 16;
        leftCol.style.maxWidth = new Length(300, LengthUnit.Pixel);
        leftCol.style.backgroundColor = new Color(0.10f, 0.13f, 0.2f, 0.9f);
        leftCol.style.borderTopWidth = 1;
        leftCol.style.borderBottomWidth = 1;
        leftCol.style.borderLeftWidth = 1;
        leftCol.style.borderRightWidth = 1;
        leftCol.style.borderTopColor = new Color(0.2f, 0.27f, 0.4f);
        leftCol.style.borderBottomColor = new Color(0.2f, 0.27f, 0.4f);
        leftCol.style.borderLeftColor = new Color(0.2f, 0.27f, 0.4f);
        leftCol.style.borderRightColor = new Color(0.2f, 0.27f, 0.4f);
        leftCol.style.borderTopLeftRadius = 4;
        leftCol.style.borderTopRightRadius = 4;
        leftCol.style.borderBottomLeftRadius = 4;
        leftCol.style.borderBottomRightRadius = 4;
        leftCol.style.paddingLeft = 12;
        leftCol.style.paddingRight = 12;
        leftCol.style.paddingTop = 12;
        leftCol.style.paddingBottom = 12;

        var label = new Label("PRODUKTY");
        label.style.fontSize = 12;
        label.style.color = new Color(0.45f, 0.58f, 0.75f);
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.style.marginBottom = 8;
        leftCol.Add(label);

        var productDb2 = EconomyManager.Instance?.ProductDatabase;
        if (productDb2 != null)
        {
            foreach (var product in productDb2.AllProducts)
            {
                var toggle = new Toggle(product.name);
                toggle.value = true;
                toggle.style.marginBottom = 4;
                toggle.style.color = new Color(0.9f, 0.95f, 1f);
                leftCol.Add(toggle);
            }
        }

        // Right column: Charts and stats
        var rightCol = new VisualElement();
        rightCol.style.flexBasis = 0;
        rightCol.style.flexGrow = 2;
        rightCol.style.flexDirection = FlexDirection.Column;

        var statsLabel = new Label("STATYSTYKI PRODUKTÓW");
        statsLabel.style.fontSize = 12;
        statsLabel.style.color = new Color(0.45f, 0.58f, 0.75f);
        statsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        statsLabel.style.marginBottom = 12;
        rightCol.Add(statsLabel);

        // Simple table/list of product stats
        var statsScroll = new ScrollView(ScrollViewMode.Vertical);
        statsScroll.style.flexGrow = 1;
        statsScroll.style.backgroundColor = new Color(0.10f, 0.13f, 0.2f, 0.9f);
        statsScroll.style.borderTopWidth = 1;
        statsScroll.style.borderBottomWidth = 1;
        statsScroll.style.borderLeftWidth = 1;
        statsScroll.style.borderRightWidth = 1;
        statsScroll.style.borderTopColor = new Color(0.2f, 0.27f, 0.4f);
        statsScroll.style.borderBottomColor = new Color(0.2f, 0.27f, 0.4f);
        statsScroll.style.borderLeftColor = new Color(0.2f, 0.27f, 0.4f);
        statsScroll.style.borderRightColor = new Color(0.2f, 0.27f, 0.4f);
        statsScroll.style.borderTopLeftRadius = 4;
        statsScroll.style.borderTopRightRadius = 4;
        statsScroll.style.borderBottomLeftRadius = 4;
        statsScroll.style.borderBottomRightRadius = 4;
        statsScroll.style.paddingLeft = 12;
        statsScroll.style.paddingRight = 12;
        statsScroll.style.paddingTop = 12;
        statsScroll.style.paddingBottom = 12;

        var productDb = EconomyManager.Instance?.ProductDatabase;
        if (productDb != null)
        {
            Debug.Log($"[Dashboard] Creating charts for {productDb.AllProducts.Count} products");
            foreach (var product in productDb.AllProducts)
            {
                var container = new VisualElement();
                container.style.flexDirection = FlexDirection.Column;
                container.style.marginBottom = 16;
                container.style.paddingBottom = 8;
                container.style.borderBottomWidth = 1;
                container.style.borderBottomColor = new Color(0.2f, 0.27f, 0.4f);

                var nameLabel = new Label(product.name);
                nameLabel.style.color = new Color(0.9f, 0.95f, 1f);
                nameLabel.style.fontSize = 11;
                nameLabel.style.marginBottom = 6;

                var chart = new LineChart();
                chart.style.width = Length.Percent(100);
                chart.style.height = 100;
                _dashboardCharts[product.id] = chart;
                Debug.Log($"[Dashboard] Created chart for {product.name}");

                container.Add(nameLabel);
                container.Add(chart);
                statsScroll.Add(container);
            }
        }

        rightCol.Add(statsScroll);

        content.Add(leftCol);
        content.Add(rightCol);
        _dashboardView.Add(content);

        _root.Add(_dashboardView);
    }

    private void OnDashboardTick(TickEvent e)
    {
        Debug.Log($"[Dashboard] OnDashboardTick called: open={_isDashboardOpen}, display={_dashboardView.style.display}");

        if (!(_isDashboardOpen && _dashboardView.style.display == DisplayStyle.Flex))
            return;

        var stats = StatisticsManager.Instance;
        if (stats == null) { Debug.LogWarning("[Dashboard] Brak StatisticsManager w scenie!"); return; }

        var productDb = EconomyManager.Instance?.ProductDatabase;
        if (productDb == null) { Debug.LogWarning("[Dashboard] Brak ProductDatabase!"); return; }

        foreach (var product in productDb.AllProducts)
        {
            if (_dashboardCharts.TryGetValue(product.id, out var chart))
            {
                var history = stats.GetHourlyPriceHistory(product.id);
                Debug.Log($"[Dashboard] {product.name}: {history.Count} price points");
                chart.SetData(history);
            }
            else
            {
                Debug.LogWarning($"[Dashboard] Brak wykresu dla id='{product.id}'");
            }
        }
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

    // --- Building Info Window (popup, nie panel) ---

    private void BuildBuildingInfoWindow()
    {
        _buildingInfoWindow = new VisualElement();
        _buildingInfoWindow.style.position = Position.Absolute;
        _buildingInfoWindow.style.left = new Length(50, LengthUnit.Percent);
        _buildingInfoWindow.style.top = new Length(50, LengthUnit.Percent);
        _buildingInfoWindow.style.translate = new Translate(new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        _buildingInfoWindow.style.width = 760;
        _buildingInfoWindow.style.height = 560;
        _buildingInfoWindow.style.backgroundColor = new Color(0.08f, 0.10f, 0.15f, 0.95f);
        _buildingInfoWindow.style.borderTopWidth = 2;
        _buildingInfoWindow.style.borderBottomWidth = 2;
        _buildingInfoWindow.style.borderLeftWidth = 2;
        _buildingInfoWindow.style.borderRightWidth = 2;
        _buildingInfoWindow.style.borderTopColor = new Color(0.3f, 0.45f, 0.65f);
        _buildingInfoWindow.style.borderBottomColor = new Color(0.3f, 0.45f, 0.65f);
        _buildingInfoWindow.style.borderLeftColor = new Color(0.3f, 0.45f, 0.65f);
        _buildingInfoWindow.style.borderRightColor = new Color(0.3f, 0.45f, 0.65f);
        _buildingInfoWindow.style.borderTopLeftRadius = 8;
        _buildingInfoWindow.style.borderTopRightRadius = 8;
        _buildingInfoWindow.style.borderBottomLeftRadius = 8;
        _buildingInfoWindow.style.borderBottomRightRadius = 8;
        _buildingInfoWindow.style.paddingLeft = 14;
        _buildingInfoWindow.style.paddingRight = 14;
        _buildingInfoWindow.style.paddingTop = 14;
        _buildingInfoWindow.style.paddingBottom = 14;
        _buildingInfoWindow.style.display = DisplayStyle.None;

        _root.Add(_buildingInfoWindow);
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
        _incomeThisTick = 0f;
        if (_currentSelectedBuilding != null)
            RefreshBuildingInfoPanel(_currentSelectedBuilding);
    }

    private void OnBuildingSelected(BuildingSelectedEvent e)
    {
        _currentSelectedBuilding = e.Building;
        _buildingInfoWindow.style.display = DisplayStyle.Flex;
        BuildBuildingInfoWindowContent(e.Building);
    }
    
    private void BuildBuildingInfoWindowContent(Building building)
    {
        _buildingInfoWindow.Clear();

        if (building == null) return;

        // Cała treść trafia do ScrollView
        _infoBody = new ScrollView(ScrollViewMode.Vertical);
        _infoBody.style.flexGrow = 1;
        _buildingInfoWindow.Add(_infoBody);

        var scrollbar = _infoBody.Q<ScrollView>()?.verticalScroller;
        if (scrollbar != null)
        {
            scrollbar.style.width = 8;
            scrollbar.style.backgroundColor = new Color(0.15f, 0.2f, 0.32f);
        }

        // === UKŁAD DWUKOLUMNOWY ===
        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.flexGrow = 1;
        _infoBody.Add(columns);

        var leftColumn = new VisualElement();
        leftColumn.style.flexGrow = 1;
        leftColumn.style.flexBasis = 0;
        leftColumn.style.marginRight = 8;

        var rightColumn = new VisualElement();
        rightColumn.style.flexGrow = 1;
        rightColumn.style.flexBasis = 0;
        rightColumn.style.marginLeft = 8;

        columns.Add(leftColumn);
        columns.Add(rightColumn);

        // Domyślnie budujemy w lewej kolumnie
        _activeColumn = leftColumn;

        // === NAGŁÓWEK ===
        var header = new VisualElement();
        header.style.marginBottom = 12;

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
        _activeColumn.Add(header);

        AddSeparator();

        // === PRODUKCJA ===
        var pb = building as ProductionBuilding;
        if (pb != null && pb.Recipe != null)
        {
            AddSectionLabel("PRODUKCJA");
            AddInfoRow("Produkt", pb.Recipe.outputProductId);
            AddInfoRow("Tempo", $"{pb.Recipe.outputAmount} j / {pb.Recipe.productionTimeTicks} tick");
            
            var storageRow = new VisualElement();
            storageRow.style.flexDirection = FlexDirection.Row;
            storageRow.style.justifyContent = Justify.SpaceBetween;
            storageRow.style.marginBottom = 4;
            var storageLbl = new Label("Magazyn");
            storageLbl.style.color = new Color(0.55f, 0.65f, 0.75f);
            storageLbl.style.fontSize = 12;
            _panelStorageLabel = new Label($"{pb.GetStorageAmount(pb.Recipe.outputProductId):F0} / {pb.StorageCapacity:F0}");
            _panelStorageLabel.style.color = new Color(0.9f, 0.95f, 1f);
            _panelStorageLabel.style.fontSize = 12;
            _panelStorageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            storageRow.Add(storageLbl);
            storageRow.Add(_panelStorageLabel);
            _activeColumn.Add(storageRow);

            if (pb.Recipe.inputs.Count > 0)
            {
                AddSeparator();
                AddSectionLabel("SUROWCE");
                foreach (var input in pb.Recipe.inputs)
                    AddInfoRow(input.productId, $"{pb.GetAvailableFromPool(input.productId):F0} dostępne");
            }
        }

        AddSeparator();

        // === EKONOMIA ===
        AddSectionLabel("EKONOMIA");
        AddInfoRow("Koszt budowy", $"${building.ConstructionCost:F0}");
        AddInfoRow("Utrzymanie", $"${building.MonthlyMaintenance:F0}/mies.");
        AddInfoRow("Pensje", $"${building.MonthlyWages:F0}/mies.");
        AddInfoRow("Poziom", $"{building.Level}");

        // === PRAWA KOLUMNA ===
        _activeColumn = rightColumn;

        // === ANALYTICS ===
        AddSectionLabel("ANALYTICS");

        _panelMarginLabel = new Label("Marża: N/A");
        _panelMarginLabel.style.color = new Color(0.9f, 0.95f, 1f);
        _panelMarginLabel.style.fontSize = 12;
        _panelMarginLabel.style.marginBottom = 4;
        _activeColumn.Add(_panelMarginLabel);

        _panelPaybackLabel = new Label("Payback: N/A");
        _panelPaybackLabel.style.color = new Color(0.9f, 0.95f, 1f);
        _panelPaybackLabel.style.fontSize = 12;
        _panelPaybackLabel.style.marginBottom = 4;
        _activeColumn.Add(_panelPaybackLabel);

        _panelMonthlyProfitLabel = new Label("Monthly Profit: N/A");
        _panelMonthlyProfitLabel.style.color = new Color(0.4f, 0.95f, 0.5f);
        _panelMonthlyProfitLabel.style.fontSize = 12;
        _panelMonthlyProfitLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _panelMonthlyProfitLabel.style.marginBottom = 4;
        _activeColumn.Add(_panelMonthlyProfitLabel);

        _panelAvgThroughputLabel = new Label("Avg Output: N/A items/tick");
        _panelAvgThroughputLabel.style.color = new Color(0.9f, 0.95f, 1f);
        _panelAvgThroughputLabel.style.fontSize = 12;
        _panelAvgThroughputLabel.style.marginBottom = 12;
        _activeColumn.Add(_panelAvgThroughputLabel);

        // === PRICE SLIDER ===
        var priceSliderLabel = new Label("Cena sprzedaży (slider)");
        priceSliderLabel.style.color = new Color(0.55f, 0.65f, 0.75f);
        priceSliderLabel.style.fontSize = 11;
        priceSliderLabel.style.marginBottom = 4;
        _activeColumn.Add(priceSliderLabel);

        _panelPriceSlider = new Slider(25f, 125f, SliderDirection.Horizontal);
        _panelPriceSlider.style.marginBottom = 8;
        _activeColumn.Add(_panelPriceSlider);

        _panelPriceValueLabel = new UnityEngine.UIElements.TextField();
        _panelPriceValueLabel.value = "$0.00";
        _panelPriceValueLabel.style.height = 30;
        _panelPriceValueLabel.style.marginBottom = 12;
        _panelPriceValueLabel.style.color = new Color(0.9f, 0.95f, 1f);
        _panelPriceValueLabel.Q<UnityEngine.UIElements.TextElement>().style.color = new Color(0.22f, 0.24f, 0.26f);
        _panelPriceValueLabel.style.fontSize = 10;
        _activeColumn.Add(_panelPriceValueLabel);

        _panelPriceSlider.RegisterValueChangedCallback(evt =>
        {
            if (_currentSelectedBuilding is ProductionBuilding prodBuilding && prodBuilding.Recipe != null)
            {
                var productDb = EconomyManager.Instance?.ProductDatabase;
                var productData = productDb?.GetById(prodBuilding.Recipe.outputProductId);
                float basePrice = productData?.basePrice ?? 1f;
                float actualPrice = (evt.newValue / 100f) * basePrice;
                prodBuilding.SetSellingPrice(actualPrice);
                _panelPriceValueLabel.SetValueWithoutNotify($"${actualPrice:F2}");
            }
        });

        _panelPriceValueLabel.RegisterValueChangedCallback(evt =>
        {
            if (_currentSelectedBuilding is ProductionBuilding prodBuilding && prodBuilding.Recipe != null)
            {
                if (float.TryParse(evt.newValue.Replace("$", ""), out float newPrice))
                {
                    prodBuilding.SetSellingPrice(Mathf.Max(0f, newPrice));
                    var productDb = EconomyManager.Instance?.ProductDatabase;
                    var productData = productDb?.GetById(prodBuilding.Recipe.outputProductId);
                    float basePrice = productData?.basePrice ?? 1f;
                    float pricePercent = basePrice > 0 ? (newPrice / basePrice) * 100f : 100f;
                    pricePercent = Mathf.Clamp(pricePercent, 25f, 125f);
                    _panelPriceSlider.SetValueWithoutNotify(pricePercent);
                }
            }
        });

        // === SPRZEDAŻ (tylko production buildings) ===
        if (pb != null)
        {
            AddSeparator();
            AddSectionLabel("SPRZEDAŻ");

            var autoSellToggle = new Toggle("Auto-sprzedaż");
            autoSellToggle.value = pb.AutoSell;
            autoSellToggle.style.color = new Color(0.9f, 0.95f, 1f);
            autoSellToggle.style.marginBottom = 8;
            autoSellToggle.RegisterValueChangedCallback(evt =>
            {
                pb.SetAutoSell(evt.newValue);
            });
            _activeColumn.Add(autoSellToggle);

            var sellBtn = new Button(() =>
            {
                float revenue = pb.SellAll();
                if (revenue > 0f)
                    Debug.Log($"[UI] Sprzedano za ${revenue:F2}");
                BuildBuildingInfoWindowContent(building);
            });
            sellBtn.text = "Sprzedaj teraz";
            sellBtn.style.backgroundColor = new Color(0.11f, 0.35f, 0.15f);
            sellBtn.style.color = new Color(0.4f, 0.95f, 0.5f);
            sellBtn.style.height = 28;
            sellBtn.style.borderTopWidth = 0;
            sellBtn.style.borderBottomWidth = 0;
            sellBtn.style.borderLeftWidth = 0;
            sellBtn.style.borderRightWidth = 0;
            _activeColumn.Add(sellBtn);
        }

        // === CLOSE BUTTON ===
        var closeBtn = new Button(() => {
            _currentSelectedBuilding = null;
            _buildingInfoWindow.style.display = DisplayStyle.None;
        });
        closeBtn.text = "✕ Zamknij";
        closeBtn.style.backgroundColor = new Color(0.15f, 0.2f, 0.32f);
        closeBtn.style.color = new Color(0.7f, 0.75f, 0.85f);
        closeBtn.style.borderTopWidth = 0;
        closeBtn.style.borderBottomWidth = 0;
        closeBtn.style.borderLeftWidth = 0;
        closeBtn.style.borderRightWidth = 0;
        closeBtn.style.height = 28;
        closeBtn.style.marginTop = 8;
        _infoBody.Add(closeBtn);
    }

    private void RefreshBuildingInfoPanel(Building building)
    {
        if (building == null)
        {
            _buildingInfoWindow.style.display = DisplayStyle.None;
            return;
        }

        _buildingInfoWindow.style.display = DisplayStyle.Flex;

        var pb = building as ProductionBuilding;

        // Podstawowe info
        if (pb != null && pb.Recipe != null && _panelStorageLabel != null)
        {
            _panelStorageLabel.text =
                $"{pb.GetStorageAmount(pb.Recipe.outputProductId):F0} / {pb.StorageCapacity:F0}";
        }

        // Ekonomia
        float monthlyCost = building.GetMonthlyCost();

        if (pb != null && pb.Recipe != null)
        {
            // Avg Throughput
            float avgThroughput = pb.GetAverageThroughput(12);
            _panelAvgThroughputLabel.text = $"Avg Output: {avgThroughput:F2} items/tick";

            // Cost per unit
            float inputCostSum = 0f;
            var productDb = EconomyManager.Instance?.ProductDatabase;
            if (productDb != null && pb.Recipe.inputs.Count > 0)
            {
                foreach (var input in pb.Recipe.inputs)
                {
                    var productData = productDb.GetById(input.productId);
                    if (productData != null)
                        inputCostSum += productData.basePrice * input.amount;
                }
            }
            float maintenancePerUnit = monthlyCost / (30f * 24f);
            float costPerUnit = (inputCostSum + maintenancePerUnit) / Mathf.Max(1f, pb.Recipe.outputAmount);

            // Marża
            float sellingPrice = pb.SellingPrice;
            float marginPercent = sellingPrice > 0 ? ((sellingPrice - costPerUnit) / sellingPrice) * 100f : 0f;
            marginPercent = Mathf.Max(marginPercent, -999f);
            _panelMarginLabel.text = $"Marża: {marginPercent:F1}%";

            // Payback time (w dniach)
            float dailyProfit = (avgThroughput * 24f * sellingPrice) - (monthlyCost / 30f);
            float paybackDays = dailyProfit > 0.01f ? building.ConstructionCost / dailyProfit : float.MaxValue;
            string paybackStr = paybackDays >= 999 ? "∞ dni" : $"{paybackDays:F1} dni";
            _panelPaybackLabel.text = $"Payback: {paybackStr}";

            // Monthly Profit
            float monthlyProduction = avgThroughput * 24f * 30f;
            float monthlyRevenue = monthlyProduction * sellingPrice;
            float monthlyProfit = monthlyRevenue - monthlyCost;
            Color profitColor = monthlyProfit >= 0 ? new Color(0.4f, 0.95f, 0.5f) : new Color(0.95f, 0.3f, 0.3f);
            _panelMonthlyProfitLabel.style.color = profitColor;
            _panelMonthlyProfitLabel.text = $"Monthly Profit: ${monthlyProfit:F0}";

            // Price slider (25-125% of base price)
            var outputProductData = productDb?.GetById(pb.Recipe.outputProductId);
            float basePrice = outputProductData?.basePrice ?? 1f;
            float pricePercent = basePrice > 0 ? (sellingPrice / basePrice) * 100f : 100f;
            pricePercent = Mathf.Clamp(pricePercent, 25f, 125f);
            _panelPriceSlider.SetValueWithoutNotify(pricePercent);
            _panelPriceValueLabel.SetValueWithoutNotify($"${sellingPrice:F2}");
        }
        else
        {
            _panelMarginLabel.text = "Marża: N/A";
            _panelPaybackLabel.text = "Payback: N/A";
            _panelMonthlyProfitLabel.text = "Monthly Profit: N/A";
            _panelAvgThroughputLabel.text = "Avg Output: N/A items/tick";
        }
    }

    private string GetBuildingType(Building building)
    {
        if (building is ProductionBuilding pb && pb.Recipe != null)
            return "Budynek produkcyjny";
        return "Budynek";
    }

    private void AddSeparator()
    {
        var sep = new VisualElement();
        sep.style.height = 1;
        sep.style.backgroundColor = new Color(0.2f, 0.27f, 0.4f);
        sep.style.marginTop = 8;
        sep.style.marginBottom = 8;
        _activeColumn.Add(sep);
    }

    private void AddSectionLabel(string text)
    {
        var label = new Label(text);
        label.style.color = new Color(0.45f, 0.58f, 0.75f);
        label.style.fontSize = 10;
        label.style.marginBottom = 6;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        _activeColumn.Add(label);
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
        _activeColumn.Add(row);
    }

    private void OnTimeUpdated(TimeUpdatedEvent e)
    {
        if (_tickLabel == null) return;
        _tickLabel.text = e.DateString;
    }

    private void OnBuildingDeselected(BuildingDeselectedEvent e)
    {
        _currentSelectedBuilding = null;
        _buildingInfoWindow.style.display = DisplayStyle.None;
    }

    private void OnSpeedButtonClicked(float speed)
    {
        GameManager.Instance?.SetGameSpeed(speed);
    }

    private void OnProductionCompleted(ProductionCompletedEvent e)
    {
        if (_currentSelectedBuilding == null) return;
        RefreshBuildingInfoPanel(_currentSelectedBuilding);
    }

    // --- Publiczne metody ---

    /// <summary>
    /// Wyświetla panel informacyjny dla przekazanego budynku.
    /// </summary>

    public void ShowBuildingInfo(Building building)
    {
        _buildingInfoWindow.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Ukrywa panel informacyjny budynku.
    /// </summary>
    public void HideBuildingInfo()
    {
        _buildingInfoWindow.style.display = DisplayStyle.None;
    }

    /// <summary>
    /// Sprawdza, czy wskaźnik myszy znajduje się nad jakimkolwiek istotnym elementem UI.
    /// Przydatne by np. blokować kliknięcia w obiekty w świecie gry pod interfejsem.
    /// </summary>
    public bool IsPointerOverUI()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (_buildingInfoWindow.style.display == DisplayStyle.Flex)
        {
            var panelRect = _buildingInfoWindow.worldBound;
            if (panelRect.Contains(mousePos))
                return true;
        }
        var buildMenuRect = _buildMenuPanel.worldBound;
        if (buildMenuRect.Contains(mousePos))
            return true;
        var hudRect = _hudPanel.worldBound;
        if (hudRect.Contains(mousePos))
            return true;
        return false;
    }
}