using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Rendering.CameraUI;

public class City : MonoBehaviour, ICity
{
    [Header("Basic Info")]
    public string cityId;
    public string cityName;
    public int population = 1;
    public float foodStored = 0f;

    [Header("Hex References")]
    public Hex centerHex;
    public List<Hex> workableHexes = new List<Hex>();
    public HashSet<Hex> workedHexes = new HashSet<Hex>();

    [Header("Buildings & Production")]
    public List<string> constructedBuildings = new List<string>();
    public Dictionary<string, int> specialists = new Dictionary<string, int>();
    public List<IProductionItem> productionQueue = new List<IProductionItem>();

    [Header("Management")]
    public CivManager civManager; // Parent CivManager reference

    private HexMapGenerator mapGenerator;


    public CivManager CivManager => civManager;
    public string CityId => cityId;
    public string CityName => cityName;
    public int Population => population;
    public float FoodStored => foodStored;
    public IHex CenterHex => centerHex;

    public List<IHex> WorkableHexes => workableHexes.Cast<IHex>().ToList();
    public HashSet<IHex> WorkedHexes => new HashSet<IHex>(workedHexes.Cast<IHex>());

    public List<string> ConstructedBuildings => constructedBuildings;
    public Dictionary<string, int> Specialists => specialists;
    public List<IProductionItem> ProductionQueue => productionQueue.Cast<IProductionItem>().ToList();

    public void Initialize(string id, string name, IHex centerHex, HexMapGenerator mapGenerator)
    {
        this.cityId = id;
        this.cityName = name;
        this.centerHex = (Hex)centerHex;
        this.mapGenerator = mapGenerator;

        GenerateWorkableHexes();
        WorkCityCenter();
    }

    public void SetCivManager(CivManager civManager)
    {
        this.civManager = civManager;
    }

    private void GenerateWorkableHexes()
    {
        workableHexes.Clear();

        for (int q = centerHex.Q - 3; q <= centerHex.Q + 3; q++)
        {
            for (int r = centerHex.R - 3; r <= centerHex.R + 3; r++)
            {
                if (Mathf.Abs(q - centerHex.Q) + Mathf.Abs(r - centerHex.R) + Mathf.Abs((-q - r) - centerHex.S) <= 6)
                {
                    Vector2Int coords = new Vector2Int(q, r);
                    if (mapGenerator.hexes.TryGetValue(coords, out Hex hex))
                    {
                        workableHexes.Add(hex);
                    }
                }
            }
        }
    }

    private void WorkCityCenter()
    {
        if (centerHex != null && !workedHexes.Contains(centerHex))
        {
            WorkHex(centerHex);
        }
    }

    public Yields GetTotalYields()
    {
        Yields total = new Yields();

        foreach (var hex in workedHexes)
        {
            total += hex.GetTotalYields();
        }

        foreach (string buildingId in constructedBuildings)
        {
            IBuilding building = BuildingDatabase.GetBuilding(buildingId);
            if (building != null)
            {
                total += building.Yields;
            }
        }

        foreach (var kvp in specialists)
        {
            IBuilding building = BuildingDatabase.GetBuilding(kvp.Key);
            if (building != null)
            {
                int specialistCount = kvp.Value;
                if (kvp.Key == "library" || kvp.Key == "university")
                    total.science += specialistCount * 2;
                else if (kvp.Key == "market")
                    total.gold += specialistCount * 2;
                else if (kvp.Key == "temple")
                    total.faith += specialistCount * 2;
            }
        }

        return total;
    }

    public int GetFoodRequiredForGrowth()
    {
        return 15 + 6 * (population - 1) + Mathf.FloorToInt((population - 1) / 4f);
    }

    public bool CanGrow()
    {
        return foodStored >= GetFoodRequiredForGrowth() &&
               workedHexes.Count < population + 1 &&
               GetAvailableWorkableHexes().Count > 0;
    }

    public void ProcessGrowth()
    {
        if (CanGrow())
        {
            foodStored -= GetFoodRequiredForGrowth();
            population++;
            OptimizeHexWork();
        }
    }

    public bool WorkHex(IHex hex)
    {
        Hex concreteHex = (Hex)hex;
        if (concreteHex == null || workedHexes.Count >= population ||
            !workableHexes.Contains(concreteHex) || !hex.CanBeWorked())
        {
            return false;
        }

        workedHexes.Add(concreteHex);
        hex.SetWorked(true, cityId);
        return true;
    }

    public bool StopWorkingHex(IHex hex)
    {
        Hex concreteHex = (Hex)hex;
        if (concreteHex != null && workedHexes.Contains(concreteHex) && concreteHex != centerHex)
        {
            workedHexes.Remove(concreteHex);
            hex.SetWorked(false);
            return true;
        }
        return false;
    }

    public List<IHex> GetAvailableWorkableHexes()
    {
        return workableHexes.Where(h => h.CanBeWorked()).Cast<IHex>().ToList();
    }

    public void OptimizeHexWork()
    {
        var available = workableHexes
            .Where(h => h.CanBeWorked())
            .OrderByDescending(h => h.GetTotalYields().food)
            .ThenByDescending(h => h.GetTotalYields().production)
            .ToList();

        while (workedHexes.Count < population && available.Count > 0)
        {
            Hex bestHex = available[0];
            available.RemoveAt(0);
            WorkHex(bestHex);
        }
    }

    public void AddToProductionQueue(IProductionItem item)
    {
        productionQueue.Add(item);
    }
    public IProductionItem GetCurrentProduction()
    {
        return productionQueue.Count > 0 ? productionQueue[0] : null;
    }

    public bool ProcessProduction(int productionYield)
    {

        Debug.Log($"Processing production for {cityName}: {productionYield} yield");
        IProductionItem current = GetCurrentProduction();
        if (current == null) return false;

        current.ProductionAccumulated += productionYield;

        if (current.IsCompleted)
        {
            CompleteProduction(current);
            productionQueue.RemoveAt(0);
            return true;
        }
        return false;
    }

    private void CompleteProduction(IProductionItem item)
    {
        if (item.ItemType == ProductionItemType.Building)
        {
            Building building = BuildingDatabase.GetBuilding(item.Id);
            if (building != null)
            {
                constructedBuildings.Add(item.Id);
                specialists[item.Id] = 0;
                Debug.Log($"{cityName} completed {building.DisplayName}");
                Debug.Log($"Length of construction buildings list: {constructedBuildings.Count}");
            }
        }
        else if (item.ItemType == ProductionItemType.Unit)
        {
            // Create unit at city location
            if (civManager?.UnitsManager != null)
            {
                var unitType = System.Enum.Parse<UnitType>(item.Id);
                var unitCategory = GetUnitCategory(unitType);
                civManager.UnitsManager.CreateUnit(unitCategory, unitType, centerHex);
            }
            Debug.Log($"{cityName} completed {item.DisplayName}");
        }
    }

    

    public void ChangeProduction(IProductionItem newItem)
    {
        var current = GetCurrentProduction();
        if (current != null)
        {
          
            productionQueue.RemoveAt(0);
        }

        StartProduction(newItem);
    }

  

    UnitCategory GetUnitCategory(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Settler:
            case UnitType.Worker:
                return UnitCategory.Civilian;
            default:
                return UnitCategory.Combat;
        }
    }

    public bool HasBuilding(string buildingId)
    {
        return constructedBuildings.Contains(buildingId);
    }

    public bool CanBuildBuilding(string buildingId)
    {
        IBuilding building = BuildingDatabase.GetBuilding(buildingId);
        if (building == null || HasBuilding(buildingId)) return false;

        foreach (string prereq in building.Prerequisites)
        {
            if (!HasBuilding(prereq)) return false;
        }
        return true;
    }

    public bool AssignSpecialist(string buildingId)
    {
        if (!HasBuilding(buildingId)) return false;

        IBuilding building = BuildingDatabase.GetBuilding(buildingId);
        int currentSpecialists = specialists.GetValueOrDefault(buildingId, 0);

        if (currentSpecialists < building.SpecialistSlots)
        {
            specialists[buildingId] = currentSpecialists + 1;
            return true;
        }
        return false;
    }

    public List<IProductionItem> GetAvailableProductionItems()
    {
        var availableItems = new List<IProductionItem>();

        // Add available buildings
        availableItems.AddRange(GetAvailableBuildingsForProduction());

        // Add available units
        availableItems.AddRange(GetAvailableUnitsForProduction());

        return availableItems;
    }



    // DYNAMIC PRODUCTION METHODS
    public List<IProductionItem> GetAvailableBuildingsForProduction()
    {
        var availableBuildings = new List<IProductionItem>();
        var allBuildings = BuildingDatabase.GetAllBuildings();

        var techManager = civManager?.TechManager;

        foreach (var buildingPair in allBuildings)
        {
            var building = buildingPair.Value;

            // Skip if already built
            if (HasBuilding(building.Id)) continue;

            // Check prerequisites (other buildings)
            bool hasPrereqs = true;
            if (building is Building buildingData)
            {
                foreach (var prereq in buildingData.Prerequisites)
                {
                    if (!HasBuilding(prereq))
                    {
                        hasPrereqs = false;
                        break;
                    }
                }
            }
            if (!hasPrereqs) continue;

            // Check tech requirements
            if (techManager != null && building.RequiredTechs != null)
            {
                bool hasTechs = true;
                foreach (var techId in building.RequiredTechs)
                {
                    if (!techManager.HasTechnology(techId))
                    {
                        hasTechs = false;
                        break;
                    }
                }
                if (!hasTechs) continue;
            }

            availableBuildings.Add(building);
        }

        return availableBuildings;
    }

    public List<IProductionItem> GetAvailableUnitsForProduction()
    {
        var availableUnits = new List<IProductionItem>();
        var allUnits = UnitDatabase.GetAllUnits();

        var techManager = civManager?.TechManager;

        foreach (var unitPair in allUnits)
        {
            var unit = unitPair.Value;

            // Check tech requirements
            bool canBuild = true;
            if (techManager != null && unit.RequiredTechs != null)
            {
                foreach (var techId in unit.RequiredTechs)
                {
                    if (!techManager.HasTechnology(techId))
                    {
                        canBuild = false;
                        break;
                    }
                }
            }

            if (canBuild)
            {
                availableUnits.Add(unit);
            }
        }

        return availableUnits;
    }


    public bool CanProduce(IProductionItem item)
    {
        if (item.ItemType == ProductionItemType.Building)
        {
            return CanProduceBuilding(item.Id);
        }
        else if (item.ItemType == ProductionItemType.Unit)
        {
            var unitData = item as UnitData;
            return unitData != null && CanProduceUnit(unitData.UnitType);
        }

        return false;
    }

    public bool CanProduceBuilding(string buildingId)
    {
        var building = BuildingDatabase.GetBuilding(buildingId);
        if (building == null || HasBuilding(buildingId)) return false;

        // Check prerequisites
        if (building is Building buildingData)
        {
            foreach (var prereq in buildingData.Prerequisites)
            {
                if (!HasBuilding(prereq)) return false;
            }
        }

        // Check tech requirements
        var techManager = civManager?.TechManager;
        if (techManager != null && building.RequiredTechs != null)
        {
            foreach (var techId in building.RequiredTechs)
            {
                if (!techManager.HasTechnology(techId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool CanProduceUnit(UnitType unitType)
    {
        var unit = UnitDatabase.GetUnitByType(unitType);
        if (unit == null) return false;

        var techManager = civManager?.TechManager;

        // Check tech requirements
        if (techManager != null && unit.RequiredTechs != null)
        {
            foreach (var techId in unit.RequiredTechs)
            {
                if (!techManager.HasTechnology(techId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void StartProduction(IProductionItem item)
    {
        if (!CanProduce(item)) return;

        AddToProductionQueue(item);
        Debug.Log($"{cityName} started producing {item.DisplayName}");
    }


    private int CalculateTurnsRemaining(IProductionItem item)
    {
        var yields = GetTotalYields();
        int productionPerTurn = yields.production;

        int accumulated = item.ProductionAccumulated;
        int remaining = item.ProductionCost - accumulated;

        return Mathf.CeilToInt((float)remaining / productionPerTurn);


    }

    public int GetTurnsRemaining()
    {
        IProductionItem current = GetCurrentProduction();
        if (current == null) return 0;
        return CalculateTurnsRemaining(current);
    }




    public void ProcessTurn()
    {
        Yields yields = GetTotalYields();

        int foodConsumption = population * 2;
        float foodSurplus = yields.food - foodConsumption;
        foodStored += foodSurplus;

        if (foodSurplus > 0)
        {
            ProcessGrowth();
        }

        Debug.Log($"Processing turn for {cityName}: Food Surplus = {foodSurplus}, Food Stored = {foodStored}");

        ProcessProduction(yields.production);
    }

    
    
}