using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CivTurnManager : MonoBehaviour, ICivTurnManager
{
    private ICivilization civilization;
    private IMapManager mapManager;
    private HashSet<string> skippedUnits = new HashSet<string>(); // Guard/Sleep units

    public ICivilization Civilization => civilization;

    // Events
    public event System.Action<bool> OnCanEndTurnChanged;
    public event System.Action<IUnit> OnUnitNeedsOrders;
    public event System.Action<ICity> OnCityNeedsProduction;

    public void Initialize(ICivilization civilization, IMapManager mapManager)
    {
        this.civilization = civilization;
        this.mapManager = mapManager;
        Debug.Log($"CivTurnManager initialized for {civilization.CivName}");
    }

    public bool CanEndTurn()
    {
        // Check units needing orders
        var unitsNeedingOrders = GetUnitsNeedingOrders();
        if (unitsNeedingOrders.Count > 0) return false;

        // Check cities needing production
        var citiesNeedingProduction = GetCitiesNeedingProduction();
        if (citiesNeedingProduction.Count > 0) return false;

        // TODO: Add more conditions here
        // - Research selection needed
        // - Policy selection needed  
        // - Diplomatic responses needed
        // - etc.

        return true;
    }

    public List<IUnit> GetUnitsNeedingOrders()
    {
        var unitsManager = civilization.CivManager?.UnitsManager;
        if (unitsManager == null) return new List<IUnit>();

        var needingOrders = new List<IUnit>();

        foreach (var unit in unitsManager.Units)
        {
            if (UnitNeedsOrders(unit))
            {
                needingOrders.Add(unit);
            }
        }

        return needingOrders;
    }

    bool UnitNeedsOrders(IUnit unit)
    {
        // Skip if unit is guarded/sleeping
        if (IsUnitSkipped(unit)) return false;

        // Skip if unit has no movement or already moved
        if (unit.Movement <= 0 || unit.HasMoved) return false;

        // Unit has movement and needs orders
        return true;
    }

    public List<ICity> GetCitiesNeedingProduction()
    {
        var citiesManager = civilization.CivManager?.CitiesManager;
        if (citiesManager == null) return new List<ICity>();

        var needingProduction = new List<ICity>();

        foreach (var city in citiesManager.GetAllCities())
        {
            if (CityNeedsProduction(city))
            {
                needingProduction.Add(city);
            }
        }

        return needingProduction;
    }

    bool CityNeedsProduction(ICity city)
    {
        // City needs production if it has no current production item
        return city.GetCurrentProduction() == null;
    }

    public void GoToNextUnitNeedingOrders()
    {
        var units = GetUnitsNeedingOrders();
        if (units.Count > 0)
        {
            var unit = units[0];
            NavigateToHex((Hex)unit.CurrentHex);
            OnUnitNeedsOrders?.Invoke(unit);
        }
    }

    public void GoToNextCityNeedingProduction()
    {
        var cities = GetCitiesNeedingProduction();
        if (cities.Count > 0)
        {
            var city = cities[0];
            NavigateToHex((Hex)city.CenterHex);
            OnCityNeedsProduction?.Invoke(city);
        }
    }

    public void GoToNextItemNeedingOrders()
    {
        // Check units first
        var units = GetUnitsNeedingOrders();
        if (units.Count > 0)
        {
            GoToNextUnitNeedingOrders();
            return;
        }

        // Then check cities
        var cities = GetCitiesNeedingProduction();
        if (cities.Count > 0)
        {
            GoToNextCityNeedingProduction();
            return;
        }

        Debug.Log("No items need orders");
    }

    void NavigateToHex(Hex hex)
    {
        if (hex != null && mapManager != null)
        {
            mapManager.SelectHex(hex);
            Debug.Log($"Navigated to hex ({hex.Q}, {hex.R})");
        }
    }

    public void SetUnitGuard(IUnit unit)
    {
        if (unit.IsCombatUnit())
        {
            skippedUnits.Add(unit.UnitId);
            Debug.Log($"{unit.UnitName} set to Guard");
            NotifyCanEndTurnChanged();
        }
    }

    public void SetUnitSleep(IUnit unit)
    {
        if (unit.IsCivilianUnit())
        {
            skippedUnits.Add(unit.UnitId);
            Debug.Log($"{unit.UnitName} set to Sleep");
            NotifyCanEndTurnChanged();
        }
    }

    public void WakeUnit(IUnit unit)
    {
        if (skippedUnits.Remove(unit.UnitId))
        {
            Debug.Log($"{unit.UnitName} woken up");
            NotifyCanEndTurnChanged();
        }
    }

    public bool IsUnitSkipped(IUnit unit)
    {
        return skippedUnits.Contains(unit.UnitId);
    }

    public void ProcessTurnStart()
    {
        // Clear skipped units at start of turn (units wake up)
        skippedUnits.Clear();
        NotifyCanEndTurnChanged();

        Debug.Log($"{civilization.CivName} turn started - {GetUnitsNeedingOrders().Count} units need orders, {GetCitiesNeedingProduction().Count} cities need production");
    }

    public void ProcessTurnEnd()
    {
        // Any cleanup needed at turn end
        Debug.Log($"{civilization.CivName} turn ended");
    }

    void NotifyCanEndTurnChanged()
    {
        OnCanEndTurnChanged?.Invoke(CanEndTurn());
    }

    // Public method to trigger CanEndTurn check (call when unit moves, production set, etc.)
    public void CheckTurnState()
    {
        NotifyCanEndTurnChanged();
    }

    // Debug info
    public string GetDebugInfo()
    {
        var unitsNeedingOrders = GetUnitsNeedingOrders().Count;
        var citiesNeedingProduction = GetCitiesNeedingProduction().Count;
        var skippedCount = skippedUnits.Count;

        return $"CanEnd: {CanEndTurn()}, UnitsNeedOrders: {unitsNeedingOrders}, CitiesNeedProduction: {citiesNeedingProduction}, Skipped: {skippedCount}";
    }
}