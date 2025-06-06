using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime;

public class GameHUD : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;

    [Header("Turn Panel")]
    public TextMeshProUGUI tourCounterText;
    public TextMeshProUGUI currentPlayerText;
    public Button endTurnButton;

    [Header("Player Stats")]
    public TextMeshProUGUI citiesText;
    public TextMeshProUGUI cultureText;
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI scienceText;

    [Header("Unit Panel")]
    public GameObject unitPanel;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitHealthText;
    public TextMeshProUGUI unitMovementText;
    public Button[] unitActionButtons;

    [Header("Unit Selection")]
    public TextMeshProUGUI unitCycleText;
    public Button cycleUnitsButton;

    [Header("City Management Panel")]
    public GameObject cityManagementPanel;


    [Header("City Management Panel - Main Panel")]
    public GameObject cityManagementPanelMainPanel;
    public TextMeshProUGUI cityName;
    public TextMeshProUGUI population;



    [Header("City Management Panel - Constructed Buildings Panel")]
    public GameObject cityManagementPanelConstructedBuildingsPanel;
    public ScrollRect constructedBuildingsScrollView;



    [Header("City Info Panel")]
    public TextMeshProUGUI food;
    public TextMeshProUGUI production;
    public TextMeshProUGUI culture;
    public TextMeshProUGUI science;

    [Header("Buildings Scroll View")]
    public ScrollRect buildingsScrollView;
    

    [Header("Units Scroll View")]
    public ScrollRect unitsScrollView;

    [Header("Unified prefab for production")]
    public GameObject productionItemPrefab;

    [Header("Current Production")]
    public TextMeshProUGUI turnRemaining;
    public GameObject currentProductionPanel;
    

    [Header("Colors")]
    public Color player1Color = Color.blue;
    public Color player2Color = Color.red;
    public Color player3Color = Color.green;
    public Color player4Color = Color.yellow;

    [Header("Movement Colors")]
    public Color movementNormalColor = Color.green;
    public Color attackRangeColor = Color.red;
    public Color cityWorkRadiusColor = new Color(1f, 0.5f, 0f, 0.3f);

    [Header("Movement Info")]
    public TextMeshProUGUI hexInfoText;
    public GameObject hexInfoPanel;

    // Hot-seat state
    private ICivilization currentPlayerCiv;
    private IUnit selectedUnit;
    private ICity selectedCity;
    private List<IHex> currentValidMoves = new List<IHex>();
    private List<IUnit> unitsOnSelectedHex = new List<IUnit>();
    private int selectedUnitIndex = 0;

    // Movement state
    private bool isInMovementMode = false;
    private Hex lastSelectedHex = null;

    void Start()
    {
        SetupEventListeners();
        HideAllPanels();
        UpdateHUD();
    }

    void SetupEventListeners()
    {
        if (endTurnButton) endTurnButton.onClick.AddListener(OnEndTurnClicked);
        if (cycleUnitsButton) cycleUnitsButton.onClick.AddListener(OnCycleUnitsClicked);

        // Setup unit action buttons
        for (int i = 0; i < unitActionButtons.Length; i++)
        {
            int index = i;
            if (unitActionButtons[i])
                unitActionButtons[i].onClick.AddListener(() => OnUnitActionClicked(index));
        }

        // Subscribe to game events
        if (gameManager)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
            gameManager.OnTurnChanged += OnTurnChanged;
            gameManager.OnCivTurnStarted += OnCivTurnStarted; // New event
            gameManager.OnCivTurnEnded += OnCivTurnEnded;     // New event
        }

        // Subscribe to map manager hex selection
        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.OnHexSelected.AddListener(OnHexSelected);
            mapManager.OnHexDeselected.AddListener(OnHexDeselected);
        }

        SubscribeToTechEvents();
    }

    

    void Update()
    {
        UpdateHUD();
        HandleInput();
        UpdateHexInfo();
        //HandleCityPanelClickOutside();
    }

    void UpdateHUD()
    {
        if (gameManager == null) return;

        UpdateTurnPanel();
        UpdatePlayerStats();
  
    }

    void UpdateTurnPanel()
    {
        // Hot-seat: Show current civilization's turn
        currentPlayerCiv = gameManager.CurrentCivilization;

        if (currentPlayerText && currentPlayerCiv != null)
        {
            string turnInfo = $"{currentPlayerCiv.CivName}'s Turn";
            if (gameManager.GetTotalAliveCivs() > 1)
            {
                turnInfo += $" ({gameManager.GetCivTurnOrder()}/{gameManager.GetTotalAliveCivs()})";
            }

            currentPlayerText.text = turnInfo;
            currentPlayerText.color = GetCivColor(gameManager.GetCivTurnOrder() - 1);
        }

        if (tourCounterText)
            tourCounterText.text = $"Turn: {gameManager.CurrentTurn}";

        // End turn button - check with CivTurnManager
        if (endTurnButton)
        {
            bool gameRunning = gameManager.CurrentGameState == GameState.Running;
            bool hasCiv = currentPlayerCiv != null;
            bool canEndTurn = currentPlayerCiv?.CivManager?.CivTurnManager?.CanEndTurn() ?? true;

            endTurnButton.interactable = gameRunning && hasCiv && canEndTurn;

            // Update button text based on what's needed
            var buttonText = endTurnButton.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (buttonText)
            {
                if (!canEndTurn)
                {
                    var turnManager = currentPlayerCiv?.CivManager?.CivTurnManager;
                    var unitsNeeded = turnManager?.GetUnitsNeedingOrders()?.Count ?? 0;
                    var citiesNeeded = turnManager?.GetCitiesNeedingProduction()?.Count ?? 0;

                    if (unitsNeeded > 0)
                        buttonText.text = $"Units Need Orders ({unitsNeeded})";
                    else if (citiesNeeded > 0)
                        buttonText.text = $"Cities Need Production ({citiesNeeded})";
                    else
                        buttonText.text = "Orders Needed";
                }
                else
                {
                    buttonText.text = "End Turn";
                }
            }
        }
    }

    Color GetCivColor(int civIndex)
    {
        switch (civIndex % 4)
        {
            case 0: return player1Color;
            case 1: return player2Color;
            case 2: return player3Color;
            case 3: return player4Color;
            default: return Color.white;
        }
    }

    void UpdatePlayerStats()
    {
        if (currentPlayerCiv?.CivManager == null) return;

        var civManager = currentPlayerCiv.CivManager;
        if (citiesText) citiesText.text = $"Cities: {civManager.GetCityCount()}";
        if (goldText) goldText.text = $"Gold: {currentPlayerCiv.Gold}";
        if (scienceText) scienceText.text = $"Science: {currentPlayerCiv.Science}";
        if (cultureText) cultureText.text = $"Culture: {currentPlayerCiv.Culture}";
    }

    // Hot-seat: Only get units for current player
    List<IUnit> GetAllUnitsAtHex(Hex hex)
    {
        List<IUnit> allUnits = new List<IUnit>();

        if (currentPlayerCiv?.CivManager?.UnitsManager == null) return allUnits;

        // Only get current player's units in hot-seat mode
        var playerUnits = currentPlayerCiv.CivManager.UnitsManager.GetUnitsAt(hex);
        if (playerUnits != null)
            allUnits.AddRange(playerUnits);

        // Sort: combat units first, then civilians
        return allUnits.OrderBy(u => u.IsCombatUnit() ? 0 : 1).ToList();
    }

    // Hot-seat: Only interact with current player's units and cities
    void HandleNormalHexSelection(Hex selectedHex)
    {
        lastSelectedHex = selectedHex;

        // Get current player's units on this hex only
        unitsOnSelectedHex = GetAllUnitsAtHex(selectedHex);
        selectedUnitIndex = 0;

        IUnit unitOnHex = GetCurrentSelectedUnit();
        ICity cityOnHex = GetCurrentPlayerCityAtHex(selectedHex);

        if (unitOnHex != null)
        {
            SelectUnit(unitOnHex);
            if (cityOnHex != null)
            {
                ShowCityManagement(cityOnHex);
            }
            else
            {
                HideCityManagement();
            }
        }
        else if (cityOnHex != null)
        {
            SelectCity(cityOnHex);
            HideUnitPanel();
        }
        else
        {
            DeselectAll();
        }
    }

    // Hot-seat: Only get current player's cities
    ICity GetCurrentPlayerCityAtHex(Hex hex)
    {
        if (currentPlayerCiv?.CivManager?.CitiesManager == null) return null;

        var cities = currentPlayerCiv.CivManager.CitiesManager.GetAllCities();
        foreach (var city in cities)
        {
            if (city.CenterHex != null && city.CenterHex.Q == hex.Q && city.CenterHex.R == hex.R)
                return city;
        }
        return null;
    }

    // Event handlers for hot-seat system
    void OnCivTurnStarted(ICivilization civ)
    {
        Debug.Log($"UI: {civ.CivName}'s turn started");

        // Clear any previous selections when new civ starts
        DeselectAll();

        // Update current player reference
        currentPlayerCiv = civ;

        // Force UI update
        UpdateHUD();
    }

    void OnCivTurnEnded(ICivilization civ)
    {
        Debug.Log($"UI: {civ.CivName}'s turn ended");

        // Clear selections when turn ends
        DeselectAll();
    }

    void OnEndTurnClicked()
    {
        if (gameManager && gameManager.CurrentGameState == GameState.Running)
        {
            var turnManager = currentPlayerCiv?.CivManager?.CivTurnManager;

            if (turnManager != null && !turnManager.CanEndTurn())
            {
                // Navigate to next item needing orders instead of ending turn
                turnManager.GoToNextItemNeedingOrders();
                return;
            }

            CancelMovementMode();
            ClearAllHighlights();
            gameManager.NextTurn();
        }
    }

    void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"UI: Game state changed to: {newState}");
        UpdateHUD();
    }

    void OnTurnChanged(int newTurn)
    {
        Debug.Log($"UI: Turn changed to: {newTurn}");
        CancelMovementMode();
        ClearAllHighlights();
        UpdateHUD();
    }

    // Rest of the methods remain the same...
    void UpdateHexInfo()
    {
        if (hexInfoText == null || !isInMovementMode || selectedUnit == null)
        {
            if (hexInfoPanel) hexInfoPanel.SetActive(false);
            return;
        }

        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            Hex hoveredHex = mapManager.GetHexUnderMouse();
            if (hoveredHex != null)
            {
                if (hexInfoPanel) hexInfoPanel.SetActive(true);

                string info = $"{hoveredHex.Terrain} - {hoveredHex.GetMovementCostInfo()}";

                bool canReach = currentValidMoves.Any(hex => hex.Q == hoveredHex.Q && hex.R == hoveredHex.R);
                if (canReach)
                {
                    var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
                    if (unitsManager != null)
                    {
                        int cost = unitsManager.GetMovementCostTo(selectedUnit, hoveredHex);
                        info += $" (Cost: {cost})";
                    }
                }
                else
                {
                    info += " (Out of range)";
                }

                hexInfoText.text = info;
            }
            else
            {
                if (hexInfoPanel) hexInfoPanel.SetActive(false);
            }
        }
    }

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMovementMode();
            DeselectAll();
        }

        if (Input.GetKeyDown(KeyCode.Tab) && unitsOnSelectedHex.Count > 1)
        {
            CycleToNextUnit();
        }

        if (Input.GetMouseButtonDown(1) && selectedUnit != null)
        {
            HandleRightClickMovement();
        }
    }

    void HandleRightClickMovement()
    {
        var mapManager = gameManager?.MapManager;
        if (mapManager == null) return;

        Hex clickedHex = mapManager.GetHexUnderMouse();
        if (clickedHex != null)
        {
            if (!isInMovementMode)
            {
                EnterMovementMode();
            }

            if (isInMovementMode)
            {
                HandleMovementClick(clickedHex);
            }
        }
    }

    void HandleMovementClick(Hex targetHex)
    {
        if (selectedUnit == null || !isInMovementMode) return;

        if (currentValidMoves.Any(hex => hex.Q == targetHex.Q && hex.R == targetHex.R))
        {
            var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
            if (unitsManager != null)
            {
                int movementCost = unitsManager.GetMovementCostTo(selectedUnit, targetHex);
                Debug.Log($"Attempting to move {selectedUnit.UnitName} to ({targetHex.Q}, {targetHex.R}) " +
                          $"(Cost: {movementCost}, Available: {selectedUnit.Movement})");

                bool moveSuccess = unitsManager.MoveUnit(selectedUnit, targetHex);
                if (moveSuccess)
                {
                    Debug.Log($"Move successful! {selectedUnit.UnitName} now has {selectedUnit.Movement} movement left");

                    isInMovementMode = false;
                    ClearAllHighlights();
                    ShowUnitInfo(selectedUnit);
                    ShowMovementRange(selectedUnit);

                    if (unitNameText)
                        unitNameText.text = $"{selectedUnit.UnitType}: {selectedUnit.UnitName}";
                }
                else
                {
                    Debug.LogWarning($"Failed to move {selectedUnit.UnitName} to ({targetHex.Q}, {targetHex.R})");
                }
            }
        }
        else
        {
            Debug.Log($"Invalid move target: ({targetHex.Q}, {targetHex.R}) is not in range");
        }
    }

    public void OnHexSelected(Hex selectedHex)
    {
        if (selectedHex == null) return;
        Debug.Log($"Hex selected at ({selectedHex.Q}, {selectedHex.R})");
        HandleNormalHexSelection(selectedHex);
    }

    public void OnHexDeselected(Hex deselectedHex)
    {
        Debug.Log("Hex deselected");
    }

    void SelectUnit(IUnit unit)
    {
        CancelMovementMode();
        selectedUnit = unit;
        ShowUnitInfo(unit);
        ShowMovementRange(unit);
        Debug.Log($"Selected unit: {unit.UnitName}");
    }

    void ShowMovementRange(IUnit unit)
    {
        if (unit == null) return;

        var mapManager = gameManager?.MapManager;
        var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;

        if (mapManager != null && unitsManager != null)
        {
            mapManager.ShowUnitMovementOptions(unit, unitsManager, movementNormalColor);
            currentValidMoves = unitsManager.GetValidMoves(unit);
            Debug.Log($"Showing movement range: {currentValidMoves.Count} valid moves for {unit.UnitName}");
        }
    }

    void SelectCity(ICity city)
    {
        CancelMovementMode();
        ClearAllHighlights();
        selectedCity = city;
        selectedUnit = null;
        ShowCityManagement(city);

        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.ShowCityWorkRadius(city, cityWorkRadiusColor);
        }
    }

    void DeselectAll()
    {
        CancelMovementMode();
        ClearAllHighlights();
        HideAllPanels();
        selectedUnit = null;
        selectedCity = null;
        unitsOnSelectedHex.Clear();
        currentValidMoves.Clear();
    }

    void ToggleMovementMode()
    {
        if (selectedUnit == null) return;

        if (isInMovementMode)
        {
            CancelMovementMode();
        }
        else
        {
            EnterMovementMode();
        }
    }

    void EnterMovementMode()
    {
        if (selectedUnit == null || selectedUnit.Movement <= 0 || selectedUnit.HasMoved)
        {
            Debug.Log("Unit cannot move");
            return;
        }

        isInMovementMode = true;

        var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
        if (unitsManager != null)
        {
            currentValidMoves = unitsManager.GetValidMoves(selectedUnit);
        }

        var mapManager = gameManager?.MapManager;
        if (mapManager != null && unitsManager != null)
        {
            mapManager.ShowUnitInMovementMode(selectedUnit, unitsManager, movementNormalColor);
        }

        Debug.Log($"Entered movement mode for {selectedUnit.UnitName}");

        if (unitNameText)
            unitNameText.text = $"[MOVE] {selectedUnit.UnitType}: {selectedUnit.UnitName}";
    }

    void CancelMovementMode()
    {
        if (!isInMovementMode) return;

        isInMovementMode = false;
        ClearAllHighlights();

        if (hexInfoPanel) hexInfoPanel.SetActive(false);

        if (selectedUnit != null && unitNameText)
            unitNameText.text = $"{selectedUnit.UnitType}: {selectedUnit.UnitName}";

        ShowMovementRange(selectedUnit);
    }

    void ClearAllHighlights()
    {
        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.ClearHighlights();
        }
        currentValidMoves.Clear();
    }

    void OnCycleUnitsClicked()
    {
        CycleToNextUnit();
    }

    void CycleToNextUnit()
    {
        if (unitsOnSelectedHex.Count <= 1) return;

        selectedUnitIndex = (selectedUnitIndex + 1) % unitsOnSelectedHex.Count;
        var nextUnit = GetCurrentSelectedUnit();

        if (nextUnit != null)
        {
            SelectUnit(nextUnit);
            Debug.Log($"Cycled to unit: {nextUnit.UnitName} ({selectedUnitIndex + 1}/{unitsOnSelectedHex.Count})");
        }
    }

    IUnit GetCurrentSelectedUnit()
    {
        if (unitsOnSelectedHex.Count == 0 || selectedUnitIndex >= unitsOnSelectedHex.Count)
            return null;
        return unitsOnSelectedHex[selectedUnitIndex];
    }

    public void ShowUnitInfo(IUnit unit)
    {
        selectedUnit = unit;
        if (unitPanel) unitPanel.SetActive(true);

        if (unitNameText)
        {
            string moveModePrefix = isInMovementMode ? "[MOVE] " : "";
            string movementStatus = "";

            if (unit.HasMoved)
                movementStatus = " (Moved)";
            else if (unit.Movement <= 0)
                movementStatus = " (No MP)";

            unitNameText.text = $"{moveModePrefix}{unit.UnitType}: {unit.UnitName}{movementStatus}";
        }

        if (unitHealthText)
            unitHealthText.text = $"HP: {unit.Health}/{unit.MaxHealth}";

        if (unitMovementText)
            unitMovementText.text = $"Movement: {unit.Movement}/{unit.MaxMovement}";

        if (unitCycleText && unitsOnSelectedHex.Count > 1)
        {
            unitCycleText.text = $"{selectedUnitIndex + 1}/{unitsOnSelectedHex.Count}";
            unitCycleText.gameObject.SetActive(true);
            if (cycleUnitsButton) cycleUnitsButton.gameObject.SetActive(true);
        }
        else
        {
            if (unitCycleText) unitCycleText.gameObject.SetActive(false);
            if (cycleUnitsButton) cycleUnitsButton.gameObject.SetActive(false);
        }

        UpdateUnitActionButtons(unit);
    }

    void UpdateUnitActionButtons(IUnit unit)
    {
        if (unitActionButtons == null) return;

        var turnManager = currentPlayerCiv?.CivManager?.CivTurnManager;
        bool isSkipped = turnManager?.IsUnitSkipped(unit) ?? false;

        // Button 0: Move/Cancel Move (always allow movement)
        if (unitActionButtons.Length > 0 && unitActionButtons[0])
        {
            var button = unitActionButtons[0];
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (isInMovementMode)
            {
                button.interactable = true;
                if (buttonText) buttonText.text = "Cancel";
            }
            else
            {
                button.interactable = unit.Movement > 0 && !unit.HasMoved;
                if (buttonText) buttonText.text = "Move";
            }
        }

        // Button 1: Attack OR Special Action (blocked by guard/sleep)
        if (unitActionButtons.Length > 1 && unitActionButtons[1])
        {
            var button = unitActionButtons[1];
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            if (unit.IsCombatUnit())
            {
                button.interactable = unit.Attack > 0 && !unit.HasMoved && !isSkipped;
                if (buttonText) buttonText.text = "Attack";
            }
            else
            {
                var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
                switch (unit.UnitType)
                {
                    case UnitType.Settler:
                        button.interactable = !unit.HasMoved && !isSkipped && unitsManager != null && unitsManager.CanSettleCity(unit);
                        if (buttonText) buttonText.text = "Settle City";
                        break;

                    case UnitType.Worker:
                        button.interactable = !unit.HasMoved && !isSkipped && unitsManager != null && unitsManager.CanBuildImprovement(unit);
                        if (buttonText) buttonText.text = "Build Improvement";
                        break;

                    default:
                        button.interactable = false;
                        if (buttonText) buttonText.text = "No Action";
                        break;
                }
            }
        }

        // Button 2: Guard/Sleep
        if (unitActionButtons.Length > 2 && unitActionButtons[2])
        {
            var button = unitActionButtons[2];
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();

            button.interactable = !unit.HasMoved;

            if (isSkipped)
            {
                if (unit.IsCombatUnit())
                {
                    if (buttonText) buttonText.text = "Guarding";
                }
                else
                {
                    if (buttonText) buttonText.text = "Sleeping";
                }
            }
            else
            {
                if (unit.IsCombatUnit())
                {
                    if (buttonText) buttonText.text = "Guard";
                }
                else
                {
                    if (buttonText) buttonText.text = "Sleep";
                }
            }
        }
    }

    void OnUnitActionClicked(int actionIndex)
    {
        if (selectedUnit == null) return;

        switch (actionIndex)
        {
            case 0: // Move/Cancel Move
                ToggleMovementMode();
                break;

            case 1: // Attack OR Special Action
                if (selectedUnit.IsCombatUnit())
                {
                    Debug.Log($"Attack with {selectedUnit.UnitName}");
                    // TODO: Implement attack targeting
                }
                else
                {
                    HandleCivilianAction(selectedUnit);
                }
                break;

            case 2: // Guard/Sleep/Wake Up
                HandleUnitStateAction(selectedUnit);
                break;

            default:
                Debug.Log($"Unit action {actionIndex} for {selectedUnit.UnitName}");
                break;
        }
    }

    void HandleUnitStateAction(IUnit unit)
    {
        var turnManager = currentPlayerCiv?.CivManager?.CivTurnManager;
        if (turnManager == null) return;

        if (turnManager.IsUnitSkipped(unit))
        {
            // Unit is already guarded/sleeping - do nothing (let movement wake it up)
            Debug.Log($"{unit.UnitName} is already in guard/sleep mode");
        }
        else
        {
            // Guard or Sleep unit
            if (unit.IsCombatUnit())
            {
                turnManager.SetUnitGuard(unit);
            }
            else
            {
                turnManager.SetUnitSleep(unit);
            }
        }

        // Update UI
        ShowUnitInfo(unit);
    }

    void HandleCivilianAction(IUnit unit)
    {
        var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
        if (unitsManager == null)
        {
            Debug.LogError("No UnitsManager available for civilian action");
            return;
        }

        switch (unit.UnitType)
        {
            case UnitType.Settler:
                AttemptSettleCity(unit, unitsManager);
                break;

            case UnitType.Worker:
                AttemptBuildImprovement(unit, unitsManager);
                break;

            default:
                Debug.Log($"No special action available for {unit.UnitType}");
                break;
        }
    }

    void AttemptSettleCity(IUnit settler, IUnitsManager unitsManager)
    {
        bool success = unitsManager.SettleCity(settler);

        if (success)
        {
            // Settler was consumed, clear selection and auto-select new city
            DeselectAll();

            // Find and select the newly created city
            var settlementHex = (Hex)settler.CurrentHex;
            var mapManager = gameManager?.MapManager;
            if (mapManager != null)
            {
                mapManager.SelectHex(settlementHex);
            }
        }
        else
        {
            // Show failure reason to player
            string reason = unitsManager.GetSettleFailureReason(settler);
            Debug.LogWarning($"Settlement failed: {reason}");
            // TODO: Show UI popup with failure reason
        }
    }

    void AttemptBuildImprovement(IUnit worker, IUnitsManager unitsManager)
    {
        bool success = unitsManager.BuildImprovement(worker);

        if (success)
        {
            Debug.Log($"Improvement built successfully by {worker.UnitName}");
            // Update UI to reflect worker's spent movement
            ShowUnitInfo(worker);
        }
        else
        {
            Debug.LogWarning($"Cannot build improvement with {worker.UnitName}");
            // TODO: Show UI feedback about why improvement can't be built
        }
    }

    public void ShowCityManagement(ICity city)
    {
        selectedCity = city;
        if (cityManagementPanel) cityManagementPanel.SetActive(true);

        if (cityName) cityName.text = city.CityName;
        if (population) population.text = $"Population: {city.Population}";

        var yields = city.GetTotalYields();
        if (food) food.text = $"Food: {yields.food}";
        if (production) production.text = $"Production: {yields.production}";
        if (culture) culture.text = $"Culture: {yields.culture}";
        if (science) science.text = $"Science: {yields.science}";



        // Clear existing items (can be from another civ)
        for (int i = currentProductionPanel.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(currentProductionPanel.transform.GetChild(i).gameObject);
            Debug.Log($"Destroyed child at index {i} in currentProductionPanel");
        }

        
        

        UpdateBuildingsList(city);
        UpdateUnitsList(city);
        UpdateConstructedBuildingsList(city);
        UpdateCurrentProduction(city);
    }

    void UpdateBuildingsList(ICity city)
    {
        if (buildingsScrollView == null || productionItemPrefab == null) return;

        // Clear existing items
        foreach (Transform child in buildingsScrollView.content)
            Destroy(child.gameObject);

        // Show available buildings for production
        var availableBuildings = city.GetAvailableBuildingsForProduction();
        Debug.Log($"Available buildings count: {availableBuildings.Count}");
        foreach (var building in availableBuildings)
        {
            CreateProductionItem(building, false, true, buildingsScrollView.content, productionItemPrefab); // not constructed, clickable
        }
    }

    void UpdateConstructedBuildingsList(ICity city)
    {
        if (constructedBuildingsScrollView == null) return;

        foreach (Transform child in constructedBuildingsScrollView.content)
            Destroy(child.gameObject);

        var constructedBuildings = city.ConstructedBuildings;
        Debug.Log($"Constructed buildings count: {constructedBuildings.Count}");
        foreach (string buildingId in constructedBuildings)
        {
            var building = BuildingDatabase.GetBuilding(buildingId);
            if (building != null)
            {
                CreateProductionItem(building, true, false, constructedBuildingsScrollView.content, productionItemPrefab);
            }
        }
    }

    void UpdateUnitsList(ICity city)
    {
        if (unitsScrollView == null || productionItemPrefab == null) return;

        // Clear existing items
        foreach (Transform child in unitsScrollView.content)
            Destroy(child.gameObject);

        // Show available units for production
        var availableUnits = city.GetAvailableUnitsForProduction();
        foreach (var unit in availableUnits)
        {
            CreateProductionItem(unit, false, true, unitsScrollView.content, productionItemPrefab); // not constructed, clickable
        }
    }

    void UpdateCurrentProduction(ICity city)
    {
        var currentProduction = city.GetCurrentProduction();
        if (currentProduction != null)
        {
            CreateProductionItem(currentProduction, true, false, currentProductionPanel.transform, productionItemPrefab);

            if (turnRemaining)
            {
                int turnsLeft = city.GetTurnsRemaining();
                turnRemaining.text = $"{turnsLeft} turns";
            }
        }
    }

    void CreateProductionItem(IProductionItem item, bool isConstructed, bool isClickable, Transform parent, GameObject prefab)
    {
        GameObject itemObj = Instantiate(prefab, parent);

        // Set icon
        var iconImage = itemObj.transform.Find("Image")?.GetComponent<UnityEngine.UI.Image>();
        if (iconImage && item.Icon)
            iconImage.sprite = item.Icon;

        // Set text
        var textComponent = itemObj.GetComponentInChildren<TextMeshProUGUI>();
        if (textComponent)
        {
            string displayText = item.DisplayName;
            
            textComponent.text = displayText;
        }

        var button = itemObj.GetComponent<UnityEngine.UI.Button>();
        if (button == null)
            button = itemObj.AddComponent<UnityEngine.UI.Button>();

        if (isClickable && !isConstructed)
        {
            button.onClick.AddListener(() => OnProductionItemClicked(item));
        }
        else
        {
            button.interactable = false; // Disable button if not clickable
        }
    }

    void OnProductionItemClicked(IProductionItem item)
    {
        if (selectedCity != null)
        {
            selectedCity.ChangeProduction(item);
            ShowCityManagement(selectedCity); // Refresh UI

            //// Update turn state since city now has production
            //var turnManager = currentPlayerCiv?.CivManager?.CivTurnManager;
            //turnManager?.CheckTurnState();
        }
    }

    

   


   


    void SubscribeToTechEvents()
    {
        // Subscribe to current civ's tech manager
        if (currentPlayerCiv?.CivManager?.TechManager != null)
        {
            currentPlayerCiv.CivManager.TechManager.OnTechnologyResearched += OnTechnologyResearched;
        }
    }

    void OnTechnologyResearched(ITechnology technology)
    {
        Debug.Log($"Technology {technology.TechName} researched - refreshing production lists");

        // Refresh city management UI if a city is selected
        if (selectedCity != null)
        {
            ShowCityManagement(selectedCity);
        }
    }


    





    public void HideUnitPanel()
    {
        selectedUnit = null;
        if (unitPanel) unitPanel.SetActive(false);
    }

    public void HideCityManagement()
    {
        selectedCity = null;
        if (cityManagementPanel) cityManagementPanel.SetActive(false);
    }

  

    void HideAllPanels()
    {
        HideUnitPanel();
        HideCityManagement();
    }

    void OnDestroy()
    {
        if (gameManager)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
            gameManager.OnTurnChanged -= OnTurnChanged;
            gameManager.OnCivTurnStarted -= OnCivTurnStarted;
            gameManager.OnCivTurnEnded -= OnCivTurnEnded;
        }

        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.OnHexSelected.RemoveListener(OnHexSelected);
            mapManager.OnHexDeselected.RemoveListener(OnHexDeselected);
        }
    }
}