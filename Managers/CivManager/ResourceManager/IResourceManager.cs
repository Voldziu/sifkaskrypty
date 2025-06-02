using System.Collections.Generic;

public interface IResourceManager
{
    ICivilization Civilization { get; }

    // Initialization
    void Initialize(ICivilization civilization, IMapManager mapManager);

    // Additional Resources
    Dictionary<AdditionalResourceType, IAdditionalResource> AdditionalResources { get; }
    int GetAdditionalResourceCount(AdditionalResourceType type);
    bool HasAdditionalResource(AdditionalResourceType type, int amount = 1);

    // Strategic Resources
    Dictionary<StrategicResourceType, IStrategicResource> StrategicResources { get; }
    int GetStrategicResourceCount(StrategicResourceType type);
    bool HasStrategicResource(StrategicResourceType type, int amount = 1);
    bool ConsumeStrategicResource(StrategicResourceType type, int amount);

    // Luxury Resources
    Dictionary<LuxuryResourceType, ILuxuryResource> LuxuryResources { get; }
    int GetLuxuryResourceCount(LuxuryResourceType type);
    bool HasLuxuryResource(LuxuryResourceType type);
    int GetTotalHappiness();

    // Trade Routes
    List<ITradeRoute> TradeRoutes { get; }
    bool CreateTradeRoute(ICity originCity, ICity destinationCity);
    bool RemoveTradeRoute(string routeId);
    int GetMaxTradeRoutes();

    // Resource Discovery
    void DiscoverResourcesOnMap();
    void ProcessResourceExtraction();

    // Turn Processing
    void ProcessTurn();

    // Events
    event System.Action<AdditionalResourceType, int> OnAdditionalResourceChanged;
    event System.Action<StrategicResourceType, int> OnStrategicResourceChanged;
    event System.Action<LuxuryResourceType, int> OnLuxuryResourceChanged;
    event System.Action<ITradeRoute> OnTradeRouteCreated;
    event System.Action<ITradeRoute> OnTradeRouteDestroyed;
}