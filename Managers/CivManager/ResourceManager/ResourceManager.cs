using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class ResourceManager : MonoBehaviour, IResourceManager
{
    [Header("Resource Settings")]
    public int baseTradeRoutes = 1;
    public int tradeRoutesPerEra = 1;

    [Header("Resource Prefabs")]
    public ResourcePrefabData[] resourcePrefabs;

    [Header("Resource Visuals")]
    public Transform resourceVisualsParent;

    private ICivilization civilization;
    private IMapManager mapManager;
    private Dictionary<AdditionalResourceType, IAdditionalResource> additionalResources = new Dictionary<AdditionalResourceType, IAdditionalResource>();
    private Dictionary<StrategicResourceType, IStrategicResource> strategicResources = new Dictionary<StrategicResourceType, IStrategicResource>();
    private Dictionary<LuxuryResourceType, ILuxuryResource> luxuryResources = new Dictionary<LuxuryResourceType, ILuxuryResource>();
    private List<ITradeRoute> tradeRoutes = new List<ITradeRoute>();
    private Dictionary<Vector2Int, GameObject> resourceVisuals = new Dictionary<Vector2Int, GameObject>();
    private int tradeRouteIdCounter = 0;

    // Properties
    public ICivilization Civilization => civilization;
    public Dictionary<AdditionalResourceType, IAdditionalResource> AdditionalResources => additionalResources;
    public Dictionary<StrategicResourceType, IStrategicResource> StrategicResources => strategicResources;
    public Dictionary<LuxuryResourceType, ILuxuryResource> LuxuryResources => luxuryResources;
    public List<ITradeRoute> TradeRoutes => tradeRoutes;

    // Events
    public event System.Action<AdditionalResourceType, int> OnAdditionalResourceChanged;
    public event System.Action<StrategicResourceType, int> OnStrategicResourceChanged;
    public event System.Action<LuxuryResourceType, int> OnLuxuryResourceChanged;
    public event System.Action<ITradeRoute> OnTradeRouteCreated;
    public event System.Action<ITradeRoute> OnTradeRouteDestroyed;

    public void Initialize(ICivilization civilization, IMapManager mapManager)
    {
        this.civilization = civilization;
        this.mapManager = mapManager;

        InitializeResources();

        if (resourceVisualsParent == null)
        {
            var resourceParent = new GameObject($"ResourceVisuals_{civilization.CivName}");
            resourceParent.transform.SetParent(transform);
            resourceVisualsParent = resourceParent.transform;
        }

        Debug.Log($"ResourceManager initialized for {civilization.CivName}");
    }

    void InitializeResources()
    {
        // Initialize Additional Resources
        foreach (AdditionalResourceType type in System.Enum.GetValues(typeof(AdditionalResourceType)))
        {
            if (type != AdditionalResourceType.None)
            {
                var prefab = GetResourcePrefab(ResourceType.Additional, additionalType: type);
                additionalResources[type] = new AdditionalResource(type, prefab);
            }
        }

        // Initialize Strategic Resources
        foreach (StrategicResourceType type in System.Enum.GetValues(typeof(StrategicResourceType)))
        {
            if (type != StrategicResourceType.None)
            {
                var prefab = GetResourcePrefab(ResourceType.Strategic, strategicType: type);
                strategicResources[type] = new StrategicResource(type, prefab);
            }
        }

        // Initialize Luxury Resources
        foreach (LuxuryResourceType type in System.Enum.GetValues(typeof(LuxuryResourceType)))
        {
            if (type != LuxuryResourceType.None)
            {
                var prefab = GetResourcePrefab(ResourceType.Luxury, luxuryType: type);
                luxuryResources[type] = new LuxuryResource(type, prefab);
            }
        }
    }

    GameObject GetResourcePrefab(ResourceType category, AdditionalResourceType additionalType = AdditionalResourceType.None,
                                StrategicResourceType strategicType = StrategicResourceType.None,
                                LuxuryResourceType luxuryType = LuxuryResourceType.None)
    {
        if (resourcePrefabs == null) return null;

        foreach (var prefabData in resourcePrefabs)
        {
            if (prefabData.resourceCategory == category)
            {
                switch (category)
                {
                    case ResourceType.Additional:
                        if (prefabData.additionalType == additionalType) return prefabData.resourcePrefab;
                        break;
                    case ResourceType.Strategic:
                        if (prefabData.strategicType == strategicType) return prefabData.resourcePrefab;
                        break;
                    case ResourceType.Luxury:
                        if (prefabData.luxuryType == luxuryType) return prefabData.resourcePrefab;
                        break;
                }
            }
        }
        return null;
    }

    public int GetAdditionalResourceCount(AdditionalResourceType type)
    {
        return additionalResources.GetValueOrDefault(type)?.Quantity ?? 0;
    }

    public bool HasAdditionalResource(AdditionalResourceType type, int amount = 1)
    {
        return GetAdditionalResourceCount(type) >= amount;
    }

    public int GetStrategicResourceCount(StrategicResourceType type)
    {
        return strategicResources.GetValueOrDefault(type)?.Quantity ?? 0;
    }

    public bool HasStrategicResource(StrategicResourceType type, int amount = 1)
    {
        return GetStrategicResourceCount(type) >= amount;
    }

    public bool ConsumeStrategicResource(StrategicResourceType type, int amount)
    {
        if (!HasStrategicResource(type, amount)) return false;

        strategicResources[type].Quantity -= amount;
        OnStrategicResourceChanged?.Invoke(type, strategicResources[type].Quantity);

        return true;
    }

    public int GetLuxuryResourceCount(LuxuryResourceType type)
    {
        return luxuryResources.GetValueOrDefault(type)?.Quantity ?? 0;
    }

    public bool HasLuxuryResource(LuxuryResourceType type)
    {
        return GetLuxuryResourceCount(type) > 0;
    }

    public int GetTotalHappiness()
    {
        int totalHappiness = 0;

        foreach (var resource in luxuryResources.Values)
        {
            if (resource.Quantity > 0)
            {
                totalHappiness += resource.HappinessBonus;
            }
        }

        return totalHappiness;
    }

    public bool CreateTradeRoute(ICity originCity, ICity destinationCity)
    {
        if (originCity == null || destinationCity == null) return false;
        if (tradeRoutes.Count >= GetMaxTradeRoutes()) return false;
        if (originCity.CityId == destinationCity.CityId) return false;

        if (tradeRoutes.Any(tr => tr.OriginCity == originCity && tr.DestinationCity == destinationCity))
        {
            return false;
        }

        string routeId = $"trade_{++tradeRouteIdCounter}";
        var tradeRoute = new TradeRoute(routeId, originCity, destinationCity);

        tradeRoutes.Add(tradeRoute);
        OnTradeRouteCreated?.Invoke(tradeRoute);

        Debug.Log($"Created trade route from {originCity.CityName} to {destinationCity.CityName} (+{tradeRoute.GoldPerTurn} gold/turn)");
        return true;
    }

    public bool RemoveTradeRoute(string routeId)
    {
        var route = tradeRoutes.FirstOrDefault(tr => tr.RouteId == routeId);
        if (route == null) return false;

        tradeRoutes.Remove(route);
        OnTradeRouteDestroyed?.Invoke(route);

        Debug.Log($"Removed trade route {routeId}");
        return true;
    }

    public int GetMaxTradeRoutes()
    {
        return baseTradeRoutes + tradeRoutesPerEra;
    }

    public void DiscoverResourcesOnMap()
    {
        var citiesManager = civilization.CivManager?.CitiesManager;
        if (citiesManager == null) return;

        var cities = citiesManager.GetAllCities();
        foreach (var city in cities)
        {
            foreach (var hex in city.WorkedHexes)
            {
                DiscoverResourceOnHex(hex);
                CreateResourceVisual(hex);
            }
        }
    }

    void DiscoverResourceOnHex(IHex hex)
    {
        switch (hex.Resource)
        {
            case ResourceType.Additional:
                var additionalType = ((Hex)hex).additionalResource;
                if (additionalType != AdditionalResourceType.None)
                {
                    additionalResources[additionalType].Quantity += 1;
                    OnAdditionalResourceChanged?.Invoke(additionalType, additionalResources[additionalType].Quantity);
                }
                break;

            case ResourceType.Strategic:
                var strategicType = ((Hex)hex).strategicResource;
                if (strategicType != StrategicResourceType.None && CanExtractResource(strategicType, hex))
                {
                    strategicResources[strategicType].Quantity += 1;
                    OnStrategicResourceChanged?.Invoke(strategicType, strategicResources[strategicType].Quantity);
                }
                break;

            case ResourceType.Luxury:
                var luxuryType = ((Hex)hex).luxuryResource;
                if (luxuryType != LuxuryResourceType.None)
                {
                    luxuryResources[luxuryType].Quantity += 1;
                    OnLuxuryResourceChanged?.Invoke(luxuryType, luxuryResources[luxuryType].Quantity);
                }
                break;
        }
    }

    void CreateResourceVisual(IHex hex)
    {
        if (hex.Resource == ResourceType.None) return;

        Vector2Int hexCoords = new Vector2Int(hex.Q, hex.R);
        if (resourceVisuals.ContainsKey(hexCoords)) return;

        GameObject prefab = null;

        switch (hex.Resource)
        {
            case ResourceType.Additional:
                prefab = GetResourcePrefab(ResourceType.Additional, additionalType: ((Hex)hex).additionalResource);
                break;
            case ResourceType.Strategic:
                prefab = GetResourcePrefab(ResourceType.Strategic, strategicType: ((Hex)hex).strategicResource);
                break;
            case ResourceType.Luxury:
                prefab = GetResourcePrefab(ResourceType.Luxury, luxuryType: ((Hex)hex).luxuryResource);
                break;
        }

        if (prefab == null) return;

        Vector3 worldPos = mapManager.HexMap.HexToWorld(hex.Q, hex.R);
        worldPos.y += 0.1f;

        GameObject resourceVisual = Instantiate(prefab, worldPos, Quaternion.identity, resourceVisualsParent);
        resourceVisual.name = $"Resource_{hex.Resource}_{hex.Q}_{hex.R}";

        resourceVisuals[hexCoords] = resourceVisual;
    }

    bool CanExtractResource(StrategicResourceType resourceType, IHex hex)
    {
        var resource = strategicResources[resourceType];
        var civManager = civilization.CivManager;

        if (civManager?.TechManager != null)
        {
            foreach (var techId in resource.RequiredTechs)
            {
                if (!civManager.TechManager.HasTechnology(techId))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public void ProcessResourceExtraction()
    {
        DiscoverResourcesOnMap();
    }

    public void ProcessTurn()
    {
        foreach (var route in tradeRoutes.ToList())
        {
            route.ProcessTurn();

            if (!route.IsActive)
            {
                RemoveTradeRoute(route.RouteId);
            }
        }

        ProcessResourceExtraction();
        ApplyTradeRouteBenefits();

        Debug.Log($"{civilization.CivName} resources - Additional: {GetTotalAdditionalResources()}, Strategic: {GetTotalStrategicResources()}, Luxury: {GetTotalLuxuryResources()}, Happiness: +{GetTotalHappiness()}");
    }

    void ApplyTradeRouteBenefits()
    {
        int totalGold = 0;
        int totalScience = 0;

        foreach (var route in tradeRoutes.Where(tr => tr.IsActive))
        {
            totalGold += route.GoldPerTurn;
            totalScience += route.SciencePerTurn;
        }

        if (totalGold > 0) civilization.AddGold(totalGold);
        if (totalScience > 0) civilization.AddScience(totalScience);
    }

    public int GetTotalAdditionalResources()
    {
        return additionalResources.Values.Sum(r => r.Quantity);
    }

    public int GetTotalStrategicResources()
    {
        return strategicResources.Values.Sum(r => r.Quantity);
    }

    public int GetTotalLuxuryResources()
    {
        return luxuryResources.Values.Sum(r => r.Quantity);
    }

    public string GetDebugInfo()
    {
        return $"Additional: {GetTotalAdditionalResources()}, Strategic: {GetTotalStrategicResources()}, Luxury: {GetTotalLuxuryResources()}, Trade Routes: {tradeRoutes.Count}/{GetMaxTradeRoutes()}, Happiness: +{GetTotalHappiness()}";
    }
}