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
    {
        // Ancient Era Units - Combat
        var warrior = new UnitData("warrior", "Warrior", 1, UnitType.Warrior, UnitCategory.Combat, 6, 6, 100, 2);

        var archer = new UnitData("archer", "Archer", 40, UnitType.Archer, UnitCategory.Combat, 5, 5, 100, 2)
            .AddRequiredTech("archery");

        var spearman = new UnitData("spearman", "Spearman", 56, UnitType.Spearman, UnitCategory.Combat, 7, 10, 100, 2)
            .AddRequiredTech("bronze_working");

        var scout = new UnitData("scout", "Scout", 1, UnitType.Scout, UnitCategory.Combat, 4, 4, 100, 3);

        // Ancient Era Units - Civilian  
        var settler = new UnitData("settler", "Settler", 1, UnitType.Settler, UnitCategory.Civilian, 0, 0, 100, 2);

        var worker = new UnitData("worker", "Worker", 70, UnitType.Worker, UnitCategory.Civilian, 0, 0, 100, 2);

        // Classical Era Units
        var swordsman = new UnitData("swordsman", "Swordsman", 75, UnitType.Swordsman, UnitCategory.Combat, 11, 11, 100, 2)
            .AddRequiredTech("iron_working");

        var horseman = new UnitData("horseman", "Horseman", 75, UnitType.Horseman, UnitCategory.Combat, 12, 11, 100, 4)
            .AddRequiredTech("horseback_riding");

        // Add all units to database
        units["warrior"] = warrior;
        units["archer"] = (UnitData)archer;
        units["spearman"] = (UnitData)spearman;
        units["scout"] = scout;
        units["settler"] = settler;
        units["worker"] = worker;
        units["swordsman"] = (UnitData)swordsman;
        units["horseman"] = (UnitData)horseman;
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

    public static List<UnitData> GetMeleeUnits()
    {
        return GetCombatUnits().Where(u => GetCombatType(u.UnitType) == CombatType.Melee).ToList();
    }

    public static List<UnitData> GetRangedUnits()
    {
        return GetCombatUnits().Where(u => GetCombatType(u.UnitType) == CombatType.Ranged).ToList();
    }

    public static CombatType GetCombatType(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Archer:
                return CombatType.Ranged;
            case UnitType.Warrior:
            case UnitType.Spearman:
            case UnitType.Swordsman:
            case UnitType.Horseman:
            case UnitType.Scout:
                return CombatType.Melee;
            default:
                return CombatType.Melee; // Default for civilian units
        }
    }

    public static int GetAttackRange(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Archer:
                return 2;
            case UnitType.Warrior:
            case UnitType.Spearman:
            case UnitType.Swordsman:
            case UnitType.Horseman:
            case UnitType.Scout:
                return 1;
            default:
                return 0; // Civilian units can't attack
        }
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