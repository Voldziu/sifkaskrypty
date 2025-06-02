using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CivsManager : MonoBehaviour, ICivsManager
{
    [Header("Civilization Settings")]
    public GameObject civManagerPrefab;
    public int maxCivilizations = 8;

    [Header("Starting Setup")]
    public bool createTestCivs = true;
    public string[] testCivNames = { "Rome", "Egypt", "Greece", "China" };

    private List<ICivilization> civilizations = new List<ICivilization>();
    private Dictionary<string, Dictionary<string, CivRelation>> relations = new Dictionary<string, Dictionary<string, CivRelation>>();
    private IMapManager mapManager;
    private int civIdCounter = 0;

    // Properties
    public List<ICivilization> Civilizations => civilizations;
    public int CivCount => civilizations.Count;

    // Events
    public event System.Action<ICivilization> OnCivilizationCreated;
    public event System.Action<ICivilization> OnCivilizationDestroyed;
    public event System.Action<ICivilization, ICivilization, CivRelation> OnRelationChanged;

    public void Initialize(IMapManager mapManager)
    {
        this.mapManager = mapManager;

        Debug.Log("=== INITIALIZING CIVILIZATIONS MANAGER ===");

        if (createTestCivs)
        {
            CreateTestCivilizations();
        }

        InitializeRelations();

        Debug.Log($"CivsManager initialized with {CivCount} civilizations");
    }

    void CreateTestCivilizations()
    {
        // Create human player first
        var humanCiv = CreateCivilization(testCivNames[0], "Player", true);

        // Create AI civilizations
        for (int i = 1; i < testCivNames.Length && i < maxCivilizations; i++)
        {
            CreateCivilization(testCivNames[i], $"AI Leader {i}", false);
        }
    }

    public ICivilization CreateCivilization(string civName, string leaderName, bool isHuman = false)
    {
        if (CivCount >= maxCivilizations)
        {
            Debug.LogWarning($"Cannot create civilization - maximum of {maxCivilizations} reached");
            return null;
        }

        string civId = $"civ_{++civIdCounter}";

        // Create CivManager GameObject
        GameObject civGO = null;
        if (civManagerPrefab != null)
        {
            civGO = Instantiate(civManagerPrefab, transform);
            civGO.name = $"CivManager_{civName}";
        }
        else
        {
            civGO = new GameObject($"CivManager_{civName}");
            civGO.transform.SetParent(transform);
        }

        // Create and setup Civilization
        var civilization = new Civilization(civId, civName, leaderName, isHuman);
        var civManager = civGO.GetComponent<CivManager>();
        if (civManager == null)
        {
            civManager = civGO.AddComponent<CivManager>();
        }

        // Initialize the civilization
        civManager.Initialize(civilization, mapManager, this);
        civilization.SetCivManager(civManager);

        civilizations.Add(civilization);

        // Initialize relations for new civ
        InitializeRelationsForCiv(civilization);

        OnCivilizationCreated?.Invoke(civilization);
        Debug.Log($"Created civilization: {civName} (Leader: {leaderName}, Human: {isHuman})");

        return civilization;
    }

    void InitializeRelations()
    {
        foreach (var civ in civilizations)
        {
            InitializeRelationsForCiv(civ);
        }
    }

    void InitializeRelationsForCiv(ICivilization civ)
    {
        if (!relations.ContainsKey(civ.CivId))
        {
            relations[civ.CivId] = new Dictionary<string, CivRelation>();
        }

        // Set neutral relations with all other civs
        foreach (var otherCiv in civilizations)
        {
            if (otherCiv.CivId != civ.CivId)
            {
                SetRelation(civ, otherCiv, CivRelation.Neutral);
            }
        }
    }

    public ICivilization GetCivilization(string civId)
    {
        return civilizations.FirstOrDefault(c => c.CivId == civId);
    }

    public ICivilization GetCivilizationByName(string civName)
    {
        return civilizations.FirstOrDefault(c => c.CivName == civName);
    }

    public List<ICivilization> GetAliveCivilizations()
    {
        return civilizations.Where(c => c.IsAlive).ToList();
    }

    public int GetAliveCivCount()
    {
        return civilizations.Count(c => c.IsAlive);
    }

    public void ProcessTurn()
    {
        Debug.Log("Processing all civilizations...");

        var aliveCivs = GetAliveCivilizations();
        foreach (var civ in aliveCivs)
        {
            var civManager = civ.CivManager;
            civManager?.ProcessTurn();
        }

        // Process inter-civ events (diplomacy, trade, etc.)
        ProcessDiplomacy();

        Debug.Log($"Processed {aliveCivs.Count} civilizations");
    }

    void ProcessDiplomacy()
    {
        // Placeholder for diplomatic AI decisions
        // AI civs might change relations, propose trades, etc.
    }

    public CivRelation GetRelation(ICivilization civ1, ICivilization civ2)
    {
        if (civ1.CivId == civ2.CivId) return CivRelation.Neutral; // Same civ

        if (relations.ContainsKey(civ1.CivId) && relations[civ1.CivId].ContainsKey(civ2.CivId))
        {
            return relations[civ1.CivId][civ2.CivId];
        }

        return CivRelation.Neutral; // Default
    }

    public void SetRelation(ICivilization civ1, ICivilization civ2, CivRelation relation)
    {
        if (civ1.CivId == civ2.CivId) return; // Can't set relation with self

        // Ensure dictionaries exist
        if (!relations.ContainsKey(civ1.CivId))
            relations[civ1.CivId] = new Dictionary<string, CivRelation>();
        if (!relations.ContainsKey(civ2.CivId))
            relations[civ2.CivId] = new Dictionary<string, CivRelation>();

        // Set bidirectional relation
        relations[civ1.CivId][civ2.CivId] = relation;
        relations[civ2.CivId][civ1.CivId] = relation;

        OnRelationChanged?.Invoke(civ1, civ2, relation);
        Debug.Log($"Relation changed: {civ1.CivName} ↔ {civ2.CivName} = {relation}");
    }

    public bool AreAtWar(ICivilization civ1, ICivilization civ2)
    {
        return GetRelation(civ1, civ2) == CivRelation.War;
    }

    public bool AreAllied(ICivilization civ1, ICivilization civ2)
    {
        return GetRelation(civ1, civ2) == CivRelation.Allied;
    }

    public ICivilization CheckVictoryConditions()
    {
        var aliveCivs = GetAliveCivilizations();

        // Domination victory - only one civ left
        if (aliveCivs.Count == 1)
        {
            return aliveCivs[0];
        }

        // Science victory - first to research all techs (placeholder)
        // Culture victory - highest culture score (placeholder)
        // Other victory conditions can be added here

        return null; // No winner yet
    }

    public Dictionary<string, object> GetGameStats()
    {
        var stats = new Dictionary<string, object>();

        stats["totalCivilizations"] = CivCount;
        stats["aliveCivilizations"] = GetAliveCivCount();

        foreach (var civ in civilizations)
        {
            if (civ.IsAlive)
            {
                var civManager = civ.CivManager;
                if (civManager != null)
                {
                    stats[$"{civ.CivName}_cities"] = civManager.GetCityCount();
                    stats[$"{civ.CivName}_population"] = civManager.GetTotalPopulation();
                }
            }
        }

        return stats;
    }
}