using UnityEngine;

public class Hex : MonoBehaviour, IHex
{
    [Header("Coordinates")]
    public int q;
    public int r;

    [Header("Movement")]
    public int movementCost = 1;
    public bool isObstacle = false;

    [Header("Terrain & Resources")]
    public TerrainType terrain = TerrainType.Grassland;
    public ResourceType resourceCategory = ResourceType.None;
    public AdditionalResourceType additionalResource = AdditionalResourceType.None;
    public StrategicResourceType strategicResource = StrategicResourceType.None;
    public LuxuryResourceType luxuryResource = LuxuryResourceType.None;
    public ImprovementType improvement = ImprovementType.None;

    [Header("City Work")]
    public bool isWorked = false;
    public string workedByCityId = "";

    [Header("Yields")]
    public Yields baseYields = new Yields(2, 1, 0, 0, 0, 0);

    public int Q => q;
    public int R => r;
    public int S => -q - r;
    public Vector2Int AxialCoords => new Vector2Int(q, r);

    public int MovementCost => movementCost;
    public bool IsObstacle => isObstacle;

    public TerrainType Terrain
    {
        get => terrain;
        set
        {
            terrain = value;
            SetMovementCost(); // Update movement cost when terrain changes
        }
    }

    public ResourceType Resource
    {
        get => resourceCategory;
        set
        {
            resourceCategory = value;
            // Reset sub-resources when category changes
            if (value != ResourceType.Additional) additionalResource = AdditionalResourceType.None;
            if (value != ResourceType.Strategic) strategicResource = StrategicResourceType.None;
            if (value != ResourceType.Luxury) luxuryResource = LuxuryResourceType.None;
        }
    }
    public ImprovementType Improvement { get => improvement; set => improvement = value; }

    public bool IsWorked => isWorked;
    public string WorkedByCityId => workedByCityId;

    public void Init(int q, int r)
    {
        this.q = q;
        this.r = r;
     
        SetMovementCost();
    }

    public Yields GetTotalYields()
    {
        Yields total = baseYields;
        total += GetResourceYields();
        total += GetImprovementYields();
        return total;
    }

    public bool CanBeWorked()
    {
        return !isObstacle && !isWorked;
    }

    public void SetWorked(bool worked, string cityId = "")
    {
        isWorked = worked;
        workedByCityId = worked ? cityId : "";
    }

    private void SetMovementCost()
    {
        switch (terrain)
        {
            case TerrainType.Grassland:
                movementCost = 1;
                isObstacle = false;
                break;
            case TerrainType.Plains:
                movementCost = 1;
                isObstacle = false;
                break;
            case TerrainType.Desert:
                movementCost = 1;
                isObstacle = false;
                break;
            case TerrainType.Tundra:
                movementCost = 1;
                isObstacle = false;
                break;
            case TerrainType.Snow:
                movementCost = 2; // Snow is harder to traverse
                isObstacle = false;
                break;
            case TerrainType.Hill:
                movementCost = 2; // Hills cost more movement
                isObstacle = false;
                break;
            case TerrainType.Mountain:
                movementCost = 999; // Mountains are impassable
                isObstacle = true;
                break;
            case TerrainType.Ocean:
                movementCost = 999; // Ocean impassable to land units
                isObstacle = true;
                break;
            case TerrainType.Coast:
                movementCost = 1; // Coast is passable
                isObstacle = false;
                break;
            default:
                movementCost = 1;
                isObstacle = false;
                break;
        }

        Debug.Log($"Hex ({q}, {r}) terrain {terrain} set movement cost to {movementCost}");
    }

    private Yields GetResourceYields()
    {
        switch (resourceCategory)
        {
            case ResourceType.Additional:
                return GetAdditionalResourceYields();
            case ResourceType.Strategic:
                return GetStrategicResourceYields();
            case ResourceType.Luxury:
                return GetLuxuryResourceYields();
            default:
                return new Yields();
        }
    }

    private Yields GetAdditionalResourceYields()
    {
        switch (additionalResource)
        {
            case AdditionalResourceType.Cattle: return new Yields(food: 1, production: 1);
            case AdditionalResourceType.Wheat: return new Yields(food: 1);
            case AdditionalResourceType.Fish: return new Yields(food: 1);
            case AdditionalResourceType.Deer: return new Yields(food: 1);
            case AdditionalResourceType.Sheep: return new Yields(food: 1);
            default: return new Yields();
        }
    }

    private Yields GetStrategicResourceYields()
    {
        switch (strategicResource)
        {
            case StrategicResourceType.Iron: return new Yields(production: 1);
            case StrategicResourceType.Coal: return new Yields(production: 2);
            case StrategicResourceType.Oil: return new Yields(production: 3);
            case StrategicResourceType.Horses: return new Yields(production: 1);
            default: return new Yields();
        }
    }

    private Yields GetLuxuryResourceYields()
    {
        switch (luxuryResource)
        {
            case LuxuryResourceType.Gold: return new Yields(gold: 2);
            case LuxuryResourceType.Silver: return new Yields(gold: 1);
            case LuxuryResourceType.Gems: return new Yields(gold: 3);
            case LuxuryResourceType.Silk: return new Yields(gold: 2);
            case LuxuryResourceType.Diamonds: return new Yields(gold: 4);
            default: return new Yields(gold: 1);
        }
    }

    private Yields GetImprovementYields()
    {
        switch (improvement)
        {
            case ImprovementType.Farm: return new Yields(food: 1);
            case ImprovementType.Mine: return new Yields(production: 1);
            case ImprovementType.TradingPost: return new Yields(gold: 1);
            case ImprovementType.Pasture: return new Yields(food: 1, production: 1);
            case ImprovementType.FishingBoats: return new Yields(food: 1);
            default: return new Yields();
        }
    }

    

    // Helper method to get movement cost info for UI
    public string GetMovementCostInfo()
    {
        if (isObstacle)
        {
            return "Impassable";
        }
        else if (movementCost == 1)
        {
            return "1 MP";
        }
        else
        {
            return $"{movementCost} MP";
        }
    }

    // Helper method to check if terrain is difficult
    public bool IsDifficultTerrain()
    {
        return movementCost > 1 && !isObstacle;
    }
}