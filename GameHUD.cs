using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
    public Button[] unitActionButtons; // Array of action buttons

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
    public GameObject buildingItemPrefab; // Prefab for building list items

    [Header("Units Scroll View")]
    public ScrollRect unitsScrollView;
    public GameObject unitItemPrefab; // Prefab for unit list items

    [Header("Current Production")]
    public TextMeshProUGUI turnRemaining;
    public TextMeshProUGUI currentProductionName;

    [Header("Colors")]
    public Color player1Color = Color.blue;
    public Color player2Color = Color.red;

    private ICivilization currentPlayerCiv;
    private IUnit selectedUnit;
    private ICity selectedCity;

    void Start()
    {
        SetupEventListeners();
        HideAllPanels();
        UpdateHUD();
    }

    void SetupEventListeners()
    {
        if (endTurnButton) endTurnButton.onClick.AddListener(OnEndTurnClicked);

        // Setup unit action buttons
        for (int i = 0; i < unitActionButtons.Length; i++)
        {
            int index = i; // Capture for closure
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
    }

    void UpdateHUD()
    {
        if (gameManager == null) return;

        UpdateTurnPanel();
        UpdatePlayerStats();
        UpdateSelectionPanels();
    }

    void UpdateTurnPanel()
    {
        // Update current player display
        if (currentPlayerText && gameManager.CivsManager != null)
        {
            var aliveCivs = gameManager.CivsManager.GetAliveCivilizations();
            if (aliveCivs.Count > 0)
            {
                currentPlayerCiv = aliveCivs[0]; // For now, show first civ as current player
                currentPlayerText.text = $"{currentPlayerCiv.CivName}'s Turn";
                currentPlayerText.color = currentPlayerCiv.IsHuman ? player1Color : player2Color;
            }
        }

        // Update turn counter
        if (tourCounterText)
        {
            tourCounterText.text = $"Turn: {gameManager.CurrentTurn}";
        }

        // Update end turn button state
        if (endTurnButton)
        {
            endTurnButton.interactable = gameManager.CurrentGameState == GameState.Running;
        }
    }

    void UpdatePlayerStats()
    {
        if (currentPlayerCiv?.CivManager == null) return;

        var civManager = currentPlayerCiv.CivManager;

        if (citiesText)
            citiesText.text = $"Cities: {civManager.GetCityCount()}";

        if (goldText)
            goldText.text = $"Gold: {currentPlayerCiv.Gold}";

        if (scienceText)
            scienceText.text = $"Science: {currentPlayerCiv.Science}";

        if (cultureText)
            cultureText.text = $"Culture: {currentPlayerCiv.Culture}";
    }

    void UpdateSelectionPanels()
    {
        // Update unit panel
        if (selectedUnit != null)
        {
            ShowUnitInfo(selectedUnit);
        }
        else
        {
            HideUnitPanel();
        }

        // Update city management panel
        if (selectedCity != null)
        {
            ShowCityManagement(selectedCity);
        }
        else
        {
            HideCityManagement();
        }
    }

    public void ShowUnitInfo(IUnit unit)
    {
        selectedUnit = unit; // Store the selected unit
        if (unitPanel) unitPanel.SetActive(true);

        if (unitNameText)
            unitNameText.text = $"{unit.UnitType}: {unit.UnitName}";

        if (unitHealthText)
            unitHealthText.text = $"HP: {unit.Health}/{unit.MaxHealth}";

        if (unitMovementText)
            unitMovementText.text = $"Movement: {unit.Movement}/{unit.MaxMovement}";

        // Update action button states
        UpdateUnitActionButtons(unit);

        Debug.Log($"Showing unit info for: {unit.UnitName}");
    }

    void UpdateUnitActionButtons(IUnit unit)
    {
        if (unitActionButtons == null) return;

        // Button 0: Move
        if (unitActionButtons.Length > 0 && unitActionButtons[0])
            unitActionButtons[0].interactable = unit.Movement > 0 && !unit.HasMoved;

        // Button 1: Attack
        if (unitActionButtons.Length > 1 && unitActionButtons[1])
            unitActionButtons[1].interactable = unit.Attack > 0 && !unit.HasMoved;
    }

    public void ShowCityManagement(ICity city)
    {
        selectedCity = city;
        if (cityManagementPanel) cityManagementPanel.SetActive(true);

        // Update city basic info
        if (cityName)
            cityName.text = city.CityName;

        if (population)
            population.text = $"Population: {city.Population}";

        // Update city yields
        var yields = city.GetTotalYields();
        if (food)
            food.text = $"Food: {yields.food}";
        if (production)
            production.text = $"Production: {yields.production}";
        if (culture)
            culture.text = $"Culture: {yields.culture}";
        if (science)
            science.text = $"Science: {yields.science}";

        // Update current production
        var currentProduction = city.GetCurrentProduction();
        if (currentProduction != null)
        {
            if (currentProductionName)
                currentProductionName.text = currentProduction.DisplayName;

            if (turnRemaining)
            {
                int turnsLeft = currentProduction.TurnsRemaining(yields.production);
                turnRemaining.text = $"{turnsLeft} turns";
            }
        }
        else
        {
            if (currentProductionName)
                currentProductionName.text = "No production";
            if (turnRemaining)
                turnRemaining.text = "-";
        }

        // Update scrollable lists
        UpdateBuildingsList(city);
        UpdateUnitsList(city);
    }

    void UpdateBuildingsList(ICity city)
    {
        if (buildingsScrollView == null || buildingItemPrefab == null) return;

        // Clear existing items
        foreach (Transform child in buildingsScrollView.content)
        {
            Destroy(child.gameObject);
        }

        // Add buildings
        var buildings = city.ConstructedBuildings;
        foreach (string buildingId in buildings)
        {
            var building = BuildingDatabase.GetBuilding(buildingId);
            if (building != null)
            {
                GameObject item = Instantiate(buildingItemPrefab, buildingsScrollView.content);
                var textComponent = item.GetComponent<TextMeshProUGUI>();
                if (textComponent)
                    textComponent.text = building.DisplayName;
            }
        }
    }

    void UpdateUnitsList(ICity city)
    {
        if (unitsScrollView == null || unitItemPrefab == null) return;

        // Clear existing items
        foreach (Transform child in unitsScrollView.content)
        {
            Destroy(child.gameObject);
        }

        // Get units in the city (simplified - you might want to get units from UnitsManager)
        // For now, just show placeholder
        if (city.Population > 0)
        {
            GameObject item = Instantiate(unitItemPrefab, unitsScrollView.content);
            var textComponent = item.GetComponent<TextMeshProUGUI>();
            if (textComponent)
                textComponent.text = "City Garrison";
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

    void HandleInput()
    {
        // ESC to deselect
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideAllPanels();
            if (gameManager?.MapManager != null)
            {
                gameManager.MapManager.DeselectHex();
            }
        }
    }

    // Event Handlers
    public void OnHexSelected(Hex selectedHex)
    {
        if (selectedHex == null) return;

        Debug.Log($"Hex selected at ({selectedHex.Q}, {selectedHex.R})");

        // Check if hex has units FIRST (prioritize unit selection)
        IUnit unitOnHex = GetUnitAtHex(selectedHex);
        if (unitOnHex != null)
        {
            Debug.Log($"Unit found: {unitOnHex.UnitName}");
            ShowUnitInfo(unitOnHex);

            // Still check for city, but don't hide unit panel
            ICity cityOnHex = GetCityAtHex(selectedHex);
            if (cityOnHex != null)
            {
                ShowCityManagement(cityOnHex);
            }
            else
            {
                // Only hide city panel if no city, keep unit panel
                HideCityManagement();
            }
        }
        else
        {
            // No unit found, check for city
            ICity cityOnHex = GetCityAtHex(selectedHex);
            if (cityOnHex != null)
            {
                ShowCityManagement(cityOnHex);
                HideUnitPanel(); // Hide unit panel since no unit
            }
            else
            {
                // Nothing on this hex
                HideAllPanels();
            }
        }
    }

    public void OnHexDeselected(Hex deselectedHex)
    {
        // Don't automatically hide panels on deselect - let user click elsewhere or press ESC
        Debug.Log("Hex deselected");
    }

    ICity GetCityAtHex(Hex hex)
    {
        if (currentPlayerCiv?.CivManager?.CitiesManager == null) return null;

        var cities = currentPlayerCiv.CivManager.CitiesManager.GetAllCities();
        foreach (var city in cities)
        {
            // Check if this hex is the city center
            if (city.CenterHex != null &&
                city.CenterHex.Q == hex.Q &&
                city.CenterHex.R == hex.R)
            {
                return city;
            }
        }
        return null;
    }



    IUnit GetUnitAtHex(Hex hex)
    {
        if (currentPlayerCiv?.CivManager?.UnitsManager == null)
        {
            Debug.Log("No UnitsManager found for current player");
            return null;
        }

        var units = currentPlayerCiv.CivManager.UnitsManager.GetUnitsAt(hex);
        if (units != null && units.Count > 0)
        {
            Debug.Log($"Found {units.Count} units at hex ({hex.Q}, {hex.R})");
            // Return the first unit (you could add logic to cycle through multiple units)
            return units[0];
        }

        // Also check other civilizations' units if you want to see enemy units
        var allCivs = gameManager.CivsManager.GetAliveCivilizations();
        foreach (var civ in allCivs)
        {
            if (civ.CivId != currentPlayerCiv.CivId && civ.CivManager?.UnitsManager != null)
            {
                var enemyUnits = civ.CivManager.UnitsManager.GetUnitsAt(hex);
                if (enemyUnits != null && enemyUnits.Count > 0)
                {
                    Debug.Log($"Found {enemyUnits.Count} enemy units from {civ.CivName} at hex ({hex.Q}, {hex.R})");
                    return enemyUnits[0]; // Return first enemy unit
                }
            }
        }

        return null;
    }

    // Button Event Handlers
    void OnEndTurnClicked()
    {
        if (gameManager && gameManager.CurrentGameState == GameState.Running)
        {
            gameManager.NextTurn();
        }
    }

    void OnUnitActionClicked(int actionIndex)
    {
        if (selectedUnit == null) return;

        switch (actionIndex)
        {
            case 0: // Move
                Debug.Log($"Move {selectedUnit.UnitName}");
                break;
            case 1: // Attack
                Debug.Log($"Attack with {selectedUnit.UnitName}");
                break;
            default:
                Debug.Log($"Unit action {actionIndex} for {selectedUnit.UnitName}");
                break;
        }
    }

    void HandleUnitReselection(IUnit unit)
    {
        if (selectedUnit == unit)
        {
            // Same unit clicked again - could cycle through units on same hex
            var hex = unit.CurrentHex;
            var unitsOnHex = currentPlayerCiv?.CivManager?.UnitsManager?.GetUnitsAt(hex);

            if (unitsOnHex != null && unitsOnHex.Count > 1)
            {
                // Find current unit index and select next one
                int currentIndex = unitsOnHex.IndexOf(unit);
                int nextIndex = (currentIndex + 1) % unitsOnHex.Count;
                ShowUnitInfo(unitsOnHex[nextIndex]);
                Debug.Log($"Cycling to next unit: {unitsOnHex[nextIndex].UnitName}");
            }
        }
        else
        {
            // Different unit selected
            ShowUnitInfo(unit);
        }
    }

    // Game Event Handlers
    void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"Game state changed to: {newState}");
        UpdateHUD();
    }

    void OnTurnChanged(int newTurn)
    {
        Debug.Log($"Turn changed to: {newTurn}");
        UpdateHUD();
    }

    void OnDestroy()
    {
        // Unsubscribe from events
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