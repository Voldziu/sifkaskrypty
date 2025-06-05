using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

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
    public TextMeshProUGUI unitCycleText; // Shows "1/2" when multiple units on hex
    public Button cycleUnitsButton;

    [Header("City Management Panel")]
    public GameObject cityManagementPanel;
    public TextMeshProUGUI cityName;
    public TextMeshProUGUI population;

    [Header("City Info Panel")]
    public TextMeshProUGUI food;
    public TextMeshProUGUI production;
    public TextMeshProUGUI culture;
    public TextMeshProUGUI science;

    [Header("Buildings Scroll View")]
    public ScrollRect buildingsScrollView;
    public GameObject buildingItemPrefab;

    [Header("Units Scroll View")]
    public ScrollRect unitsScrollView;
    public GameObject unitItemPrefab;

    [Header("Current Production")]
    public TextMeshProUGUI turnRemaining;
    public TextMeshProUGUI currentProductionName;

    [Header("Colors")]
    public Color player1Color = Color.blue;
    public Color player2Color = Color.red;

    [Header("Movement Colors")]
    public Color movementNormalColor = Color.green;
    public Color attackRangeColor = Color.red;
    public Color cityWorkRadiusColor = new Color(1f, 0.5f, 0f, 0.3f); // Orange with transparency

    [Header("Movement Info")]
    public TextMeshProUGUI hexInfoText; // Shows terrain and movement cost info
    public GameObject hexInfoPanel; // Panel to show hex information

    // Selection state
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
        }

        // Subscribe to map manager hex selection
        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.OnHexSelected.AddListener(OnHexSelected);
            mapManager.OnHexDeselected.AddListener(OnHexDeselected);
        }
    }

    void Update()
    {
        UpdateHUD();
        HandleInput();
        UpdateHexInfo(); // Show hex information when hovering
    }

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

                // Show terrain and movement cost info
                string info = $"{hoveredHex.Terrain} - {hoveredHex.GetMovementCostInfo()}";

                // Check if hex is reachable
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
        // ESC to deselect and cancel movement mode
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMovementMode();
            DeselectAll();
        }

        // Tab to cycle through units on current hex
        if (Input.GetKeyDown(KeyCode.Tab) && unitsOnSelectedHex.Count > 1)
        {
            CycleToNextUnit();
        }

        // Right-click for movement when any unit is selected
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
            // If not in movement mode, automatically enter it first
            if (!isInMovementMode)
            {
                EnterMovementMode();
            }

            // Then handle the movement
            if (isInMovementMode)
            {
                HandleMovementClick(clickedHex);
            }
        }
    }

    public void OnHexSelected(Hex selectedHex)
    {
        if (selectedHex == null) return;

        Debug.Log($"Hex selected at ({selectedHex.Q}, {selectedHex.R})");

        // Normal selection logic - always handle selection first
        HandleNormalHexSelection(selectedHex);
    }

    void HandleMovementClick(Hex targetHex)
    {
        if (selectedUnit == null || !isInMovementMode) return;

        // Check if the target hex is in valid moves
        if (currentValidMoves.Any(hex => hex.Q == targetHex.Q && hex.R == targetHex.R))
        {
            // Show movement cost before moving
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

                    // Always exit movement mode first and clear highlights
                    isInMovementMode = false;
                    ClearAllHighlights();

                    // Update the unit info display to show new movement points
                    ShowUnitInfo(selectedUnit);

                    // Show updated movement range using MapManager
                    ShowMovementRange(selectedUnit);

                    // Restore normal unit display (remove [MOVE] prefix)
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

            // TODO: Future feature - Queue movement with pathfinding
            // if (IsValidQueuedMove(targetHex))
            // {
            //     QueueMovement(selectedUnit, targetHex);
            // }
        }
    }

    void HandleNormalHexSelection(Hex selectedHex)
    {
        lastSelectedHex = selectedHex;

        // Get all units on this hex
        unitsOnSelectedHex = GetAllUnitsAtHex(selectedHex);
        selectedUnitIndex = 0;

        // Check what's on this hex
        IUnit unitOnHex = GetCurrentSelectedUnit();
        ICity cityOnHex = GetCityAtHex(selectedHex);

        if (unitOnHex != null)
        {
            SelectUnit(unitOnHex);

            // Show city management if there's also a city
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
            // Empty hex selected - clear everything
            DeselectAll();
        }
    }

    void SelectUnit(IUnit unit)
    {
        // Cancel any previous movement mode
        CancelMovementMode();

        selectedUnit = unit;
        ShowUnitInfo(unit);

        // Automatically show movement range when unit is selected using MapManager
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
            // Use MapManager's specialized method for showing unit movement options
            mapManager.ShowUnitMovementOptions(unit, unitsManager, movementNormalColor);

            // Update current valid moves for local tracking
            currentValidMoves = unitsManager.GetValidMoves(unit);

            Debug.Log($"Showing movement range via MapManager: {currentValidMoves.Count} valid moves for {unit.UnitName} (MP: {unit.Movement})");
        }
    }

    void SelectCity(ICity city)
    {
        CancelMovementMode();
        ClearAllHighlights(); // Clear any unit movement highlights
        selectedCity = city;
        selectedUnit = null;
        ShowCityManagement(city);

        // Show city work radius using MapManager
        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.ShowCityWorkRadius(city, cityWorkRadiusColor);
        }
    }

    void DeselectAll()
    {
        CancelMovementMode();
        ClearAllHighlights(); // Clear any remaining highlights
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

        // Get valid moves if we don't have them
        var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;
        if (unitsManager != null)
        {
            currentValidMoves = unitsManager.GetValidMoves(selectedUnit);
        }

        // Use MapManager's specialized method for movement mode highlighting
        var mapManager = gameManager?.MapManager;
        if (mapManager != null && unitsManager != null)
        {
            mapManager.ShowUnitInMovementMode(selectedUnit, unitsManager, movementNormalColor);
        }

        Debug.Log($"Entered movement mode for {selectedUnit.UnitName} - Right-click to move");

        // Update UI to show movement mode
        if (unitNameText)
            unitNameText.text = $"[MOVE] {selectedUnit.UnitType}: {selectedUnit.UnitName}";
    }

    void CancelMovementMode()
    {
        if (!isInMovementMode) return;

        isInMovementMode = false;

        // Clear all highlights when cancelling movement
        ClearAllHighlights();

        // Hide hex info panel
        if (hexInfoPanel) hexInfoPanel.SetActive(false);

        // Restore normal unit display
        if (selectedUnit != null && unitNameText)
            unitNameText.text = $"{selectedUnit.UnitType}: {selectedUnit.UnitName}";

        // If unit is still selected and can move, show movement range again
        ShowMovementRange(selectedUnit);
    }

    void ClearAllHighlights()
    {
        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.ClearHighlights();
            Debug.Log("All highlights cleared via MapManager");
        }
        currentValidMoves.Clear();
    }

    void ShowAttackRange(IUnit unit)
    {
        if (unit == null || !unit.IsCombatUnit()) return;

        var mapManager = gameManager?.MapManager;
        var unitsManager = currentPlayerCiv?.CivManager?.UnitsManager;

        if (mapManager != null && unitsManager != null)
        {
            // Use MapManager's specialized method for attack range
            mapManager.HighlightAttackRange(unit, attackRangeColor, unitsManager);
            Debug.Log($"Showing attack range for {unit.UnitName}");
        }
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

    List<IUnit> GetAllUnitsAtHex(Hex hex)
    {
        List<IUnit> allUnits = new List<IUnit>();

        if (currentPlayerCiv?.CivManager?.UnitsManager == null) return allUnits;

        // Get player's units
        var playerUnits = currentPlayerCiv.CivManager.UnitsManager.GetUnitsAt(hex);
        if (playerUnits != null)
            allUnits.AddRange(playerUnits);

        // Get other civilizations' units (for visibility)
        var allCivs = gameManager.CivsManager.GetAliveCivilizations();
        foreach (var civ in allCivs)
        {
            if (civ.CivId != currentPlayerCiv.CivId && civ.CivManager?.UnitsManager != null)
            {
                var enemyUnits = civ.CivManager.UnitsManager.GetUnitsAt(hex);
                if (enemyUnits != null)
                    allUnits.AddRange(enemyUnits);
            }
        }

        // Sort units: player units first, then by category (combat, then civilian)
        return allUnits.OrderBy(u =>
        {
            if (u is Unit unit)
            {
                // Player units first (0), enemy units second (1)
                int playerPriority = unit.name.Contains(currentPlayerCiv.CivName) ? 0 : 1;
                // Combat units first (0), civilian units second (1)
                int categoryPriority = u.IsCombatUnit() ? 0 : 1;
                return playerPriority * 10 + categoryPriority;
            }
            return 999;
        }).ToList();
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

        // Update unit cycle display
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

        // Button 0: Move/Cancel Move
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

        // Button 1: Attack
        if (unitActionButtons.Length > 1 && unitActionButtons[1])
        {
            var button = unitActionButtons[1];
            button.interactable = unit.Attack > 0 && !unit.HasMoved;
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText) buttonText.text = "Attack";
        }

        // Button 2: Show Attack Range (for combat units)
        if (unitActionButtons.Length > 2 && unitActionButtons[2])
        {
            var button = unitActionButtons[2];
            button.interactable = unit.IsCombatUnit();
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText) buttonText.text = "Range";
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
            case 1: // Attack
                Debug.Log($"Attack with {selectedUnit.UnitName}");
                // TODO: Implement attack targeting
                break;
            case 2: // Show Attack Range
                if (selectedUnit.IsCombatUnit())
                {
                    ClearAllHighlights();
                    ShowAttackRange(selectedUnit);
                }
                break;
            default:
                Debug.Log($"Unit action {actionIndex} for {selectedUnit.UnitName}");
                break;
        }
    }

    // Rest of the existing methods remain the same...
    void UpdateHUD()
    {
        if (gameManager == null) return;
        UpdateTurnPanel();
        UpdatePlayerStats();
        UpdateSelectionPanels();
    }

    void UpdateTurnPanel()
    {
        if (currentPlayerText && gameManager.CivsManager != null)
        {
            var aliveCivs = gameManager.CivsManager.GetAliveCivilizations();
            if (aliveCivs.Count > 0)
            {
                currentPlayerCiv = aliveCivs[0];
                currentPlayerText.text = $"{currentPlayerCiv.CivName}'s Turn";
                currentPlayerText.color = currentPlayerCiv.IsHuman ? player1Color : player2Color;
            }
        }

        if (tourCounterText)
            tourCounterText.text = $"Turn: {gameManager.CurrentTurn}";

        if (endTurnButton)
            endTurnButton.interactable = gameManager.CurrentGameState == GameState.Running;
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

    void UpdateSelectionPanels()
    {
        if (selectedUnit != null)
            ShowUnitInfo(selectedUnit);
        else
            HideUnitPanel();

        if (selectedCity != null)
            ShowCityManagement(selectedCity);
        else
            HideCityManagement();
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

        var currentProduction = city.GetCurrentProduction();
        if (currentProduction != null)
        {
            if (currentProductionName) currentProductionName.text = currentProduction.DisplayName;
            if (turnRemaining)
            {
                int turnsLeft = currentProduction.TurnsRemaining(yields.production);
                turnRemaining.text = $"{turnsLeft} turns";
            }
        }
        else
        {
            if (currentProductionName) currentProductionName.text = "No production";
            if (turnRemaining) turnRemaining.text = "-";
        }

        UpdateBuildingsList(city);
        UpdateUnitsList(city);
    }

    void UpdateBuildingsList(ICity city)
    {
        if (buildingsScrollView == null || buildingItemPrefab == null) return;

        foreach (Transform child in buildingsScrollView.content)
            Destroy(child.gameObject);

        var buildings = city.ConstructedBuildings;
        foreach (string buildingId in buildings)
        {
            var building = BuildingDatabase.GetBuilding(buildingId);
            if (building != null)
            {
                GameObject item = Instantiate(buildingItemPrefab, buildingsScrollView.content);
                var textComponent = item.GetComponent<TextMeshProUGUI>();
                if (textComponent) textComponent.text = building.DisplayName;
            }
        }
    }

    void UpdateUnitsList(ICity city)
    {
        if (unitsScrollView == null || unitItemPrefab == null) return;

        foreach (Transform child in unitsScrollView.content)
            Destroy(child.gameObject);

        if (city.Population > 0)
        {
            GameObject item = Instantiate(unitItemPrefab, unitsScrollView.content);
            var textComponent = item.GetComponent<TextMeshProUGUI>();
            if (textComponent) textComponent.text = "City Garrison";
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

    public void OnHexDeselected(Hex deselectedHex)
    {
        Debug.Log("Hex deselected");
    }

    ICity GetCityAtHex(Hex hex)
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

    void OnEndTurnClicked()
    {
        if (gameManager && gameManager.CurrentGameState == GameState.Running)
        {
            CancelMovementMode(); // Cancel any ongoing movement
            ClearAllHighlights(); // Clear any remaining highlights
            gameManager.NextTurn();
        }
    }

    void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"Game state changed to: {newState}");
        UpdateHUD();
    }

    void OnTurnChanged(int newTurn)
    {
        Debug.Log($"Turn changed to: {newTurn}");
        CancelMovementMode(); // Cancel movement when turn changes
        ClearAllHighlights(); // Clear any remaining highlights

        // If a unit is selected, show its movement range again (units get movement back)
        ShowMovementRange(selectedUnit);

        UpdateHUD();
    }

    void OnDestroy()
    {
        if (gameManager)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
            gameManager.OnTurnChanged -= OnTurnChanged;
        }

        var mapManager = gameManager?.MapManager;
        if (mapManager != null)
        {
            mapManager.OnHexSelected.RemoveListener(OnHexSelected);
            mapManager.OnHexDeselected.RemoveListener(OnHexDeselected);
        }
    }
}