using UnityEngine;

public class CivManager : MonoBehaviour, ICivManager
{
    [Header("Civilization")]
    public string civId;
    public string civName;

    [Header("Parent")]
    public ICivsManager civsManager;

    [Header("Sub-Managers")]
    public CitiesManager citiesManager;
    public UnitsManager unitsManager;
    public TechManager techManager;
    public DiplomacyManager diplomacyManager;
    public ResourceManager resourceManager;
    public CivTurnManager civTurnManager;

    [Header("Resource Settings")]
    public float goldPerTurn = 0f;
    public float scienceMultiplier = 1f;
    public float cultureMultiplier = 1f;
    public float faithMultiplier = 1f;

    private ICivilization civilization;
    private IMapManager mapManager;


    // Properties

    public ICivsManager CivsManager => civsManager;
    public ICivilization Civilization => civilization;
    public ICitiesManager CitiesManager => citiesManager;
    public IUnitsManager UnitsManager => unitsManager;
    public ITechManager TechManager => techManager;
    public IDiplomacyManager DiplomacyManager => diplomacyManager;
    public IResourceManager ResourceManager => resourceManager;

    public ICivTurnManager CivTurnManager => civTurnManager;


    public void Initialize(ICivilization civilization, IMapManager mapManager, ICivsManager civsManager)
    {
        this.civilization = civilization;
        this.mapManager = mapManager;
        this.civsManager = civsManager;
        this.civId = civilization.CivId;
        this.civName = civilization.CivName;

        // Setup all sub-managers
        SetupCitiesManager();
        SetupUnitsManager();
        SetupTechManager();
        SetupDiplomacyManager();
        SetupResourceManager();
        SetupCivTurnManager();

        // NO automatic city creation - we'll let units create cities
        Debug.Log($"CivManager initialized for {civilization.CivName}");
    }

    void SetupCitiesManager()
    {
        // Create CitiesManager if it doesn't exist
        if (citiesManager == null)
        {
            citiesManager = gameObject.AddComponent<CitiesManager>();
        }

        // Configure CitiesManager
        citiesManager.mapGenerator = mapManager.HexMap;

        // Create city prefab if needed (placeholder)
        if (citiesManager.cityPrefab == null)
        {
            CreateCityPrefab();
        }
    }

    void SetupUnitsManager()
    {
        if (unitsManager == null)
        {
            unitsManager = gameObject.AddComponent<UnitsManager>();
        }

        unitsManager.Initialize(civilization, mapManager);
    }

    void SetupTechManager()
    {
        if (techManager == null)
        {
            techManager = gameObject.AddComponent<TechManager>();
        }

        techManager.Initialize(civilization);
    }

    void SetupDiplomacyManager()
    {
        if (diplomacyManager == null)
        {
            diplomacyManager = gameObject.AddComponent<DiplomacyManager>();
        }

        diplomacyManager.Initialize(civilization, civsManager);
    }

    void SetupResourceManager()
    {
        if (resourceManager == null)
        {
            resourceManager = gameObject.AddComponent<ResourceManager>();
        }

        resourceManager.Initialize(civilization, mapManager);
    }
    void SetupCivTurnManager()
    {
        if (civTurnManager == null)
        {
            civTurnManager = gameObject.AddComponent<CivTurnManager>();
        }
        civTurnManager.Initialize(civilization, mapManager);
    }

    void CreateCityPrefab()
    {
        // Create a simple city prefab if none exists
        GameObject cityPrefab = new GameObject("CityPrefab");
        cityPrefab.AddComponent<City>();

        // Make it a prefab-like object (in real project, you'd assign a proper prefab)
        citiesManager.cityPrefab = cityPrefab;
        cityPrefab.SetActive(false); // Hide the template
    }

    public void ProcessTurn()
    {
        if (!IsAlive()) return;

        Debug.Log($"Processing turn for {civilization.CivName}");

        // Process all sub-managers in order
        citiesManager.ProcessTurn();
        unitsManager?.ProcessTurn();
        techManager?.ProcessTurn(civilization.Science);
        diplomacyManager?.ProcessTurn();
        resourceManager?.ProcessTurn();

        // Collect yields from cities and convert to civilization resources
        CollectYields();

        // Process other civ-specific systems
        ProcessResources();

        // Check if civilization should be destroyed
        CheckSurvival();
    }

    public void CollectYields()
    {
        var totalYields = citiesManager.GetTotalYields();

        // Convert city yields to civilization resources
        civilization.AddGold(totalYields.gold + Mathf.FloorToInt(goldPerTurn));
        civilization.AddScience(Mathf.FloorToInt(totalYields.science * scienceMultiplier));
        civilization.AddCulture(Mathf.FloorToInt(totalYields.culture * cultureMultiplier));
        civilization.AddFaith(Mathf.FloorToInt(totalYields.faith * faithMultiplier));

        if (citiesManager.GetCityCount() > 0)
        {
            Debug.Log($"{civilization.CivName} collected: " +
                      $"Gold +{totalYields.gold + goldPerTurn}, " +
                      $"Science +{totalYields.science}, " +
                      $"Culture +{totalYields.culture}, " +
                      $"Faith +{totalYields.faith}");
        }
    }

    public void ProcessResources()
    {
        // Handle maintenance costs
        ProcessMaintenance();

        // Handle resource decay or growth
        // (e.g., happiness, health, etc.)

        // Handle random events
        // ProcessRandomEvents();
    }

    void ProcessMaintenance()
    {
        // Calculate maintenance costs
        int cityMaintenance = GetCityCount() * 2; // 2 gold per city
        int buildingMaintenance = CalculateBuildingMaintenance();

        int totalMaintenance = cityMaintenance + buildingMaintenance;

        if (totalMaintenance > 0)
        {
            if (civilization.Gold >= totalMaintenance)
            {
                civilization.SpendGold(totalMaintenance);
                Debug.Log($"{civilization.CivName} paid {totalMaintenance} gold in maintenance");
            }
            else
            {
                // Deficit - handle negative consequences
                int deficit = totalMaintenance - civilization.Gold;
                civilization.Gold = 0;
                Debug.LogWarning($"{civilization.CivName} has a deficit of {deficit} gold!");

                // Could reduce production, happiness, etc.
            }
        }
    }

    int CalculateBuildingMaintenance()
    {
        // Calculate maintenance for all buildings in all cities
        int maintenance = 0;
        var cities = citiesManager.GetAllCities();

        foreach (var city in cities)
        {
            // Simple maintenance: 1 gold per building
            maintenance += city.ConstructedBuildings.Count;
        }

        return maintenance;
    }

    void CheckSurvival()
    {
        // Check if civilization should be destroyed
        if (GetCityCount() == 0 && unitsManager.GetUnitTypeCount(UnitType.Settler) == 0)
        {
            Debug.Log($"{civilization.CivName} has no cities and no settlers left!");
            DestroyCivilization();
        }
    }

    public int GetCityCount()
    {
        return citiesManager.GetCityCount();
    }

    public int GetTotalPopulation()
    {
        return citiesManager.GetTotalPopulation();
    }

    public Yields GetTotalYields()
    {
        return citiesManager.GetTotalYields();
    }

    public bool IsAlive()
    {
        return civilization.IsAlive;
    }

    public void DestroyCivilization()
    {
        if (civilization.IsAlive)
        {
            // Free up all worked hexes
            var cities = citiesManager.GetAllCities();
            foreach (var city in cities)
            {
                citiesManager.RemoveCity(city.CityId);
            }

            civilization.DestroyCivilization();

            // Disable this GameObject
            gameObject.SetActive(false);
        }
    }

    // Public methods for external use
    public bool CanAfford(int goldCost)
    {
        return civilization.Gold >= goldCost;
    }

    public bool SpendGold(int amount)
    {
        return civilization.SpendGold(amount);
    }

    public void FoundCity(string cityName, Hex location)
    {
        if (location != null)
        {
            var city = citiesManager.FoundCity(cityName, location);
            if (city != null)
            {
                Debug.Log($"{civilization.CivName} founded {cityName}");
            }
        }
    }

    // Debug info
    public string GetDebugInfo()
    {
        var info = $"{civilization.CivName}: " +
                   $"Cities: {GetCityCount()}, " +
                   $"Pop: {GetTotalPopulation()}, " +
                   $"Gold: {civilization.Gold}, " +
                   $"Science: {civilization.Science}, " +
                   $"Culture: {civilization.Culture}";

        if (unitsManager != null)
            info += $", Units: {unitsManager.UnitCount}";

        if (techManager != null)
            info += $", Techs: {techManager.ResearchedTechs.Count}";

        if (resourceManager != null)
            info += $", Resources: {resourceManager.GetTotalStrategicResources()}";

        return info;
    }
}