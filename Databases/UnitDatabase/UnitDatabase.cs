using System.Collections.Generic;
using System.Linq;

public static class UnitDatabase
{
    private static Dictionary<string, UnitData> units = new Dictionary<string, UnitData>();

    static UnitDatabase()
    {
        InitializeUnits();
    }

    static void InitializeUnits()


    // UNCOMMENT WHEN ICON ARE AVAILABLE
    {
        // Ancient Era Units
        var warrior = new UnitData("warrior", "Warrior", 40, UnitType.Warrior, UnitCategory.Combat, 6, 6, 100, 2);

        //var archer = new UnitData("archer", "Archer", 40, UnitType.Archer, UnitCategory.Combat, 5, 5, 100, 2)
        //    .AddRequiredTech("archery");

        //var spearman = new UnitData("spearman", "Spearman", 56, UnitType.Spearman, UnitCategory.Combat, 7, 10, 100, 2)
        //    .AddRequiredTech("bronze_working");

        //var scout = new UnitData("scout", "Scout", 25, UnitType.Scout, UnitCategory.Combat, 4, 4, 100, 3);

        var settler = new UnitData("settler", "Settler", 106, UnitType.Settler, UnitCategory.Civilian, 0, 0, 100, 2);

        //var worker = new UnitData("worker", "Worker", 70, UnitType.Worker, UnitCategory.Civilian, 0, 0, 100, 2);

        // Classical Era Units
        // var swordsman = new UnitData("swordsman", "Swordsman", 75, UnitType.Swordsman, UnitCategory.Combat, 11, 11, 100, 2)
        //     .AddRequiredTech("iron_working");

        // var horseman = new UnitData("horseman", "Horseman", 75, UnitType.Horseman, UnitCategory.Combat, 12, 11, 100, 4)
        //     .AddRequiredTech("horseback_riding");

        // Add all units to database
        units["warrior"] = warrior;
        //units["archer"] = archer;
        //units["spearman"] = spearman;
        //units["scout"] = scout;
        units["settler"] = settler;
        //units["worker"] = worker;
        // units["swordsman"] = swordsman;
        // units["horseman"] = horseman;
    }

    public static UnitData GetUnit(string id)
    {
        return units.GetValueOrDefault(id);
    }

    public static UnitData GetUnitByType(UnitType unitType)
    {
        return units.Values.FirstOrDefault(u => u.UnitType == unitType);
    }

    public static Dictionary<string, UnitData> GetAllUnits()
    {
        return units.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public static List<UnitData> GetUnitsByCategory(UnitCategory category)
    {
        return units.Values.Where(u => u.UnitCategory == category).ToList();
    }

    public static List<UnitData> GetCombatUnits()
    {
        return GetUnitsByCategory(UnitCategory.Combat);
    }

    public static List<UnitData> GetCivilianUnits()
    {
        return GetUnitsByCategory(UnitCategory.Civilian);
    }

    public static bool HasUnit(string id)
    {
        return units.ContainsKey(id);
    }

    public static bool HasUnitType(UnitType unitType)
    {
        return units.Values.Any(u => u.UnitType == unitType);
    }
}