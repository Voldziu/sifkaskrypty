using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]


public class TechManager : MonoBehaviour, ITechManager
{
    [Header("Research Settings")]
    public float researchSpeedMultiplier = 1f;

    [Header("Technology Database")]
    public TechPrefabData[] technologyPrefabs;

    private List<string> researchedTechs = new List<string>();
    private ITechnology currentResearch = null;
    private int scienceAccumulated = 0;
    private ICivilization civilization;
    private Dictionary<string, TechPrefabData> techDatabase = new Dictionary<string, TechPrefabData>();

    // Properties
    public List<string> ResearchedTechs => researchedTechs;
    public ITechnology CurrentResearch => currentResearch;
    public int ScienceAccumulated => scienceAccumulated;

    // Events
    public event System.Action<ITechnology> OnTechnologyResearched;
    public event System.Action<ITechnology> OnResearchStarted;

    public void Initialize(ICivilization civilization)
    {
        this.civilization = civilization;

        // Build tech database from prefabs
        BuildTechDatabase();

        // Start with basic technologies
        AddStartingTechnologies();

        Debug.Log($"TechManager initialized for {civilization.CivName} with {techDatabase.Count} technologies");
    }

    void BuildTechDatabase()
    {
        techDatabase.Clear();

        if (technologyPrefabs != null)
        {
            foreach (var techPrefab in technologyPrefabs)
            {
                if (!string.IsNullOrEmpty(techPrefab.techId))
                {
                    techDatabase[techPrefab.techId] = techPrefab;
                }
            }
        }

        // Add fallback technologies if no prefabs provided
        if (techDatabase.Count == 0)
        {
            AddFallbackTechnologies();
        }
    }

    void AddFallbackTechnologies()
    {
        // Basic starting technologies
        var agriculture = new TechPrefabData
        {
            techId = "agriculture",
            techName = "Agriculture",
            description = "Allows farms and granaries",
            era = TechEra.Ancient,
            scienceCost = 25,
            prerequisites = new List<string>(),
            unlocksBuildings = new List<string> { "granary", "farm" },
            unlocksUnits = new List<string>()
        };

        var pottery = new TechPrefabData
        {
            techId = "pottery",
            techName = "Pottery",
            description = "Enables pottery and monuments",
            era = TechEra.Ancient,
            scienceCost = 25,
            prerequisites = new List<string>(),
            unlocksBuildings = new List<string> { "monument" },
            unlocksUnits = new List<string>()
        };

        techDatabase["agriculture"] = agriculture;
        techDatabase["pottery"] = pottery;
    }

    void AddStartingTechnologies()
    {
        // Every civilization starts with these basic techs
        if (techDatabase.ContainsKey("agriculture"))
            researchedTechs.Add("agriculture");
        if (techDatabase.ContainsKey("pottery"))
            researchedTechs.Add("pottery");

        Debug.Log($"{civilization.CivName} starts with {researchedTechs.Count} technologies");
    }

    public bool StartResearch(string techId)
    {
        if (!CanResearch(techId))
        {
            Debug.LogWarning($"Cannot research {techId} - prerequisites not met or already researched");
            return false;
        }

        var techData = GetTechPrefabData(techId);
        if (techData == null)
        {
            Debug.LogError($"Technology {techId} not found in database");
            return false;
        }

        // Convert TechPrefabData to ITechnology
        currentResearch = new Technology(techData.techId, techData.techName, techData.description, techData.era, techData.scienceCost);
        currentResearch.Prerequisites.AddRange(techData.prerequisites);
        currentResearch.UnlocksBuildings.AddRange(techData.unlocksBuildings);
        currentResearch.UnlocksUnits.AddRange(techData.unlocksUnits);

        scienceAccumulated = 0;

        OnResearchStarted?.Invoke(currentResearch);
        Debug.Log($"{civilization.CivName} started researching {currentResearch.TechName}");

        return true;
    }

    TechPrefabData GetTechPrefabData(string techId)
    {
        return techDatabase.GetValueOrDefault(techId);
    }

    public bool CanResearch(string techId)
    {
        if (HasTechnology(techId)) return false;

        var techData = GetTechPrefabData(techId);
        if (techData == null) return false;

        // Check prerequisites
        foreach (var prereq in techData.prerequisites)
        {
            if (!HasTechnology(prereq))
            {
                return false;
            }
        }

        return true;
    }

    public List<ITechnology> GetAvailableTechs()
    {
        var available = new List<ITechnology>();

        foreach (var techData in techDatabase.Values)
        {
            if (CanResearch(techData.techId))
            {
                var tech = new Technology(techData.techId, techData.techName, techData.description, techData.era, techData.scienceCost);
                tech.Prerequisites.AddRange(techData.prerequisites);
                tech.UnlocksBuildings.AddRange(techData.unlocksBuildings);
                tech.UnlocksUnits.AddRange(techData.unlocksUnits);
                available.Add(tech);
            }
        }

        return available;
    }

    public bool HasTechnology(string techId)
    {
        return researchedTechs.Contains(techId);
    }

    public void ProcessTurn(int sciencePoints)
    {
        if (currentResearch == null)
        {
            if (sciencePoints > 0)
            {
                AutoSelectResearch();
            }
            return;
        }

        int adjustedScience = Mathf.FloorToInt(sciencePoints * researchSpeedMultiplier);
        scienceAccumulated += adjustedScience;

        Debug.Log($"{civilization.CivName} research progress: {scienceAccumulated}/{currentResearch.ScienceCost} on {currentResearch.TechName}");

        if (scienceAccumulated >= currentResearch.ScienceCost)
        {
            CompleteTechnology(currentResearch);
        }
    }

    void CompleteTechnology(ITechnology tech)
    {
        researchedTechs.Add(tech.TechId);

        int overflow = scienceAccumulated - tech.ScienceCost;
        currentResearch = null;
        scienceAccumulated = 0;

        OnTechnologyResearched?.Invoke(tech);
        Debug.Log($"{civilization.CivName} completed research: {tech.TechName}!");

        if (tech.UnlocksBuildings.Count > 0)
        {
            Debug.Log($"  Unlocked buildings: {string.Join(", ", tech.UnlocksBuildings)}");
        }
        if (tech.UnlocksUnits.Count > 0)
        {
            Debug.Log($"  Unlocked units: {string.Join(", ", tech.UnlocksUnits)}");
        }

        if (overflow > 0)
        {
            AutoSelectResearch();
            if (currentResearch != null)
            {
                scienceAccumulated = overflow;
            }
        }
    }

    void AutoSelectResearch()
    {
        var availableTechs = GetAvailableTechs();
        if (availableTechs.Count > 0)
        {
            var cheapestTech = availableTechs.OrderBy(t => t.ScienceCost).First();
            StartResearch(cheapestTech.TechId);
        }
    }

    public bool CanBuildBuilding(string buildingId)
    {
        foreach (var techData in techDatabase.Values)
        {
            if (techData.unlocksBuildings.Contains(buildingId))
            {
                return HasTechnology(techData.techId);
            }
        }

        return true; // If no tech requirement, can build
    }

    public bool CanBuildUnit(string unitId)
    {
        foreach (var techData in techDatabase.Values)
        {
            if (techData.unlocksUnits.Contains(unitId))
            {
                return HasTechnology(techData.techId);
            }
        }

        return true; // If no tech requirement, can build
    }

    public List<ITechnology> GetTechsByEra(TechEra era)
    {
        var techsInEra = new List<ITechnology>();

        foreach (var techData in techDatabase.Values)
        {
            if (techData.era == era)
            {
                var tech = new Technology(techData.techId, techData.techName, techData.description, techData.era, techData.scienceCost);
                tech.Prerequisites.AddRange(techData.prerequisites);
                tech.UnlocksBuildings.AddRange(techData.unlocksBuildings);
                tech.UnlocksUnits.AddRange(techData.unlocksUnits);
                techsInEra.Add(tech);
            }
        }

        return techsInEra;
    }

    // Utility methods
    public int GetResearchProgress()
    {
        if (currentResearch == null) return 0;
        return Mathf.FloorToInt((float)scienceAccumulated / currentResearch.ScienceCost * 100);
    }

    public int GetTurnsToComplete(int sciencePerTurn)
    {
        if (currentResearch == null || sciencePerTurn <= 0) return 0;

        int remainingScience = currentResearch.ScienceCost - scienceAccumulated;
        return Mathf.CeilToInt((float)remainingScience / sciencePerTurn);
    }

    public TechEra GetCurrentEra()
    {
        var eras = System.Enum.GetValues(typeof(TechEra)).Cast<TechEra>().OrderBy(e => e);

        foreach (var era in eras.Reverse())
        {
            var eratechs = GetTechsByEra(era);
            if (eratechs.Any(t => HasTechnology(t.TechId)))
            {
                return era;
            }
        }

        return TechEra.Ancient;
    }

    public string GetDebugInfo()
    {
        return $"Techs: {researchedTechs.Count}, Current: {currentResearch?.TechName ?? "None"}, Progress: {GetResearchProgress()}%";
    }
}