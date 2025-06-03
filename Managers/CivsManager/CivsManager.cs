using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CivsManager : MonoBehaviour, ICivsManager
{
    [Header("Civilization Settings")]
    public GameObject civManagerPrefab;
    public int numberOfPlayers = 2;

    [Header("Civilization Names")]
    public List<string> civilizationNames = new List<string> { "Rome", "Egypt", "Greece", "China", "Persia", "Japan", "Germany", "France" };
    public List<string> leaderNames = new List<string> { "Caesar", "Cleopatra", "Alexander", "Qin Shi Huang", "Cyrus", "Oda Nobunaga", "Bismarck", "Napoleon" };

    [Header("Starting Units")]
    public bool giveStartingUnits = true;
    public int minDistanceBetweenStarts = 15;

    private List<ICivilization> civilizations = new List<ICivilization>();
    private Dictionary<string, Dictionary<string, CivRelation>> relations = new Dictionary<string, Dictionary<string, CivRelation>>();
    private IMapManager mapManager;
    private int civIdCounter = 0;
    private List<Vector2Int> usedStartPositions = new List<Vector2Int>();

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

        CreatePlayerCivilizations();
        InitializeRelations();

        Debug.Log($"CivsManager initialized with {CivCount} civilizations");
    }

    void CreatePlayerCivilizations()
    {
        int actualPlayers = Mathf.Min(numberOfPlayers, civilizationNames.Count);

        for (int i = 0; i < actualPlayers; i++)
        {
            string civName = civilizationNames[i];
            string leaderName = i < leaderNames.Count ? leaderNames[i] : $"Leader {i + 1}";

            // All players are human-controlled
            CreateCivilization(civName, leaderName, true);
        }
    }

    public ICivilization CreateCivilization(string civName, string leaderName, bool isHuman = false)
    {
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

        // Create starting units for all players
        if (giveStartingUnits)
        {
            CreateStartingUnits(civilization);
        }

        OnCivilizationCreated?.Invoke(civilization);
        Debug.Log($"Created civilization: {civName} (Leader: {leaderName}, Human: {isHuman})");

        return civilization;
    }

    void CreateStartingUnits(ICivilization civilization)
    {
        // Find a valid starting position
        var startPos = FindValidStartingPosition();
        if (startPos == null)
        {
            Debug.LogError($"Could not find valid starting position for {civilization.CivName}!");
            return;
        }

        usedStartPositions.Add(startPos.Value);

        // Get the hex at starting position
        var startHex = mapManager.GetHex(startPos.Value);
        if (startHex == null)
        {
            Debug.LogError($"No hex found at position {startPos.Value}!");
            return;
        }

        // Create settler at starting position
        var unitsManager = civilization.CivManager?.UnitsManager;
        if (unitsManager != null)
        {
            var settler = unitsManager.CreateUnit(UnitType.Settler, startHex);
            if (settler != null)
            {
                Debug.Log($"Created Settler for {civilization.CivName} at ({startHex.Q}, {startHex.R})");
            }

            // Find a neighboring hex for the warrior
            var neighbors = mapManager.GetNeighbors(startHex);
            Hex warriorHex = null;

            foreach (var neighbor in neighbors)
            {
                if (!neighbor.IsObstacle && neighbor.Terrain != TerrainType.Mountain && neighbor.Terrain != TerrainType.Ocean)
                {
                    warriorHex = neighbor;
                    break;
                }
            }

            if (warriorHex != null)
            {
                var warrior = unitsManager.CreateUnit(UnitType.Warrior, warriorHex);
                if (warrior != null)
                {
                    Debug.Log($"Created Warrior for {civilization.CivName} at ({warriorHex.Q}, {warriorHex.R})");
                }
            }
            else
            {
                Debug.LogWarning($"Could not find valid neighbor hex for warrior placement for {civilization.CivName}");
            }
        }
    }

    Vector2Int? FindValidStartingPosition()
    {
        var allHexes = mapManager.GetAllHexes();
        List<Vector2Int> validPositions = new List<Vector2Int>();

        // Find all valid starting positions
        foreach (var kvp in allHexes)
        {
            var hex = kvp.Value;

            // Check if hex is valid for starting
            if (hex.IsObstacle || hex.Terrain == TerrainType.Mountain || hex.Terrain == TerrainType.Ocean)
                continue;

            // Check if it's far enough from other starts
            bool tooClose = false;
            foreach (var usedPos in usedStartPositions)
            {
                var usedHex = mapManager.GetHex(usedPos);
                if (usedHex != null && mapManager.GetDistance(hex, usedHex) < minDistanceBetweenStarts)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                // Check if it has at least one valid neighbor for warrior
                var neighbors = mapManager.GetNeighbors(hex);
                bool hasValidNeighbor = neighbors.Any(n => !n.IsObstacle && n.Terrain != TerrainType.Mountain && n.Terrain != TerrainType.Ocean);

                if (hasValidNeighbor)
                {
                    validPositions.Add(kvp.Key);
                }
            }
        }

        if (validPositions.Count == 0)
        {
            Debug.LogWarning("No valid starting positions found!");
            return null;
        }

        // Choose a random valid position
        int randomIndex = Random.Range(0, validPositions.Count);
        return validPositions[randomIndex];
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