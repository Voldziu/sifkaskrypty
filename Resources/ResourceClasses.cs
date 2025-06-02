using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public class AdditionalResource : IAdditionalResource
{
    public AdditionalResourceType type;
    public int quantity;
    public int foodBonus;
    public int productionBonus;
    public GameObject prefab;

    public AdditionalResourceType Type => type;
    public int Quantity { get => quantity; set => quantity = value; }
    public int FoodBonus => foodBonus;
    public int ProductionBonus => productionBonus;
    public GameObject Prefab => prefab;

    public AdditionalResource(AdditionalResourceType type, GameObject prefab = null)
    {
        this.type = type;
        this.quantity = 0;
        this.prefab = prefab;
        InitializeValues();
    }

    void InitializeValues()
    {
        switch (type)
        {
            case AdditionalResourceType.Cattle:
                foodBonus = 1; productionBonus = 1; break;
            case AdditionalResourceType.Wheat:
                foodBonus = 1; productionBonus = 0; break;
            case AdditionalResourceType.Fish:
                foodBonus = 1; productionBonus = 0; break;
            case AdditionalResourceType.Deer:
                foodBonus = 1; productionBonus = 0; break;
            case AdditionalResourceType.Sheep:
                foodBonus = 1; productionBonus = 0; break;
            default:
                foodBonus = 0; productionBonus = 0; break;
        }
    }
}

[System.Serializable]
public class StrategicResource : IStrategicResource
{
    public StrategicResourceType type;
    public int quantity;
    public int productionPerTurn;
    public List<string> requiredBuildings;
    public List<string> requiredTechs;
    public GameObject prefab;

    public StrategicResourceType Type => type;
    public int Quantity { get => quantity; set => quantity = value; }
    public int ProductionPerTurn => productionPerTurn;
    public List<string> RequiredBuildings => requiredBuildings;
    public List<string> RequiredTechs => requiredTechs;
    public GameObject Prefab => prefab;

    public StrategicResource(StrategicResourceType type, GameObject prefab = null, int productionPerTurn = 1)
    {
        this.type = type;
        this.quantity = 0;
        this.productionPerTurn = productionPerTurn;
        this.prefab = prefab;
        this.requiredBuildings = new List<string>();
        this.requiredTechs = new List<string>();

        InitializeRequirements();
    }

    void InitializeRequirements()
    {
        switch (type)
        {
            case StrategicResourceType.Iron:
                requiredTechs.Add("iron_working");
                requiredBuildings.Add("mine");
                break;
            case StrategicResourceType.Coal:
                requiredTechs.Add("mining");
                requiredBuildings.Add("mine");
                break;
            case StrategicResourceType.Oil:
                requiredTechs.Add("combustion");
                requiredBuildings.Add("oil_well");
                break;
            case StrategicResourceType.Horses:
                requiredTechs.Add("animal_husbandry");
                requiredBuildings.Add("pasture");
                break;
        }
    }
}

[System.Serializable]
public class LuxuryResource : ILuxuryResource
{
    public LuxuryResourceType type;
    public int quantity;
    public int happinessBonus;
    public int tradeValue;
    public GameObject prefab;

    public LuxuryResourceType Type => type;
    public int Quantity { get => quantity; set => quantity = value; }
    public int HappinessBonus => happinessBonus;
    public int TradeValue => tradeValue;
    public GameObject Prefab => prefab;

    public LuxuryResource(LuxuryResourceType type, GameObject prefab = null)
    {
        this.type = type;
        this.quantity = 0;
        this.prefab = prefab;
        InitializeValues();
    }

    void InitializeValues()
    {
        switch (type)
        {
            case LuxuryResourceType.Gold:
                happinessBonus = 4; tradeValue = 240; break;
            case LuxuryResourceType.Silver:
                happinessBonus = 4; tradeValue = 120; break;
            case LuxuryResourceType.Gems:
                happinessBonus = 4; tradeValue = 300; break;
            case LuxuryResourceType.Silk:
                happinessBonus = 4; tradeValue = 180; break;
            case LuxuryResourceType.Spices:
                happinessBonus = 4; tradeValue = 150; break;
            case LuxuryResourceType.Diamonds:
                happinessBonus = 4; tradeValue = 400; break;
            default:
                happinessBonus = 4; tradeValue = 100; break;
        }
    }
}

[System.Serializable]
public class TradeRoute : ITradeRoute
{
    public string routeId;
    public ICity originCity;
    public ICity destinationCity;
    public int goldPerTurn;
    public int sciencePerTurn;
    public bool isActive;
    public int turnsRemaining;

    public string RouteId => routeId;
    public ICity OriginCity => originCity;
    public ICity DestinationCity => destinationCity;
    public int GoldPerTurn => goldPerTurn;
    public int SciencePerTurn => sciencePerTurn;
    public bool IsActive => isActive;
    public int TurnsRemaining => turnsRemaining;

    public TradeRoute(string routeId, ICity originCity, ICity destinationCity)
    {
        this.routeId = routeId;
        this.originCity = originCity;
        this.destinationCity = destinationCity;
        this.isActive = true;
        this.turnsRemaining = 30;

        CalculateTradeValue();
    }

    void CalculateTradeValue()
    {
        int baseGold = (originCity.Population + destinationCity.Population) / 2;
        int baseScience = originCity.Population / 3;

        goldPerTurn = baseGold + 2;
        sciencePerTurn = baseScience;
    }

    public void ProcessTurn()
    {
        if (isActive)
        {
            turnsRemaining--;
            if (turnsRemaining <= 0)
            {
                isActive = false;
            }
        }
    }
}