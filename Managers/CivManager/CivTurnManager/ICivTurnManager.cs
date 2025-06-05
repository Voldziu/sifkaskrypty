using System.Collections.Generic;

public interface ICivTurnManager
{
    ICivilization Civilization { get; }

    // Initialization
    void Initialize(ICivilization civilization, IMapManager mapManager);

    // Turn state checking
    bool CanEndTurn();
    List<IUnit> GetUnitsNeedingOrders();
    List<ICity> GetCitiesNeedingProduction();

    // Navigation helpers
    void GoToNextUnitNeedingOrders();
    void GoToNextCityNeedingProduction();
    void GoToNextItemNeedingOrders(); // Units first, then cities

    // Unit state management
    void SetUnitGuard(IUnit unit);
    void SetUnitSleep(IUnit unit);
    void WakeUnit(IUnit unit);
    bool IsUnitSkipped(IUnit unit);

    // Turn processing
    void ProcessTurnStart();
    void ProcessTurnEnd();

    // Events
    event System.Action<bool> OnCanEndTurnChanged;
    event System.Action<IUnit> OnUnitNeedsOrders;
    event System.Action<ICity> OnCityNeedsProduction;
}