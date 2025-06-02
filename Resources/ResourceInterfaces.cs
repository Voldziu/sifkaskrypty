using System.Collections.Generic;



public enum TerrainType
{
    Grassland, Plains, Desert, Tundra, Snow, Hill, Mountain, Ocean, Coast
}

public enum ResourceType
{
    None, Additional, Strategic, Luxury
}

public enum AdditionalResourceType
{
    None, Cattle, Wheat, Fish, Deer, Sheep
}

public enum StrategicResourceType
{
    None, Iron, Coal, Oil, Aluminum, Uranium, Horses
}

public enum LuxuryResourceType
{
    None, Gold, Silver, Gems, Silk, Spices, Wine, Furs, Ivory, Diamonds
}

public enum ImprovementType
{
    None, Farm, Mine, TradingPost, Pasture, FishingBoats
}

public interface IAdditionalResource
{
    AdditionalResourceType Type { get; }
    int Quantity { get; set; }
    int FoodBonus { get; }
    int ProductionBonus { get; }
}

public interface IStrategicResource
{
    StrategicResourceType Type { get; }
    int Quantity { get; set; }
    int ProductionPerTurn { get; }
    List<string> RequiredBuildings { get; }
    List<string> RequiredTechs { get; }
}

public interface ILuxuryResource
{
    LuxuryResourceType Type { get; }
    int Quantity { get; set; }
    int HappinessBonus { get; }
    int TradeValue { get; }
}

public interface ITradeRoute
{
    string RouteId { get; }
    ICity OriginCity { get; }
    ICity DestinationCity { get; }
    int GoldPerTurn { get; }
    int SciencePerTurn { get; }
    bool IsActive { get; }
    int TurnsRemaining { get; }

    void ProcessTurn();
}