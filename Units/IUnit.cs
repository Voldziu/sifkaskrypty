using System.Collections.Generic;
using UnityEngine;

public enum UnitType
{
    Warrior, Archer, Spearman, Settler, Worker, Scout, Horseman, Swordsman
}

public interface IUnit
{
    // Basic Properties
    string UnitId { get; }
    string UnitName { get; }
    UnitType UnitType { get; }
    UnitCategory UnitCategory { get; }

    CombatType CombatType { get; }
    IHex CurrentHex { get; }

    // Stats Properties
    int Health { get; set; }
    int MaxHealth { get; }
    int Movement { get; set; }
    int MaxMovement { get; }
    int Attack { get; }
    int AttackRange { get; }
    int Defense { get; }
    bool HasMoved { get;}

    // Movement Methods
    void MoveTo(IHex hex);
    bool CanMoveTo(IHex hex);

    // Turn Management
    void ProcessTurn();

    // Combat Methods
    void TakeDamage(int damage);
    void Heal(int amount);
    bool IsAlive();

    // Unit Type Checks
    bool IsCombatUnit();
    bool IsCivilianUnit();
}

