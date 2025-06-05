using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitsManager : MonoBehaviour, IUnitsManager
{
    [Header("Unit Settings")]
    public UnitPrefabData[] unitPrefabs;
    public int maxUnits = 50;

    [Header("Unit Stacking")]
    public bool allowUnitStacking = true; // One combat + one civilian per hex
    public bool allowEnemyStacking = false; // Whether enemy units can stack with player units

    [Header("Movement Settings")]
    public bool useMovementCosts = true; // Use hex movement costs for pathfinding

    private List<IUnit> units = new List<IUnit>();
    private ICivilization civilization;
    private IMapManager mapManager;
    private int unitIdCounter = 0;

    // Properties
    public List<IUnit> Units => units;
    public int UnitCount => units.Count;
    public UnitPrefabData[] UnitPrefabs => unitPrefabs;

    // Events
    public event System.Action<IUnit> OnUnitCreated;
    public event System.Action<IUnit> OnUnitDestroyed;
    public event System.Action<IUnit, IHex> OnUnitMoved;

    public void Initialize(ICivilization civilization, IMapManager mapManager)
    {
        this.civilization = civilization;
        this.mapManager = mapManager;

        Debug.Log($"UnitsManager initialized for {civilization.CivName}");
    }

    public IUnit CreateUnit(UnitCategory unitCategory,UnitType unitType, IHex location)
    {
        if (UnitCount >= maxUnits)
        {
            Debug.LogWarning($"Cannot create unit - maximum of {maxUnits} reached");
            return null;
        }

        if (location == null || location.IsObstacle)
        {
            Debug.LogWarning("Cannot create unit on invalid location");
            return null;
        }

        // Check unit stacking rules
        if (!CanPlaceUnitAt(unitType, location))
        {
            Debug.LogWarning($"Cannot place {unitType} at ({location.Q}, {location.R}) - stacking rules violated");
            return null;
        }

        // Get unit prefab
        var unitPrefabData = GetUnitPrefabData(unitType);
        if (unitPrefabData?.prefab == null)
        {
            Debug.LogWarning($"No prefab found for unit type {unitType}");
            return null;
        }

        // Create unit GameObject
        string unitId = $"unit_{++unitIdCounter}";
        Vector3 worldPos = mapManager.HexMap.HexToWorld(location.Q, location.R);
        GameObject unitGO = Instantiate(unitPrefabData.prefab, worldPos, Quaternion.identity, transform);
        unitGO.name = $"{unitType}_{civilization.CivName}_{unitId}";

        // Setup Unit component
        Unit unitComponent = unitGO.GetComponent<Unit>();
        if (unitComponent == null)
        {
            unitComponent = unitGO.AddComponent<Unit>();
        }

        unitComponent.Initialize(unitId, unitType, (Hex)location, unitPrefabData.prefab);

        
       

        units.Add(unitComponent);
        OnUnitCreated?.Invoke(unitComponent);

        Debug.Log($"Created {unitType} for {civilization.CivName} at ({location.Q}, {location.R})");
        return unitComponent;
    }

    
        
    bool CanPlaceUnitAt(UnitType unitType, IHex location)
    {
        if (!allowUnitStacking)
        {
            // No stacking allowed - check if any unit is present
            return GetUnitsAt(location).Count == 0;
        }

        var unitsAtLocation = GetUnitsAt(location);
        var unitCategory = GetUnitCategory(unitType);

        // Check stacking rules
        foreach (var unit in unitsAtLocation)
        {
            // Same category conflict
            if (unit.UnitCategory == unitCategory)
            {
                // If it's the same civilization, don't allow
                if (IsOwnedByCurrentCiv(unit))
                {
                    return false;
                }

                // If it's an enemy unit and enemy stacking is not allowed
                if (!allowEnemyStacking)
                {
                    return false;
                }
            }
        }

        return true;
    }

    bool IsOwnedByCurrentCiv(IUnit unit)
    {
        // Check if unit belongs to current civilization
        // This is a simple check - you might want to add a civilization reference to units
        if (unit is Unit concreteUnit)
        {
            return concreteUnit.name.Contains(civilization.CivName);
        }
        return units.Contains(unit); // If it's in our units list, it's ours
    }

    UnitCategory GetUnitCategory(UnitType unitType)
    {
        switch (unitType)
        {
            case UnitType.Settler:
            case UnitType.Worker:
                return UnitCategory.Civilian;
            default:
                return UnitCategory.Combat;
        }
    }

    UnitPrefabData GetUnitPrefabData(UnitType unitType)
    {
        return unitPrefabs?.FirstOrDefault(up => up.unitType == unitType);
    }

    

   

    public IUnit GetUnit(string unitId)
    {
        return units.FirstOrDefault(u => u.UnitId == unitId);
    }

    public List<IUnit> GetUnitsAt(IHex hex)
    {
        return units.Where(u => u.CurrentHex == hex && u.IsAlive()).ToList();
    }

    public List<IUnit> GetCombatUnitsAt(IHex hex)
    {
        return GetUnitsAt(hex).Where(u => u.UnitCategory == UnitCategory.Combat).ToList();
    }

    public List<IUnit> GetCivilianUnitsAt(IHex hex)
    {
        return GetUnitsAt(hex).Where(u => u.UnitCategory == UnitCategory.Civilian).ToList();
    }

    // Enhanced method to get units by category with priority sorting
    public List<IUnit> GetUnitsAtSorted(IHex hex)
    {
        var unitsAtHex = GetUnitsAt(hex);

        // Sort: Combat units first, then civilians
        return unitsAtHex.OrderBy(u => u.UnitCategory == UnitCategory.Combat ? 0 : 1).ToList();
    }

    public bool RemoveUnit(string unitId)
    {
        var unit = GetUnit(unitId);
        if (unit != null)
        {
            units.Remove(unit);
            OnUnitDestroyed?.Invoke(unit);

            // Destroy the GameObject
            if (unit is Unit unitComponent)
            {
                Destroy(unitComponent.gameObject);
            }

            Debug.Log($"Removed unit {unit.UnitName}");
            return true;
        }
        return false;
    }

    public bool MoveUnit(IUnit unit, IHex destination)
    {
        if (unit == null || destination == null) return false;
        if (!unit.CanMoveTo(destination)) return false;

        // Check if unit has enough movement points
        if (unit.Movement <= 0 || unit.HasMoved)
        {
            Debug.LogWarning($"Unit {unit.UnitName} has no movement left");
            return false;
        }

        // Check stacking rules at destination
        if (!CanPlaceUnitAt(unit.UnitType, destination))
        {
            Debug.LogWarning($"Cannot move {unit.UnitName} - destination blocked by stacking rules");
            return false;
        }

        // Calculate movement cost
        int movementCost = CalculateMovementCost(unit.CurrentHex, destination);

        if (unit.Movement < movementCost)
        {
            Debug.LogWarning($"Unit {unit.UnitName} doesn't have enough movement ({unit.Movement} < {movementCost}) for terrain {destination.Terrain}");
            return false;
        }

        var oldHex = unit.CurrentHex;
        unit.MoveTo(destination);

        // Deduct movement cost
        unit.Movement -= movementCost;

       

        OnUnitMoved?.Invoke(unit, destination);
        Debug.Log($"Moved {unit.UnitName} from ({oldHex.Q}, {oldHex.R}) to ({destination.Q}, {destination.R}) " +
                  $"(Cost: {movementCost}, Remaining: {unit.Movement}/{unit.MaxMovement})");

        return true;
    }

    int CalculateMovementCost(IHex from, IHex to)
    {
        if (!useMovementCosts) return 1;

        // For adjacent hexes, use the destination hex's movement cost
        if (mapManager.GetDistance((Hex)from, (Hex)to) == 1)
        {
            int cost = to.MovementCost;
            Debug.Log($"Movement cost from ({from.Q},{from.R}) to ({to.Q},{to.R}): {cost} (terrain: {to.Terrain})");
            return cost;
        }

        // For non-adjacent moves, calculate path cost (shouldn't happen in normal gameplay)
        var path = mapManager.FindPath((Hex)from, (Hex)to);
        if (path != null && path.Count > 1)
        {
            int totalCost = 0;
            for (int i = 1; i < path.Count; i++) // Skip starting hex
            {
                totalCost += path[i].MovementCost;
            }
            Debug.Log($"Path movement cost from ({from.Q},{from.R}) to ({to.Q},{to.R}): {totalCost} over {path.Count - 1} hexes");
            return totalCost;
        }

        Debug.LogWarning($"Cannot calculate movement cost from ({from.Q},{from.R}) to ({to.Q},{to.R}) - invalid path");
        return 999; // Invalid move
    }

    public List<IHex> GetValidMoves(IUnit unit)
    {
        if (unit == null || unit.Movement <= 0 || unit.HasMoved)
            return new List<IHex>();

        var validMoves = new List<IHex>();

        if (useMovementCosts)
        {
            // Use movement-cost-aware pathfinding
            var reachableHexes = mapManager.GetReachableHexesWithMovement((Hex)unit.CurrentHex, unit.Movement);

            foreach (var hex in reachableHexes)
            {
                if (unit.CanMoveTo(hex) && CanPlaceUnitAt(unit.UnitType, hex))
                {
                    validMoves.Add(hex);
                }
            }

            Debug.Log($"Unit {unit.UnitName} can reach {validMoves.Count} hexes with {unit.Movement} movement points (using movement costs)");
        }
        else
        {
            // Simple range-based movement (ignoring movement costs)
            var reachableHexes = mapManager.GetReachableHexes((Hex)unit.CurrentHex, unit.Movement);

            foreach (var hex in reachableHexes)
            {
                if (unit.CanMoveTo(hex) && CanPlaceUnitAt(unit.UnitType, hex))
                {
                    validMoves.Add(hex);
                }
            }

            Debug.Log($"Unit {unit.UnitName} can reach {validMoves.Count} hexes with {unit.Movement} range (ignoring movement costs)");
        }

        return validMoves;
    }

    // Method to check if a specific move is valid
    public bool IsValidMove(IUnit unit, IHex destination)
    {
        if (unit == null || destination == null) return false;
        if (unit.Movement <= 0 || unit.HasMoved) return false;
        if (!unit.CanMoveTo(destination)) return false;
        if (!CanPlaceUnitAt(unit.UnitType, destination)) return false;

        int movementCost = CalculateMovementCost(unit.CurrentHex, destination);
        return unit.Movement >= movementCost;
    }

    // Enhanced method to get movement cost to a destination
    public int GetMovementCostTo(IUnit unit, IHex destination)
    {
        if (unit == null || destination == null) return 999;
        return CalculateMovementCost(unit.CurrentHex, destination);
    }

    public bool CanAttack(IUnit attacker, IUnit target)
    {
        if (attacker == null || target == null) return false;
        if (!attacker.IsCombatUnit()) return false;
        if (attacker.HasMoved) return false;
        if (IsOwnedByCurrentCiv(target)) return false; // Can't attack own units

        int distance = mapManager.GetDistance((Hex)attacker.CurrentHex, (Hex)target.CurrentHex);
        return distance <= 1; // Melee range
    }

    public void Attack(IUnit attacker, IUnit target)
    {
        if (!CanAttack(attacker, target)) return;

        int damage = CalculateDamage(attacker, target);
        target.TakeDamage(damage);

        if (target.IsCombatUnit())
        {
            int counterDamage = CalculateDamage(target, attacker) / 2;
            attacker.TakeDamage(counterDamage);
        }

        

        Debug.Log($"{attacker.UnitName} attacked {target.UnitName} for {damage} damage");

        if (!target.IsAlive())
        {
            RemoveUnit(target.UnitId);
        }
        if (!attacker.IsAlive())
        {
            RemoveUnit(attacker.UnitId);
        }
    }

    int CalculateDamage(IUnit attacker, IUnit defender)
    {
        int baseDamage = attacker.Attack - defender.Defense;
        int randomFactor = Random.Range(-5, 6);
        return Mathf.Max(1, baseDamage + randomFactor);
    }

    public void ProcessTurn()
    {
        Debug.Log($"Processing {UnitCount} units for {civilization.CivName}");

        foreach (var unit in units.ToList())
        {
            unit.ProcessTurn();

            if (!unit.IsAlive())
            {
                RemoveUnit(unit.UnitId);
            }
        }

        ResetMovement();
    }

    public void ResetMovement()
    {
        foreach (var unit in units)
        {
            
            unit.Movement = unit.MaxMovement;
        }
    }

  
    public bool CanSettleCity(IUnit settler)
    {
        if (settler.UnitType != UnitType.Settler) return false;
        if (settler.CurrentHex == null) return false;

        var currentHex = (Hex)settler.CurrentHex;

        // Check terrain suitability
        if (currentHex.IsObstacle) return false;
        if (currentHex.Terrain == TerrainType.Mountain || currentHex.Terrain == TerrainType.Ocean) return false;

        // Check if hex is already worked
        if (currentHex.IsWorked) return false;

        // Check distance from ALL cities (including other civs)
        var civsManager = civilization.CivManager?.CivsManager;
        if (civsManager != null)
        {
            var allCivs = civsManager.GetAliveCivilizations();
            foreach (var civ in allCivs)
            {
                var citiesManager = civ.CivManager?.CitiesManager;
                if (citiesManager != null)
                {
                    var cities = citiesManager.GetAllCities();
                    foreach (var city in cities)
                    {
                        var cityHex = (Hex)city.CenterHex;
                        if (cityHex != null)
                        {
                            int distance = mapManager.GetDistance(currentHex, cityHex);
                            if (distance < 4) // Minimum 4 hex distance
                            {
                                return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }

    public string GetSettleFailureReason(IUnit settler)
    {
        if (settler.UnitType != UnitType.Settler) return "Not a settler unit";
        if (settler.CurrentHex == null) return "No current hex";

        var currentHex = (Hex)settler.CurrentHex;

        if (currentHex.IsObstacle || currentHex.Terrain == TerrainType.Mountain || currentHex.Terrain == TerrainType.Ocean)
        {
            return "Invalid terrain - cannot settle on obstacles, mountains, or ocean";
        }

        if (currentHex.IsWorked)
        {
            return "Hex is already worked by another city";
        }

        // Check city distance
        var civsManager = civilization.CivManager?.CivsManager;
        if (civsManager != null)
        {
            var allCivs = civsManager.GetAliveCivilizations();
            foreach (var civ in allCivs)
            {
                var citiesManager = civ.CivManager?.CitiesManager;
                if (citiesManager != null)
                {
                    var cities = citiesManager.GetAllCities();
                    foreach (var city in cities)
                    {
                        var cityHex = (Hex)city.CenterHex;
                        if (cityHex != null)
                        {
                            int distance = mapManager.GetDistance(currentHex, cityHex);
                            if (distance < 4)
                            {
                                return $"Too close to {city.CityName} (distance: {distance}, minimum: 4)";
                            }
                        }
                    }
                }
            }
        }

        return "Unknown reason";
    }

    public string GenerateCityName()
    {
        var citiesManager = civilization.CivManager?.CitiesManager;
        int cityCount = citiesManager?.GetCityCount() ?? 0;

        string baseName = civilization.CivName;

        // Simple naming pattern
        switch (cityCount)
        {
            case 0: return $"{baseName} Capital";
            case 1: return $"New {baseName}";
            case 2: return $"{baseName} Harbor";
            case 3: return $"{baseName} Valley";
            default: return $"{baseName} City {cityCount + 1}";
        }
    }

    public bool SettleCity(IUnit settler, string cityName = null)
    {
        if (!CanSettleCity(settler))
        {
            Debug.LogWarning($"Cannot settle city: {GetSettleFailureReason(settler)}");
            return false;
        }

        var settlementHex = settler.CurrentHex;
        var citiesManager = civilization.CivManager?.CitiesManager;

        if (citiesManager == null)
        {
            Debug.LogError("No CitiesManager available for settling");
            return false;
        }

        // Generate city name if not provided
        if (string.IsNullOrEmpty(cityName))
        {
            cityName = GenerateCityName();
        }

        Debug.Log($"Settling {cityName} at ({settlementHex.Q}, {settlementHex.R})");

        // Create the city using existing CitiesManager
        var newCity = citiesManager.FoundCity(cityName, settlementHex);

        if (newCity != null)
        {
            Debug.Log($"Successfully settled {cityName}!");

            // Remove the settler unit
            bool removed = RemoveUnit(settler.UnitId);
            if (removed)
            {
                Debug.Log($"Settler {settler.UnitName} consumed in settling");
            }

            return true;
        }
        else
        {
            Debug.LogError($"Failed to found city {cityName}");
            return false;
        }
    }

    // ============ WORKER IMPROVEMENT METHODS ============
    public bool CanBuildImprovement(IUnit worker)
    {
        if (worker.UnitType != UnitType.Worker) return false;
        if (worker.CurrentHex == null) return false;

        var currentHex = (Hex)worker.CurrentHex;

        // Check if hex can have improvements
        if (currentHex.IsObstacle) return false;
        if (currentHex.Improvement != ImprovementType.None) return false; // Already has improvement

        // Check if terrain supports improvements
        switch (currentHex.Terrain)
        {
            case TerrainType.Grassland:
            case TerrainType.Plains:
            case TerrainType.Desert:
            case TerrainType.Hill:
                return true;
            default:
                return false;
        }
    }

    public ImprovementType GetBestImprovementForHex(IHex hex)
    {
        var concreteHex = (Hex)hex;

        // Auto-select improvement based on terrain and resources
        switch (concreteHex.Resource)
        {
            case ResourceType.Additional:
                switch (concreteHex.additionalResource)
                {
                    case AdditionalResourceType.Cattle:
                    case AdditionalResourceType.Sheep:
                        return ImprovementType.Pasture;
                    case AdditionalResourceType.Fish:
                        return ImprovementType.FishingBoats;
                    case AdditionalResourceType.Wheat:
                        return ImprovementType.Farm;
                    default:
                        break;
                }
                break;
            case ResourceType.Strategic:
            case ResourceType.Luxury:
                return ImprovementType.Mine;
        }

        // Default based on terrain
        switch (concreteHex.Terrain)
        {
            case TerrainType.Grassland:
            case TerrainType.Plains:
                return ImprovementType.Farm;
            case TerrainType.Hill:
                return ImprovementType.Mine;
            case TerrainType.Desert:
                return ImprovementType.TradingPost;
            default:
                return ImprovementType.None;
        }
    }

    public bool BuildImprovement(IUnit worker, ImprovementType improvementType = ImprovementType.None)
    {
        if (!CanBuildImprovement(worker))
        {
            Debug.LogWarning($"Cannot build improvement: Worker cannot build here");
            return false;
        }

        var targetHex = (Hex)worker.CurrentHex;

        // Auto-select improvement if not specified
        if (improvementType == ImprovementType.None)
        {
            improvementType = GetBestImprovementForHex(targetHex);
        }

        if (improvementType == ImprovementType.None)
        {
            Debug.LogWarning("No suitable improvement for this hex");
            return false;
        }

        // Set improvement on hex
        targetHex.Improvement = improvementType;

        // Worker consumes movement
        worker.Movement = 0;

        Debug.Log($"Built {improvementType} at ({targetHex.Q}, {targetHex.R})");
        return true;
    }

    // Build unit methods
    public bool CanBuildUnit(UnitType unitType)
    {
        var unitData = GetUnitPrefabData(unitType);
        if (unitData == null) return false;

        // Check tech requirements
        var techManager = civilization.CivManager?.TechManager;
        if (techManager != null && unitData.requiredTechs != null)
        {
            foreach (var tech in unitData.requiredTechs)
            {
                if (!techManager.HasTechnology(tech))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public int GetUnitProductionCost(UnitType unitType)
    {
        var unitData = GetUnitPrefabData(unitType);
        return unitData?.productionCost ?? 100;
    }

    // Utility methods
    public List<IUnit> GetCombatUnits()
    {
        return units.Where(u => u.IsCombatUnit()).ToList();
    }

    public List<IUnit> GetCivilianUnits()
    {
        return units.Where(u => u.IsCivilianUnit()).ToList();
    }

    public int GetUnitTypeCount(UnitType unitType)
    {
        return units.Count(u => u.UnitType == unitType);
    }

    public bool HasUnits()
    {
        return units.Count > 0;
    }

    // Get all units at a hex (including enemy units for visibility)
    public List<IUnit> GetAllUnitsAtHex(IHex hex, ICivsManager civsManager)
    {
        var allUnits = new List<IUnit>();

        // Add our units
        allUnits.AddRange(GetUnitsAt(hex));

        // Add other civs' units (for selection/visibility)
        if (civsManager != null)
        {
            var allCivs = civsManager.GetAliveCivilizations();
            foreach (var civ in allCivs)
            {
                if (civ.CivId != civilization.CivId)
                {
                    var enemyUnitsManager = civ.CivManager?.UnitsManager;
                    if (enemyUnitsManager != null)
                    {
                        var enemyUnits = enemyUnitsManager.GetUnitsAt(hex);
                        allUnits.AddRange(enemyUnits);
                    }
                }
            }
        }

        return allUnits;
    }

    // Check if unit can move at all
    public bool CanUnitMove(IUnit unit)
    {
        if (unit == null) return false;
        if (unit.Movement <= 0 || unit.HasMoved) return false;

        var validMoves = GetValidMoves(unit);
        return validMoves.Count > 0;
    }

    // Get the best move for AI (placeholder for future AI)
    public IHex GetBestMoveForAI(IUnit unit)
    {
        var validMoves = GetValidMoves(unit);
        if (validMoves.Count == 0) return null;

        // Simple AI: random move for now
        int randomIndex = Random.Range(0, validMoves.Count);
        return validMoves[randomIndex];
    }
}