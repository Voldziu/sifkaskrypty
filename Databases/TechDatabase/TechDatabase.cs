using System.Collections.Generic;
using System.Linq;

public static class TechDatabase
{
    private static Dictionary<string, Technology> technologies = new Dictionary<string, Technology>();

    static TechDatabase()
    {
        InitializeTechnologies();
    }

    static void InitializeTechnologies()
    {
        // Ancient Era
        var agriculture = new Technology("agriculture", "Agriculture", "Allows farms and granaries", TechEra.Ancient, 25)
            .UnlockBuilding("granary")
            .UnlockBuilding("farm");

        var animalHusbandry = new Technology("animal_husbandry", "Animal Husbandry", "Enables pastures and horses", TechEra.Ancient, 35)
            .UnlockUnit("horseman")
            .UnlockBuilding("pasture");

        var pottery = new Technology("pottery", "Pottery", "Enables pottery and monuments", TechEra.Ancient, 25)
            .UnlockBuilding("monument");

        var mining = new Technology("mining", "Mining", "Enables mines and access to metals", TechEra.Ancient, 35)
            .UnlockBuilding("mine");

        var sailing = new Technology("sailing", "Sailing", "Enables boats and coastal exploration", TechEra.Ancient, 50)
            .UnlockUnit("galley")
            .UnlockBuilding("lighthouse");

        var archery = new Technology("archery", "Archery", "Enables archers", TechEra.Ancient, 25)
            .UnlockUnit("archer");

        var bronzeWorking = new Technology("bronze_working", "Bronze Working", "Enables spearmen and barracks", TechEra.Ancient, 55)
            .AddPrerequisite("mining")
            .UnlockUnit("spearman")
            .UnlockBuilding("barracks");

        var writing = new Technology("writing", "Writing", "Enables libraries and scientific progress", TechEra.Ancient, 55)
            .AddPrerequisite("pottery")
            .UnlockBuilding("library");

        // Classical Era
        var ironWorking = new Technology("iron_working", "Iron Working", "Enables swordsmen and iron weapons", TechEra.Classical, 105)
            .AddPrerequisite("bronze_working")
            .UnlockUnit("swordsman");

        var mathematics = new Technology("mathematics", "Mathematics", "Enables advanced construction", TechEra.Classical, 105)
            .AddPrerequisite("writing")
            .UnlockBuilding("courthouse");

        var philosophy = new Technology("philosophy", "Philosophy", "Enables temples and faith", TechEra.Classical, 105)
            .AddPrerequisite("writing")
            .UnlockBuilding("temple");

        var currency = new Technology("currency", "Currency", "Enables markets and trade", TechEra.Classical, 105)
            .AddPrerequisite("bronze_working")
            .UnlockBuilding("market");

        // Medieval Era
        var engineering = new Technology("engineering", "Engineering", "Enables aqueducts and roads", TechEra.Medieval, 175)
            .AddPrerequisite("mathematics")
            .AddPrerequisite("iron_working")
            .UnlockBuilding("aqueduct");

        var machinery = new Technology("machinery", "Machinery", "Enables crossbowmen and workshops", TechEra.Medieval, 175)
            .AddPrerequisite("engineering")
            .UnlockUnit("crossbowman")
            .UnlockBuilding("workshop");

        // Add all technologies to database
        technologies["agriculture"] = agriculture;
        technologies["animal_husbandry"] = animalHusbandry;
        technologies["pottery"] = pottery;
        technologies["mining"] = mining;
        technologies["sailing"] = sailing;
        technologies["archery"] = archery;
        technologies["bronze_working"] = bronzeWorking;
        technologies["writing"] = writing;
        technologies["iron_working"] = ironWorking;
        technologies["mathematics"] = mathematics;
        technologies["philosophy"] = philosophy;
        technologies["currency"] = currency;
        technologies["engineering"] = engineering;
        technologies["machinery"] = machinery;
    }

    public static ITechnology GetTechnology(string techId)
    {
        return technologies.GetValueOrDefault(techId);
    }

    public static List<ITechnology> GetAllTechnologies()
    {
        return technologies.Values.Cast<ITechnology>().ToList();
    }

    public static List<ITechnology> GetTechnologiesByEra(TechEra era)
    {
        return technologies.Values.Where(t => t.Era == era).Cast<ITechnology>().ToList();
    }

    public static bool HasTechnology(string techId)
    {
        return technologies.ContainsKey(techId);
    }
}