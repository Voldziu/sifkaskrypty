using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    public List<ProductionItem> productionQueue = new List<ProductionItem>();

    private HexMapGenerator mapGenerator;


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
        productionQueue.Add((ProductionItem)item);
    }

    public IProductionItem GetCurrentProduction()
    {
        return productionQueue.Count > 0 ? productionQueue[0] : null;
    }

    public bool ProcessProduction(int productionYield)
    {
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
        if (item.Type == ProductionType.Building)
        {
            IBuilding building = BuildingDatabase.GetBuilding(item.Id);
            if (building != null)
            {
                constructedBuildings.Add(item.Id);
                specialists[item.Id] = 0;
                Debug.Log($"{cityName} completed {building.DisplayName}");
            }
        }
        else if (item.Type == ProductionType.Unit)
        {
            Debug.Log($"{cityName} completed {item.DisplayName}");
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

        ProcessProduction(yields.production);
    }
}