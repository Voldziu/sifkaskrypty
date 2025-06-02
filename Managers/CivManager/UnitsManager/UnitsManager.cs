using System.Collections.Generic;
using UnityEngine;

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

    private List<IUnit> units = new List<IUnit>();
    private ICivilization civilization;
    private IMapManager mapManager;
    private int unitIdCounter = 0;

    // Properties
    public List<IUnit> Units => units;
    public int UnitCount => units.Count;

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

    public IUnit CreateUnit(UnitType unitType, IHex location)
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

        // Apply civilization colors/materials
        ApplyCivVisuals(unitComponent);

        units.Add(unitComponent);
        OnUnitCreated?.Invoke(unitComponent);

        Debug.Log($"Created {unitType} for {civilization.CivName} at ({location.Q}, {location.R})");
        return unitComponent;
    }

    bool CanPlaceUnitAt(UnitType unitType, IHex location)
    {
        if (!allowUnitStacking) return GetUnitsAt(location).Count == 0;

        var unitsAtLocation = GetUnitsAt(location);
        var unitCategory = GetUnitCategory(unitType);

        // Check if there's already a unit of the same category at this location
        foreach (var unit in unitsAtLocation)
        {
            if (unit.UnitCategory == unitCategory)
            {
                return false; // Already has a unit of this category
            }
        }

        return true;
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

    void ApplyCivVisuals(Unit unit)
    {
        // Apply civilization-specific colors or materials
        var renderers = unit.GetComponentsInChildren<Renderer>();
        Color civColor = GetCivColor();

        foreach (var renderer in renderers)
        {
            // Tint the unit with civilization color
            renderer.material.color = civColor;
        }
    }

    Color GetCivColor()
    {
        switch (civilization.CivName)
        {
            case "Rome": return Color.red;
            case "Egypt": return Color.yellow;
            case "Greece": return Color.blue;
            case "China": return Color.green;
            default: return Color.white;
        }
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

        // Check stacking rules at destination
        if (!CanPlaceUnitAt(unit.UnitType, destination))
        {
            Debug.LogWarning($"Cannot move {unit.UnitName} - destination blocked");
            return false;
        }

        var oldHex = unit.CurrentHex;
        unit.MoveTo(destination);

        OnUnitMoved?.Invoke(unit, destination);
        Debug.Log($"Moved {unit.UnitName} from ({oldHex.Q}, {oldHex.R}) to ({destination.Q}, {destination.R})");

        return true;
    }

    public List<IHex> GetValidMoves(IUnit unit)
    {
        if (unit == null) return new List<IHex>();

        var validMoves = new List<IHex>();
        var reachableHexes = mapManager.GetReachableHexes((Hex)unit.CurrentHex, unit.Movement);

        foreach (var hex in reachableHexes)
        {
            if (unit.CanMoveTo(hex) && CanPlaceUnitAt(unit.UnitType, hex))
            {
                validMoves.Add(hex);
            }
        }

        return validMoves;
    }

    public bool CanAttack(IUnit attacker, IUnit target)
    {
        if (attacker == null || target == null) return false;
        if (!attacker.IsCombatUnit()) return false;
        if (attacker.HasMoved) return false;

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

        attacker.HasMoved = true;

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
            unit.HasMoved = false;
            unit.Movement = unit.MaxMovement;
        }
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
}