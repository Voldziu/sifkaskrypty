using System.Collections.Generic;

public interface IUnitsManager
{
    List<IUnit> Units { get; }
    int UnitCount { get; }

    // Initialization
    void Initialize(ICivilization civilization, IMapManager mapManager);

    // Unit Management
    IUnit CreateUnit(UnitCategory unitCategory,UnitType unitType, IHex location);
    IUnit GetUnit(string unitId);
    List<IUnit> GetUnitsAt(IHex hex);
    bool RemoveUnit(string unitId);

    // Movement & Combat
    bool MoveUnit(IUnit unit, IHex destination);
    List<IHex> GetValidMoves(IUnit unit);
    bool CanAttack(IUnit attacker, IUnit target);
    void Attack(IUnit attacker, IUnit target);


    public int GetMovementCostTo(IUnit unit, IHex destination);

    // Turn Processing
    void ProcessTurn();
    void ResetMovement();

    // Events
    event System.Action<IUnit> OnUnitCreated;
    event System.Action<IUnit> OnUnitDestroyed;
    event System.Action<IUnit, IHex> OnUnitMoved;
}