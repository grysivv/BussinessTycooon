using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Centralny zarządca UI. Jedyny punkt otwierania i zamykania paneli.
/// Buduje całe UI programowo przez kod C# — bez plików UXML/USS.
/// Kolory i style pochodzą z UITheme.
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
    private Label _windowTitleLabel;
    private Label _windowSubtitleLabel;

    // Dashboard
    private VisualElement _dashboardView;
    private bool _isDashboardOpen = false;
    private readonly Dictionary<string, LineChart> _dashboardCharts = new();
    private readonly Dictionary<string, Label> _dashboardPriceLabels = new();
    private readonly Dictionary<string, VisualElement> _dashboardCards = new();

    // Elementy HUD
    private Label _moneyLabel;
    private Label _incomeLabel;
    private Label _dateLabel;
    private readonly Dictionary<float, Button> _speedButtons = new();
    private float _activeSpeed = 1f;

    // Menu budowania — do wyszarzania budynków, na które nie stać
    private readonly List<(VisualElement card, Label costLabel, BuildingData data)> _buildCards = new();

    // Aktualnie wybrany budynek (do panelu info)
    private Building _currentSelectedBuilding;
    private float _incomeThisTick = 0f;

    private Label _panelStorageLabel;
    private Label _panelMarginLabel;
    private Label _panelPaybackLabel;
    private Label _panelMonthlyProfitLabel;
    private Label _panelAvgThroughputLabel;
    private Slider _panelPriceSlider;
    private TextField _panelPriceField;

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

        BuildRoot();
        BuildHUD();
        BuildBuildMenu();
        BuildBuildingInfoWindow();
        BuildDashboard();

        // Stan początkowy (jeśli managerowie już żyją)
        var economy = EconomyManager.Instance;
        if (economy != null)
        {
            UpdateMoneyDisplay(economy.PlayerMoney);
            RefreshBuildMenuAffordability(economy.PlayerMoney);
        }
        RefreshSpeedButtons();
    }

    void OnEnable()
    {
        EventBus.Subscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Subscribe<TickEvent>(OnTick);
        EventBus.Subscribe<BuildingSelectedEvent>(OnBuildingSelected);
        EventBus.Subscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        EventBus.Subscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Subscribe<TimeUpdatedEvent>(OnTimeUpdated);
        EventBus.Subscribe<SaleCompletedEvent>(OnSaleCompleted);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<PlayerMoneyChangedEvent>(OnMoneyChanged);
        EventBus.Unsubscribe<TickEvent>(OnTick);
        EventBus.Unsubscribe<BuildingSelectedEvent>(OnBuildingSelected);
        EventBus.Unsubscribe<BuildingDeselectedEvent>(OnBuildingDeselected);
        EventBus.Unsubscribe<ProductionCompletedEvent>(OnProductionCompleted);
        EventBus.Unsubscribe<TimeUpdatedEvent>(OnTimeUpdated);
        EventBus.Unsubscribe<SaleCompletedEvent>(OnSaleCompleted);
    }

    // --- Root ---

    private void BuildRoot()
    {
        _root.style.width = Length.Percent(100);
        _root.style.height = Length.Percent(100);
        _root.style.flexDirection = FlexDirection.Column;
        _root.style.justifyContent = Justify.SpaceBetween;

        // Root nie może łapać kliknięć — inaczej IsPointerOverUI()
        // uznawałby cały ekran za UI.
        _root.pickingMode = PickingMode.Ignore;
    }

    // --- HUD (górny pasek) ---

    private void BuildHUD()
    {
        _hudPanel = new VisualElement();
        _hudPanel.style.flexDirection = FlexDirection.Row;
        _hudPanel.style.justifyContent = Justify.SpaceBetween;
        _hudPanel.style.alignItems = Align.Center;
        _hudPanel.style.backgroundColor = UITheme.PanelBg;
        _hudPanel.style.borderBottomWidth = 1;
        _hudPanel.style.borderBottomColor = UITheme.Border;
        _hudPanel.style.SetPadding(20, 0);
        _hudPanel.style.height = 52;

        // Lewa strona: pieniądze + dochód (pill)
        var leftGroup = new VisualElement();
        leftGroup.style.flexDirection = FlexDirection.Row;
        leftGroup.style.alignItems = Align.Center;

        _moneyLabel = new Label(UITheme.FormatMoney(50000));
        _moneyLabel.style.color = UITheme.TextPrimary;
        _moneyLabel.style.fontSize = 19;
        _moneyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        _moneyLabel.style.marginRight = 12;

        _incomeLabel = new Label("+$0/h");
        _incomeLabel.style.color = UITheme.TextSecondary;
        _incomeLabel.style.backgroundColor = UITheme.NeutralSoft;
        _incomeLabel.style.fontSize = 11;
        _incomeLabel.style.SetRadius(10);
        _incomeLabel.style.SetPadding(8, 3);

        leftGroup.Add(_moneyLabel);
        leftGroup.Add(_incomeLabel);

        // Prawa strona: data + segmentowana kontrolka prędkości + Ekonomia
        var rightGroup = new VisualElement();
        rightGroup.style.flexDirection = FlexDirection.Row;
        rightGroup.style.alignItems = Align.Center;

        _dateLabel = new Label("—");
        _dateLabel.style.color = UITheme.TextSecondary;
        _dateLabel.style.fontSize = 12;
        _dateLabel.style.marginRight = 16;

        var speedSegment = new VisualElement();
        speedSegment.style.flexDirection = FlexDirection.Row;
        speedSegment.style.alignItems = Align.Center;
        speedSegment.style.backgroundColor = UITheme.InsetBg;
        speedSegment.style.SetBorder(UITheme.Border);
        speedSegment.style.SetRadius(6);
        speedSegment.style.SetPadding(2, 2);
        speedSegment.style.height = 28;

        foreach (var (label, speed) in new (string, float)[]
            { ("II", 0f), ("×1", 1f), ("×3", 3f) })
        {
            var btn = CreateSegmentButton(label, () => SetGameSpeed(speed));
            _speedButtons[speed] = btn;

            // Hover nie może nadpisywać podświetlenia aktywnej prędkości
            float capturedSpeed = speed;
            btn.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (!Mathf.Approximately(capturedSpeed, _activeSpeed))
                    btn.style.backgroundColor = UITheme.NeutralSoft;
            });
            btn.RegisterCallback<MouseLeaveEvent>(_ => RefreshSpeedButtons());

            speedSegment.Add(btn);
        }

        var economyBtn = CreateToolbarButton("Ekonomia", ToggleDashboard);
        economyBtn.style.marginLeft = 12;
        AddHoverEffect(economyBtn, Color.clear, UITheme.CardBgHover);

        rightGroup.Add(_dateLabel);
        rightGroup.Add(speedSegment);
        rightGroup.Add(economyBtn);

        _hudPanel.Add(leftGroup);
        _hudPanel.Add(rightGroup);
        _root.Add(_hudPanel);
    }

    /// <summary>Przycisk-duch: przezroczysty, cienka linia, do pasków narzędzi.</summary>
    private Button CreateToolbarButton(string label, System.Action onClick)
    {
        var btn = new Button(onClick) { text = label };
        btn.style.backgroundColor = Color.clear;
        btn.style.color = UITheme.TextSecondary;
        btn.style.SetBorder(UITheme.Border);
        btn.style.SetRadius(6);
        btn.style.SetMargin(0, 0);
        btn.style.paddingLeft = 12;
        btn.style.paddingRight = 12;
        btn.style.height = 28;
        return btn;
    }

    /// <summary>Płaski przycisk wewnątrz segmentowanej kontrolki.</summary>
    private Button CreateSegmentButton(string label, System.Action onClick)
    {
        var btn = new Button(onClick) { text = label };
        btn.style.backgroundColor = Color.clear;
        btn.style.color = UITheme.TextSecondary;
        btn.style.SetBorder(Color.clear, 0);
        btn.style.SetRadius(4);
        btn.style.SetMargin(1, 0);
        btn.style.SetPadding(0, 0);
        btn.style.width = 32;
        btn.style.height = 22;
        btn.style.fontSize = 11;
        btn.style.unityTextAlign = TextAnchor.MiddleCenter;
        return btn;
    }

    private static void AddHoverEffect(VisualElement element, Color normal, Color hover)
    {
        element.RegisterCallback<MouseEnterEvent>(_ => element.style.backgroundColor = hover);
        element.RegisterCallback<MouseLeaveEvent>(_ => element.style.backgroundColor = normal);
    }

    private void SetGameSpeed(float speed)
    {
        GameManager.Instance?.SetGameSpeed(speed);
        _activeSpeed = speed;
        RefreshSpeedButtons();
    }

    private void RefreshSpeedButtons()
    {
        foreach (var kv in _speedButtons)
        {
            bool active = Mathf.Approximately(kv.Key, _activeSpeed);
            kv.Value.style.backgroundColor = active ? UITheme.CardBgActive : Color.clear;
            kv.Value.style.color = active ? UITheme.TextPrimary : UITheme.TextSecondary;
        }
    }

    private void UpdateMoneyDisplay(float amount)
    {
        if (_moneyLabel == null) return;
        _moneyLabel.text = UITheme.FormatMoney(amount);
        _moneyLabel.style.color = amount < 5000 ? UITheme.Negative : UITheme.TextPrimary;
    }

    // --- Dashboard ---

    private void ToggleDashboard()
    {
        _isDashboardOpen = !_isDashboardOpen;

        if (_isDashboardOpen)
        {
            _hudPanel.style.display = DisplayStyle.None;
            _buildMenuPanel.style.display = DisplayStyle.None;
            _buildingInfoWindow.style.display = DisplayStyle.None;
            _dashboardView.style.display = DisplayStyle.Flex;
            UpdateDashboardCharts();
        }
        else
        {
            _dashboardView.style.display = DisplayStyle.None;
            _hudPanel.style.display = DisplayStyle.Flex;
            _buildMenuPanel.style.display = DisplayStyle.Flex;
            if (_currentSelectedBuilding != null)
                _buildingInfoWindow.style.display = DisplayStyle.Flex;
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
        _dashboardView.style.backgroundColor = UITheme.OverlayBg;

        // Górny pasek: tytuł + zamknięcie
        var topBar = new VisualElement();
        topBar.style.height = 48;
        topBar.style.backgroundColor = UITheme.PanelBg;
        topBar.style.borderBottomWidth = 1;
        topBar.style.borderBottomColor = UITheme.Border;
        topBar.style.flexDirection = FlexDirection.Row;
        topBar.style.alignItems = Align.Center;
        topBar.style.SetPadding(16, 0);

        var titleLabel = new Label("PANEL EKONOMII");
        titleLabel.style.fontSize = 13;
        titleLabel.style.letterSpacing = 2f;
        titleLabel.style.color = UITheme.TextPrimary;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.flexGrow = 1;
        topBar.Add(titleLabel);

        var closeBtn = CreateToolbarButton("✕", ToggleDashboard);
        closeBtn.style.width = 34;
        AddHoverEffect(closeBtn, Color.clear, UITheme.CardBgHover);
        topBar.Add(closeBtn);

        _dashboardView.Add(topBar);

        // Zawartość: dwie kolumny
        var content = new VisualElement();
        content.style.flexGrow = 1;
        content.style.flexDirection = FlexDirection.Row;
        content.style.SetPadding(16);

        // Lewa kolumna: filtry produktów
        var leftCol = new VisualElement();
        leftCol.style.width = 240;
        leftCol.style.marginRight = 16;
        leftCol.style.backgroundColor = UITheme.CardBg;
        leftCol.style.SetBorder(UITheme.Border);
        leftCol.style.SetRadius(6);
        leftCol.style.SetPadding(12);
        leftCol.style.alignSelf = Align.FlexStart;

        AddSectionLabel(leftCol, "PRODUKTY");

        var productDb = EconomyManager.Instance?.ProductDatabase;
        if (productDb != null)
        {
            foreach (var product in productDb.AllProducts)
            {
                string productId = product.id;
                var toggle = new Toggle(product.name) { value = true };
                toggle.style.marginBottom = 4;
                StyleToggle(toggle);
                toggle.RegisterValueChangedCallback(evt =>
                {
                    if (_dashboardCards.TryGetValue(productId, out var card))
                        card.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                });
                leftCol.Add(toggle);
            }
        }

        // Prawa kolumna: wykresy cen
        var rightCol = new VisualElement();
        rightCol.style.flexBasis = 0;
        rightCol.style.flexGrow = 1;
        rightCol.style.flexDirection = FlexDirection.Column;

        AddSectionLabel(rightCol, "CENY PRODUKTÓW (OSTATNIE GODZINY)");

        var chartsScroll = new ScrollView(ScrollViewMode.Vertical);
        chartsScroll.style.flexGrow = 1;

        if (productDb != null)
        {
            foreach (var product in productDb.AllProducts)
            {
                var card = new VisualElement();
                card.style.backgroundColor = UITheme.CardBg;
                card.style.SetBorder(UITheme.Border);
                card.style.SetRadius(6);
                card.style.SetPadding(10);
                card.style.marginBottom = 10;

                var headerRow = new VisualElement();
                headerRow.style.flexDirection = FlexDirection.Row;
                headerRow.style.justifyContent = Justify.SpaceBetween;
                headerRow.style.marginBottom = 6;

                var nameLabel = new Label(product.name);
                nameLabel.style.color = UITheme.TextPrimary;
                nameLabel.style.fontSize = 12;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                var priceLabel = new Label("—");
                priceLabel.style.color = UITheme.TextSecondary;
                priceLabel.style.fontSize = 12;

                headerRow.Add(nameLabel);
                headerRow.Add(priceLabel);

                var chart = new LineChart();

                card.Add(headerRow);
                card.Add(chart);
                chartsScroll.Add(card);

                _dashboardCharts[product.id] = chart;
                _dashboardPriceLabels[product.id] = priceLabel;
                _dashboardCards[product.id] = card;
            }
        }

        rightCol.Add(chartsScroll);

        content.Add(leftCol);
        content.Add(rightCol);
        _dashboardView.Add(content);

        _root.Add(_dashboardView);
    }

    private void UpdateDashboardCharts()
    {
        var stats = StatisticsManager.Instance;
        var productDb = EconomyManager.Instance?.ProductDatabase;
        if (stats == null || productDb == null) return;

        foreach (var product in productDb.AllProducts)
        {
            if (!_dashboardCharts.TryGetValue(product.id, out var chart))
                continue;

            var history = stats.GetHourlyPriceHistory(product.id);
            chart.SetData(history);

            if (_dashboardPriceLabels.TryGetValue(product.id, out var priceLabel))
                priceLabel.text = history.Count > 0 ? $"${history[history.Count - 1]:F2}" : "—";
        }
    }

    // --- Build Menu (dolny pasek) ---

    private void BuildBuildMenu()
    {
        _buildMenuPanel = new VisualElement();
        _buildMenuPanel.style.flexDirection = FlexDirection.Column;
        _buildMenuPanel.style.backgroundColor = UITheme.PanelBg;
        _buildMenuPanel.style.borderTopWidth = 1;
        _buildMenuPanel.style.borderTopColor = UITheme.Border;
        _buildMenuPanel.style.SetPadding(12, 8);

        AddSectionLabel(_buildMenuPanel, "BUDOWA");

        var cardsRow = new ScrollView(ScrollViewMode.Horizontal);
        cardsRow.style.flexDirection = FlexDirection.Row;
        _buildMenuPanel.Add(cardsRow);

        if (_buildingDatabase == null)
        {
            Debug.LogWarning("[UIManager] Brak BuildingDatabase!");
            _root.Add(_buildMenuPanel);
            return;
        }

        foreach (var buildingData in _buildingDatabase.AllBuildings)
        {
            var btn = CreateBuildingButton(buildingData);
            cardsRow.Add(btn);
        }

        _root.Add(_buildMenuPanel);
    }

    private VisualElement CreateBuildingButton(BuildingData data)
    {
        var card = new VisualElement();
        card.style.flexDirection = FlexDirection.Column;
        card.style.alignItems = Align.Center;
        card.style.justifyContent = Justify.Center;
        card.style.backgroundColor = UITheme.CardBg;
        card.style.SetBorder(UITheme.Border);
        card.style.SetRadius(8);
        card.style.SetPadding(10, 6);
        card.style.marginRight = 8;
        card.style.width = 112;
        card.style.height = 56;

        // Jedna linia z wielokropkiem — karty zawsze mają identyczne wymiary
        var nameLabel = new Label(data.displayName);
        nameLabel.style.color = UITheme.TextPrimary;
        nameLabel.style.fontSize = 12;
        nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
        nameLabel.style.overflow = Overflow.Hidden;
        nameLabel.style.textOverflow = TextOverflow.Ellipsis;
        nameLabel.style.maxWidth = Length.Percent(100);

        var costLabel = new Label(UITheme.FormatMoney(data.constructionCost));
        costLabel.style.color = UITheme.TextSecondary;
        costLabel.style.fontSize = 11;
        costLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        costLabel.style.marginTop = 2;

        card.Add(nameLabel);
        card.Add(costLabel);

        // Kliknięcie uruchamia tryb stawiania
        card.RegisterCallback<ClickEvent>(evt =>
        {
            _buildingPlacer?.StartPlacing(data);
        });

        AddHoverEffect(card, UITheme.CardBg, UITheme.CardBgHover);

        _buildCards.Add((card, costLabel, data));
        return card;
    }

    private void RefreshBuildMenuAffordability(float money)
    {
        foreach (var (card, costLabel, data) in _buildCards)
        {
            bool affordable = money >= data.constructionCost;
            card.style.opacity = affordable ? 1f : 0.4f;
            costLabel.style.color = affordable ? UITheme.TextSecondary : UITheme.Negative;
        }
    }

    // --- Building Info Window (okno wyśrodkowane) ---

    private void BuildBuildingInfoWindow()
    {
        _buildingInfoWindow = new VisualElement();
        _buildingInfoWindow.style.position = Position.Absolute;
        _buildingInfoWindow.style.left = new Length(50, LengthUnit.Percent);
        _buildingInfoWindow.style.top = new Length(50, LengthUnit.Percent);
        _buildingInfoWindow.style.translate = new Translate(
            new Length(-50, LengthUnit.Percent), new Length(-50, LengthUnit.Percent));
        _buildingInfoWindow.style.width = 840;
        _buildingInfoWindow.style.maxHeight = 600;
        _buildingInfoWindow.style.backgroundColor = UITheme.PanelBg;
        _buildingInfoWindow.style.SetBorder(UITheme.BorderStrong);
        _buildingInfoWindow.style.SetRadius(8);
        _buildingInfoWindow.style.display = DisplayStyle.None;

        // Nagłówek okna: typ + nazwa po lewej, ✕ po prawej (wyrównany na środku)
        var headerBar = new VisualElement();
        headerBar.style.flexDirection = FlexDirection.Row;
        headerBar.style.alignItems = Align.Center;
        headerBar.style.justifyContent = Justify.SpaceBetween;
        headerBar.style.SetPadding(14, 10);
        headerBar.style.borderBottomWidth = 1;
        headerBar.style.borderBottomColor = UITheme.Border;
        headerBar.style.height = 50;

        var headerText = new VisualElement();
        headerText.style.flexGrow = 1;
        headerText.style.flexDirection = FlexDirection.Column;
        headerText.style.justifyContent = Justify.Center;

        _windowSubtitleLabel = new Label("");
        _windowSubtitleLabel.style.color = UITheme.TextHeader;
        _windowSubtitleLabel.style.fontSize = 10;
        _windowSubtitleLabel.style.letterSpacing = 1.5f;
        _windowSubtitleLabel.style.marginBottom = 2;

        _windowTitleLabel = new Label("");
        _windowTitleLabel.style.color = UITheme.TextPrimary;
        _windowTitleLabel.style.fontSize = 16;
        _windowTitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

        headerText.Add(_windowSubtitleLabel);
        headerText.Add(_windowTitleLabel);

        var closeBtn = new Button(CloseBuildingInfoWindow) { text = "✕" };
        closeBtn.style.width = 28;
        closeBtn.style.height = 28;
        closeBtn.style.backgroundColor = Color.clear;
        closeBtn.style.color = UITheme.TextSecondary;
        closeBtn.style.fontSize = 16;
        closeBtn.style.SetBorder(Color.clear, 0);
        closeBtn.style.SetRadius(4);
        closeBtn.style.SetPadding(0, 0);
        closeBtn.style.marginLeft = 12;
        closeBtn.style.alignSelf = Align.Center;
        AddHoverEffect(closeBtn, Color.clear, UITheme.CardBgHover);

        headerBar.Add(headerText);
        headerBar.Add(closeBtn);
        _buildingInfoWindow.Add(headerBar);

        // Treść w ScrollView
        _infoBody = new ScrollView(ScrollViewMode.Vertical);
        _infoBody.style.flexGrow = 1;
        _infoBody.style.SetPadding(14, 12);
        _buildingInfoWindow.Add(_infoBody);

        _root.Add(_buildingInfoWindow);
    }

    private void CloseBuildingInfoWindow()
    {
        _currentSelectedBuilding = null;
        _buildingInfoWindow.style.display = DisplayStyle.None;
    }

    // --- Aktualizacja paneli ---

    private void OnMoneyChanged(PlayerMoneyChangedEvent e)
    {
        UpdateMoneyDisplay(e.NewAmount);
        RefreshBuildMenuAffordability(e.NewAmount);
    }

    private void OnSaleCompleted(SaleCompletedEvent e)
    {
        _incomeThisTick += e.Revenue;
    }

    private void OnTick(TickEvent e)
    {
        // Dochód z minionego ticka (1 tick = 1 godzina gry)
        if (_incomeLabel != null)
        {
            bool positive = _incomeThisTick > 0f;
            _incomeLabel.text = $"+{UITheme.FormatMoney(_incomeThisTick)}/h";
            _incomeLabel.style.color = positive ? UITheme.Positive : UITheme.TextSecondary;
            _incomeLabel.style.backgroundColor = positive ? UITheme.PositiveSoft : UITheme.NeutralSoft;
        }
        _incomeThisTick = 0f;

        if (_currentSelectedBuilding != null)
            RefreshBuildingInfoPanel(_currentSelectedBuilding);

        if (_isDashboardOpen)
            UpdateDashboardCharts();
    }

    private void OnBuildingSelected(BuildingSelectedEvent e)
    {
        if (_isDashboardOpen) return;

        _currentSelectedBuilding = e.Building;
        _buildingInfoWindow.style.display = DisplayStyle.Flex;
        BuildBuildingInfoWindowContent(e.Building);
    }

    private void BuildBuildingInfoWindowContent(Building building)
    {
        _infoBody.Clear();

        if (building == null) return;

        _windowTitleLabel.text = building.DisplayName;
        _windowSubtitleLabel.text = $"{GetBuildingType(building)}  ·  ({building.GridX}, {building.GridY})";

        // === UKŁAD DWUKOLUMNOWY ===
        var columns = new VisualElement();
        columns.style.flexDirection = FlexDirection.Row;
        columns.style.width = Length.Percent(100);
        columns.style.minHeight = 300;
        _infoBody.Add(columns);

        var leftColumn = new VisualElement();
        leftColumn.style.flexGrow = 1;
        leftColumn.style.flexBasis = 0;
        leftColumn.style.minWidth = 0;
        leftColumn.style.marginRight = 12;

        var rightColumn = new VisualElement();
        rightColumn.style.flexGrow = 1;
        rightColumn.style.flexBasis = 0;
        rightColumn.style.minWidth = 0;
        rightColumn.style.marginLeft = 12;

        columns.Add(leftColumn);
        columns.Add(rightColumn);

        // === LEWA KOLUMNA: PRODUKCJA + EKONOMIA ===
        var pb = building as ProductionBuilding;
        if (pb != null && pb.Recipe != null)
        {
            AddSectionLabel(leftColumn, "PRODUKCJA");
            AddInfoRow(leftColumn, "Produkt", pb.Recipe.outputProductId);
            AddInfoRow(leftColumn, "Tempo", $"{pb.Recipe.outputAmount} j / {pb.Recipe.productionTimeTicks} tick");
            _panelStorageLabel = AddInfoRow(leftColumn, "Magazyn",
                $"{pb.GetStorageAmount(pb.Recipe.outputProductId):F0} / {pb.StorageCapacity:F0}");

            if (pb.Recipe.inputs.Count > 0)
            {
                AddSeparator(leftColumn);
                AddSectionLabel(leftColumn, "SUROWCE");
                foreach (var input in pb.Recipe.inputs)
                    AddInfoRow(leftColumn, input.productId, $"{pb.GetAvailableFromPool(input.productId):F0} dostępne");
            }

            AddSeparator(leftColumn);
        }

        AddSectionLabel(leftColumn, "EKONOMIA");
        AddInfoRow(leftColumn, "Koszt budowy", UITheme.FormatMoney(building.ConstructionCost));
        AddInfoRow(leftColumn, "Utrzymanie", $"{UITheme.FormatMoney(building.MonthlyMaintenance)}/mies.");
        AddInfoRow(leftColumn, "Pensje", $"{UITheme.FormatMoney(building.MonthlyWages)}/mies.");
        AddInfoRow(leftColumn, "Poziom", $"{building.Level}");

        // === PRAWA KOLUMNA: ANALITYKA + CENA + SPRZEDAŻ ===
        AddSectionLabel(rightColumn, "ANALITYKA");
        _panelMarginLabel = AddInfoRow(rightColumn, "Marża", "N/A");
        _panelPaybackLabel = AddInfoRow(rightColumn, "Zwrot inwestycji", "N/A");
        _panelMonthlyProfitLabel = AddInfoRow(rightColumn, "Zysk miesięczny", "N/A");
        _panelMonthlyProfitLabel.style.color = UITheme.Positive;
        _panelAvgThroughputLabel = AddInfoRow(rightColumn, "Śr. produkcja", "N/A");

        AddSeparator(rightColumn);

        // === CENA SPRZEDAŻY ===
        AddSectionLabel(rightColumn, "CENA SPRZEDAŻY");

        var sliderCaption = new Label("Procent ceny bazowej (25–125%)");
        sliderCaption.style.color = UITheme.TextSecondary;
        sliderCaption.style.fontSize = 11;
        sliderCaption.style.marginBottom = 4;
        rightColumn.Add(sliderCaption);

        _panelPriceSlider = new Slider(25f, 125f, SliderDirection.Horizontal);
        _panelPriceSlider.style.marginBottom = 8;
        StyleSlider(_panelPriceSlider);
        rightColumn.Add(_panelPriceSlider);

        _panelPriceField = new TextField();
        _panelPriceField.value = "$0.00";
        _panelPriceField.style.height = 28;
        _panelPriceField.style.marginBottom = 12;
        StyleTextFieldInput(_panelPriceField);
        rightColumn.Add(_panelPriceField);

        _panelPriceSlider.RegisterValueChangedCallback(evt =>
        {
            if (_currentSelectedBuilding is ProductionBuilding prodBuilding && prodBuilding.Recipe != null)
            {
                var productDb = EconomyManager.Instance?.ProductDatabase;
                var productData = productDb?.GetById(prodBuilding.Recipe.outputProductId);
                float basePrice = productData?.basePrice ?? 1f;
                float actualPrice = (evt.newValue / 100f) * basePrice;
                prodBuilding.SetSellingPrice(actualPrice);
                _panelPriceField.SetValueWithoutNotify($"${actualPrice:F2}");
            }
        });

        _panelPriceField.RegisterValueChangedCallback(evt =>
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
            AddSeparator(rightColumn);
            AddSectionLabel(rightColumn, "SPRZEDAŻ");

            var autoSellToggle = new Toggle("Auto-sprzedaż");
            autoSellToggle.value = pb.AutoSell;
            autoSellToggle.style.width = Length.Percent(100);
            autoSellToggle.style.height = 24;
            autoSellToggle.style.marginBottom = 12;
            StyleToggle(autoSellToggle);
            autoSellToggle.RegisterValueChangedCallback(evt =>
            {
                pb.SetAutoSell(evt.newValue);
            });
            rightColumn.Add(autoSellToggle);

            var sellBtn = new Button(() =>
            {
                pb.SellAll();
                BuildBuildingInfoWindowContent(building);
                RefreshBuildingInfoPanel(building);
            });
            sellBtn.text = "Sprzedaj teraz";
            sellBtn.style.backgroundColor = UITheme.PositiveSoft;
            sellBtn.style.color = UITheme.Positive;
            sellBtn.style.SetBorder(new Color(0.204f, 0.827f, 0.600f, 0.3f));
            sellBtn.style.SetRadius(6);
            sellBtn.style.height = 30;
            AddHoverEffect(sellBtn, UITheme.PositiveSoft, new Color(0.204f, 0.827f, 0.600f, 0.22f));
            rightColumn.Add(sellBtn);
        }

        // Wypełnij wartości analityki od razu, bez czekania na kolejny tick
        RefreshBuildingInfoPanel(building);
    }

    private static void StyleTextFieldInput(TextField field)
    {
        field.style.color = UITheme.TextPrimary;
        var input = field.Q<VisualElement>("unity-text-input");
        if (input == null) return;
        input.style.backgroundColor = UITheme.InsetBg;
        input.style.color = UITheme.TextPrimary;
        input.style.SetBorder(UITheme.Border);
        input.style.SetRadius(6);
    }

    /// <summary>Minimalne przemalowanie domyślnego suwaka Unity pod motyw dashboardu.</summary>
    private static void StyleSlider(Slider slider)
    {
        slider.style.height = 20;
        slider.style.marginBottom = 12;
        slider.style.paddingTop = 8;
        slider.style.paddingBottom = 8;

        var input = slider.Q<VisualElement>("unity-input");
        if (input != null)
        {
            input.style.height = 20;
        }

        var tracker = slider.Q<VisualElement>("unity-tracker");
        if (tracker != null)
        {
            tracker.style.backgroundColor = UITheme.CardBgActive;
            tracker.style.height = 4;
            tracker.style.borderTopWidth = 0;
            tracker.style.borderBottomWidth = 0;
            tracker.style.borderLeftWidth = 0;
            tracker.style.borderRightWidth = 0;
            tracker.style.SetRadius(2);
            tracker.style.marginTop = 0;
            tracker.style.marginBottom = 0;
        }

        var dragger = slider.Q<VisualElement>("unity-dragger");
        if (dragger != null)
        {
            dragger.style.backgroundColor = UITheme.Accent;
            dragger.style.width = 12;
            dragger.style.height = 12;
            dragger.style.SetBorder(Color.clear, 0);
            dragger.style.SetRadius(6);
            dragger.style.marginTop = 0;
            dragger.style.marginBottom = 0;
            dragger.style.position = Position.Relative;
            dragger.style.top = -4;
        }
    }

    /// <summary>Minimalne przemalowanie domyślnego checkboxa Unity pod motyw dashboardu.</summary>
    private static void StyleToggle(Toggle toggle)
    {
        toggle.style.color = UITheme.TextPrimary;
        toggle.style.fontSize = 12;
        toggle.style.paddingLeft = 0;
        toggle.style.paddingRight = 0;

        var input = toggle.Q<VisualElement>("unity-input");
        if (input != null)
        {
            input.style.width = 16;
            input.style.height = 16;
            input.style.borderTopWidth = 1;
            input.style.borderBottomWidth = 1;
            input.style.borderLeftWidth = 1;
            input.style.borderRightWidth = 1;
            input.style.borderTopColor = UITheme.Border;
            input.style.borderBottomColor = UITheme.Border;
            input.style.borderLeftColor = UITheme.Border;
            input.style.borderRightColor = UITheme.Border;
            input.style.SetRadius(3);
            input.style.marginRight = 8;
        }

        var checkmark = toggle.Q<VisualElement>("unity-checkmark");
        if (checkmark != null)
        {
            checkmark.style.unityBackgroundImageTintColor = UITheme.Accent;
        }

        var label = toggle.Q<Label>("unity-text");
        if (label != null)
        {
            label.style.color = UITheme.TextPrimary;
            label.style.fontSize = 12;
        }
    }

    private void RefreshBuildingInfoPanel(Building building)
    {
        if (building == null)
        {
            _buildingInfoWindow.style.display = DisplayStyle.None;
            return;
        }

        if (!_isDashboardOpen)
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
            // Średnia produkcja
            float avgThroughput = pb.GetAverageThroughput(12);
            _panelAvgThroughputLabel.text = $"{avgThroughput:F2} j/tick";

            // Koszt jednostkowy
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
            _panelMarginLabel.text = $"{marginPercent:F1}%";
            _panelMarginLabel.style.color = marginPercent >= 0 ? UITheme.TextPrimary : UITheme.Negative;

            // Czas zwrotu inwestycji (w dniach)
            float dailyProfit = (avgThroughput * 24f * sellingPrice) - (monthlyCost / 30f);
            float paybackDays = dailyProfit > 0.01f ? building.ConstructionCost / dailyProfit : float.MaxValue;
            _panelPaybackLabel.text = paybackDays >= 999 ? "∞ dni" : $"{paybackDays:F1} dni";

            // Zysk miesięczny
            float monthlyProduction = avgThroughput * 24f * 30f;
            float monthlyRevenue = monthlyProduction * sellingPrice;
            float monthlyProfit = monthlyRevenue - monthlyCost;
            _panelMonthlyProfitLabel.style.color = monthlyProfit >= 0 ? UITheme.Positive : UITheme.Negative;
            _panelMonthlyProfitLabel.text = UITheme.FormatMoney(monthlyProfit);

            // Suwak ceny (25–125% ceny bazowej)
            var outputProductData = productDb?.GetById(pb.Recipe.outputProductId);
            float basePrice = outputProductData?.basePrice ?? 1f;
            float pricePercent = basePrice > 0 ? (sellingPrice / basePrice) * 100f : 100f;
            pricePercent = Mathf.Clamp(pricePercent, 25f, 125f);
            _panelPriceSlider.SetValueWithoutNotify(pricePercent);
            _panelPriceField.SetValueWithoutNotify($"${sellingPrice:F2}");
        }
        else
        {
            _panelMarginLabel.text = "N/A";
            _panelPaybackLabel.text = "N/A";
            _panelMonthlyProfitLabel.text = "N/A";
            _panelAvgThroughputLabel.text = "N/A";
        }
    }

    private string GetBuildingType(Building building)
    {
        if (building is ProductionBuilding pb && pb.Recipe != null)
            return "Budynek produkcyjny";
        return "Budynek";
    }

    // --- Pomocnicze klocki UI ---

    private void AddSeparator(VisualElement parent)
    {
        var sep = new VisualElement();
        sep.style.height = 1;
        sep.style.backgroundColor = UITheme.Border;
        sep.style.SetMargin(0, 8);
        parent.Add(sep);
    }

    private void AddSectionLabel(VisualElement parent, string text)
    {
        var label = new Label(text);
        label.style.color = UITheme.TextHeader;
        label.style.fontSize = 10;
        label.style.letterSpacing = 1.5f;
        label.style.marginBottom = 6;
        label.style.unityFontStyleAndWeight = FontStyle.Bold;
        parent.Add(label);
    }

    /// <summary>Dodaje wiersz "etykieta ... wartość" i zwraca Label wartości (do późniejszej aktualizacji).</summary>
    private Label AddInfoRow(VisualElement parent, string labelText, string valueText)
    {
        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.marginBottom = 4;

        var lbl = new Label(labelText);
        lbl.style.color = UITheme.TextSecondary;
        lbl.style.fontSize = 12;

        var val = new Label(valueText);
        val.style.color = UITheme.TextPrimary;
        val.style.fontSize = 12;
        val.style.unityFontStyleAndWeight = FontStyle.Bold;

        row.Add(lbl);
        row.Add(val);
        parent.Add(row);
        return val;
    }

    private void OnTimeUpdated(TimeUpdatedEvent e)
    {
        if (_dateLabel == null) return;
        _dateLabel.text = e.DateString;
    }

    private void OnBuildingDeselected(BuildingDeselectedEvent e)
    {
        CloseBuildingInfoWindow();
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
        _currentSelectedBuilding = building;
        _buildingInfoWindow.style.display = DisplayStyle.Flex;
        BuildBuildingInfoWindowContent(building);
    }

    /// <summary>
    /// Ukrywa panel informacyjny budynku.
    /// </summary>
    public void HideBuildingInfo()
    {
        CloseBuildingInfoWindow();
    }

    /// <summary>
    /// Sprawdza, czy wskaźnik myszy znajduje się nad jakimkolwiek elementem UI.
    /// Używa panel.Pick() zamiast ręcznych prostokątów — działa dla wszystkich paneli
    /// i poprawnie obsługuje konwersję współrzędnych ekranu (oś Y ekranu rośnie w górę,
    /// a w UI Toolkit w dół).
    /// </summary>
    public bool IsPointerOverUI()
    {
        if (_root == null || _root.panel == null || Mouse.current == null)
            return false;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
            _root.panel, new Vector2(screenPos.x, Screen.height - screenPos.y));

        return _root.panel.Pick(panelPos) != null;
    }
}
