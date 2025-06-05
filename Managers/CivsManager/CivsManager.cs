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
    public int minDistanceBetweenStarts = 10;

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

        // Verify map is ready
        if (!ValidateMapReady())
        {
            Debug.LogError("Map is not ready for civilization placement!");
            return;
        }

        CreatePlayerCivilizations();
        InitializeRelations();

        Debug.Log($"CivsManager initialized with {CivCount} civilizations");
    }

    bool ValidateMapReady()
    {
        if (mapManager == null)
        {
            Debug.LogError("MapManager is null!");
            return false;
        }

        if (mapManager.HexMap == null)
        {
            Debug.LogError("HexMap is null!");
            return false;
        }

        var allHexes = mapManager.GetAllHexes();
        if (allHexes == null || allHexes.Count == 0)
        {
            Debug.LogError($"No hexes available! GetAllHexes returned {allHexes?.Count ?? 0} hexes");

            // Try direct access to hexes
            if (mapManager.HexMap.hexes != null)
            {
                Debug.LogError($"Direct hex access shows {mapManager.HexMap.hexes.Count} hexes");
            }

            return false;
        }

        Debug.Log($"Map validation successful: {allHexes.Count} hexes available");
        return true;
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
            var settler = unitsManager.CreateUnit(UnitCategory.Civilian,UnitType.Settler, startHex);
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
                var warrior = unitsManager.CreateUnit(UnitCategory.Combat,UnitType.Warrior, warriorHex);
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

        if (allHexes == null || allHexes.Count == 0)
        {
            Debug.LogError("GetAllHexes returned null or empty!");

            // Try to force map generation
            if (mapManager.HexMap != null)
            {
                Debug.LogWarning("Attempting to access hexes directly...");
                // This might trigger hex generation if it's lazy-loaded
                var directHexes = mapManager.HexMap.hexes;
                if (directHexes != null && directHexes.Count > 0)
                {
                    Debug.Log($"Direct access found {directHexes.Count} hexes");
                    allHexes = directHexes;
                }
            }

            if (allHexes == null || allHexes.Count == 0)
            {
                return null;
            }
        }

        List<Vector2Int> validPositions = new List<Vector2Int>();

        Debug.Log($"Searching for starting positions. Total hexes: {allHexes.Count}");
        Debug.Log($"Used start positions: {usedStartPositions.Count}, Min distance: {minDistanceBetweenStarts}");

        // Reduce minimum distance based on map size
        int mapSize = Mathf.RoundToInt(Mathf.Sqrt(allHexes.Count));
        int adjustedMinDistance = Mathf.Min(minDistanceBetweenStarts, mapSize / 3);

        Debug.Log($"Map appears to be roughly {mapSize}x{mapSize}, using min distance: {adjustedMinDistance}");

        int obstacleCount = 0;
        int terrainFilteredCount = 0;
        int tooCloseCount = 0;
        int noValidNeighborCount = 0;

        // Find all valid starting positions
        foreach (var kvp in allHexes)
        {
            var hex = kvp.Value;

            // Check if hex is valid for starting - be less restrictive
            if (hex.IsObstacle)
            {
                obstacleCount++;
                continue;
            }

            // Only exclude truly unlivable terrain
            if (hex.Terrain == TerrainType.Mountain || hex.Terrain == TerrainType.Ocean)
            {
                terrainFilteredCount++;
                continue;
            }

            // Check distance to other starts
            bool tooClose = false;
            foreach (var usedPos in usedStartPositions)
            {
                var usedHex = mapManager.GetHex(usedPos);
                if (usedHex != null && mapManager.GetDistance(hex, usedHex) < adjustedMinDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
            {
                tooCloseCount++;
                continue;
            }

            // Check for valid neighbors - be more lenient
            var neighbors = mapManager.GetNeighbors(hex);
            bool hasValidNeighbor = neighbors.Count > 0 && neighbors.Any(n => !n.IsObstacle);

            if (!hasValidNeighbor)
            {
                noValidNeighborCount++;
                continue;
            }

            validPositions.Add(kvp.Key);
            Debug.Log($"Valid position found at ({kvp.Key.x}, {kvp.Key.y}) - Terrain: {hex.Terrain}");
        }

        Debug.Log($"Position filtering results:");
        Debug.Log($"  Obstacles: {obstacleCount}");
        Debug.Log($"  Bad terrain: {terrainFilteredCount}");
        Debug.Log($"  Too close: {tooCloseCount}");
        Debug.Log($"  No valid neighbors: {noValidNeighborCount}");
        Debug.Log($"  Valid positions: {validPositions.Count}");

        if (validPositions.Count == 0)
        {
            Debug.LogError("No valid starting positions found! Trying emergency fallback...");
            return FindEmergencyStartingPosition();
        }

        // Choose a random valid position
        int randomIndex = Random.Range(0, validPositions.Count);
        var chosenPos = validPositions[randomIndex];
        Debug.Log($"Chosen starting position: ({chosenPos.x}, {chosenPos.y})");
        return chosenPos;
    }

    // Add this emergency fallback method:
    Vector2Int? FindEmergencyStartingPosition()
    {
        var allHexes = mapManager.GetAllHexes();

        Debug.Log("Emergency fallback: looking for ANY non-obstacle hex...");

        // Just find ANY non-obstacle hex
        foreach (var kvp in allHexes)
        {
            var hex = kvp.Value;
            if (!hex.IsObstacle)
            {
                Debug.LogWarning($"Emergency position selected at ({kvp.Key.x}, {kvp.Key.y}) - Terrain: {hex.Terrain}");
                return kvp.Key;
            }
        }

        // Absolutely last resort: pick the first hex period
        if (allHexes.Count > 0)
        {
            var firstHex = allHexes.First();
            Debug.LogError($"DESPERATE FALLBACK: Using first hex at ({firstHex.Key.x}, {firstHex.Key.y})");
            return firstHex.Key;
        }

        Debug.LogError("Could not find ANY hex at all!");
        return null;
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