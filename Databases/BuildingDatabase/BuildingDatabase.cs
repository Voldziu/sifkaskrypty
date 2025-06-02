using System.Collections.Generic;
using System.Linq;

public static class BuildingDatabase
{
    private static Dictionary<string, Building> buildings = new Dictionary<string, Building>();

    static BuildingDatabase()
    {
        InitializeBuildings();
    }

    static void InitializeBuildings()
    {
        var monument = new Building("monument", "Monument", 40, new Yields(culture: 2));
        var granary = new Building("granary", "Granary", 60, new Yields(food: 2));
        var library = new Building("library", "Library", 75, new Yields(science: 1), 2);
        var market = new Building("market", "Market", 100, new Yields(gold: 1), 1);
        var barracks = new Building("barracks", "Barracks", 75, new Yields());
        var temple = new Building("temple", "Temple", 100, new Yields(faith: 2), 1);
        var workshop = new Building("workshop", "Workshop", 100, new Yields(production: 2), 2);
        var university = new Building("university", "University", 160, new Yields(science: 2), 2);

        // Set up prerequisites
        university.prerequisites.Add("library");

        // Add all buildings to database
        buildings["monument"] = monument;
        buildings["granary"] = granary;
        buildings["library"] = library;
        buildings["market"] = market;
        buildings["barracks"] = barracks;
        buildings["temple"] = temple;
        buildings["workshop"] = workshop;
        buildings["university"] = university;
    }

    public static IBuilding GetBuilding(string id)
    {
        return buildings.GetValueOrDefault(id);
    }

    public static Dictionary<string, IBuilding> GetAllBuildings()
    {
        return buildings.ToDictionary(kvp => kvp.Key, kvp => (IBuilding)kvp.Value);
    }

    public static bool HasBuilding(string id)
    {
        return buildings.ContainsKey(id);
    }
}