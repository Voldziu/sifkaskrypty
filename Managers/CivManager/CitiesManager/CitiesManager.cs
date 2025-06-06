using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CitiesManager : MonoBehaviour, ICitiesManager
{

    [Header("References")]
    public CivManager civManager;
    [Header("Dependencies")]
    public HexMapGenerator mapGenerator;
    public GameObject cityPrefab;

    [Header("Cities")]
    public Dictionary<string, City> cities = new Dictionary<string, City>();

    private int cityIdCounter = 0;

    public Dictionary<string, ICity> Cities => cities.ToDictionary(kvp => kvp.Key, kvp => (ICity)kvp.Value);
    public CivManager CivManager => civManager;

    public ICity FoundCity(string cityName, IHex location)
    {
        Hex hexLocation = (Hex)location;
        if (hexLocation == null || hexLocation.IsObstacle)
        {
            Debug.LogWarning("Cannot found city on invalid location");
            return null;
        }

        foreach (var existingCity in cities.Values)
        {
            if (mapGenerator.GetDistance(hexLocation, existingCity.centerHex) < 4)
            {
                Debug.LogWarning("Cannot found city too close to existing city");
                return null;
            }
        }

        string cityId = $"city_{++cityIdCounter}";

        Vector3 worldPos = mapGenerator.HexToWorld(hexLocation.Q, hexLocation.R);
        GameObject cityGO = Instantiate(cityPrefab, worldPos, Quaternion.identity, transform);

        City city = cityGO.GetComponent<City>();
        if (city == null)
        {
            city = cityGO.AddComponent<City>();
        }

        city.Initialize(cityId, cityName, location, mapGenerator,civManager);
        cities[cityId] = city;

        Debug.Log($"Founded {cityName} at ({location.Q}, {location.R})");
        return city;
    }

    public ICity GetCity(string cityId)
    {
        return cities.GetValueOrDefault(cityId);
    }

    public List<ICity> GetAllCities()
    {
        return cities.Values.Cast<ICity>().ToList();
    }

    public bool RemoveCity(string cityId)
    {
        if (cities.TryGetValue(cityId, out City city))
        {
            foreach (var hex in city.workedHexes)
            {
                hex.SetWorked(false);
            }

            if (city != null)
                Destroy(city.gameObject);

            cities.Remove(cityId);
            return true;
        }
        return false;
    }

    public void ProcessTurn()
    {
        foreach (var city in cities.Values)
        {
            city.ProcessTurn();
        }
    }

    public int GetTotalPopulation()
    {
        return cities.Values.Sum(city => city.Population);
    }

    public Yields GetTotalYields()
    {
        Yields total = new Yields();
        foreach (var city in cities.Values)
        {
            total += city.GetTotalYields();
        }
        return total;
    }

    public int GetCityCount()
    {
        return cities.Count;
    }

    public ICity GetNearestCity(Vector3 worldPosition)
    {
        City nearest = null;
        float minDistance = float.MaxValue;

        foreach (var city in cities.Values)
        {
            float distance = Vector3.Distance(worldPosition, city.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = city;
            }
        }

        return nearest;
    }

    public ICity GetNearestCity(IHex hex)
    {
        Hex concreteHex = (Hex)hex;
        City nearest = null;
        int minDistance = int.MaxValue;

        foreach (var city in cities.Values)
        {
            int distance = mapGenerator.GetDistance(concreteHex, city.centerHex);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = city;
            }
        }

        return nearest;
    }

    public List<ICity> GetCitiesInRange(IHex center, int range)
    {
        Hex centerHex = (Hex)center;
        List<ICity> citiesInRange = new List<ICity>();

        foreach (var city in cities.Values)
        {
            if (mapGenerator.GetDistance(centerHex, city.centerHex) <= range)
            {
                citiesInRange.Add(city);
            }
        }

        return citiesInRange;
    }

    public void AutoManageAllCities()
    {
        foreach (var city in cities.Values)
        {
            city.OptimizeHexWork();
        }
    }

    public Dictionary<string, object> GetCivilizationStats()
    {
        var stats = new Dictionary<string, object>();
        Yields totalYields = GetTotalYields();

        stats["cityCount"] = GetCityCount();
        stats["totalPopulation"] = GetTotalPopulation();
        stats["totalFood"] = totalYields.food;
        stats["totalProduction"] = totalYields.production;
        stats["totalGold"] = totalYields.gold;
        stats["totalScience"] = totalYields.science;
        stats["totalCulture"] = totalYields.culture;
        stats["totalFaith"] = totalYields.faith;

        return stats;
    }

    public IHex FindBestCityLocation(IHex searchCenter, int searchRange)
    {
        Hex centerHex = (Hex)searchCenter;
        List<Hex> candidates = new List<Hex>();

        for (int q = centerHex.Q - searchRange; q <= centerHex.Q + searchRange; q++)
        {
            for (int r = centerHex.R - searchRange; r <= centerHex.R + searchRange; r++)
            {
                if (mapGenerator.GetDistance(centerHex, new Hex { q = q, r = r }) <= searchRange)
                {
                    Vector2Int coords = new Vector2Int(q, r);
                    if (mapGenerator.hexes.TryGetValue(coords, out Hex hex))
                    {
                        if (CanFoundCityAt(hex))
                        {
                            candidates.Add(hex);
                        }
                    }
                }
            }
        }

        Hex bestLocation = null;
        float bestScore = -1f;

        foreach (var candidate in candidates)
        {
            float score = ScoreCityLocation(candidate);
            if (score > bestScore)
            {
                bestScore = score;
                bestLocation = candidate;
            }
        }

        return bestLocation;
    }

    private bool CanFoundCityAt(Hex hex)
    {
        if (hex.IsObstacle) return false;

        foreach (var city in cities.Values)
        {
            if (mapGenerator.GetDistance(hex, city.centerHex) < 4)
                return false;
        }

        return true;
    }

    private float ScoreCityLocation(Hex location)
    {
        float score = 0f;

        List<Hex> neighbors = mapGenerator.GetNeighbors(location);

        foreach (var neighbor in neighbors)
        {
            Yields yields = neighbor.GetTotalYields();
            score += yields.food * 2f;
            score += yields.production * 1.5f;
            score += yields.gold * 1f;
        }

        if (location.Terrain == TerrainType.Coast)
            score += 10f;

        foreach (var neighbor in neighbors)
        {
            if (neighbor.Resource == ResourceType.Luxury)
                score += 5f;
        }

        return score;
    }
}