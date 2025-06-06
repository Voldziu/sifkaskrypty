using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitData : ProductionItem
{
    [SerializeField] private UnitType unitType;
    [SerializeField] private UnitCategory unitCategory;

    [Header("Combat Stats")]
    [SerializeField] private int attack;
    [SerializeField] private int defense;
    [SerializeField] private int health;
    [SerializeField] private int movement;

    public override ProductionItemType ItemType => ProductionItemType.Unit;
    public UnitType UnitType => unitType;
    public UnitCategory UnitCategory => unitCategory;
    public int Attack => attack;
    public int Defense => defense;
    public int MaxHealth => health;
    public int MaxMovement => movement;

    public UnitData(string id, string displayName, int productionCost, UnitType unitType, UnitCategory unitCategory,
                   int attack, int defense, int health, int movement)
        : base(id, displayName, productionCost)
    {
        this.unitType = unitType;
        this.unitCategory = unitCategory;
        this.attack = attack;
        this.defense = defense;
        this.health = health;
        this.movement = movement;
        this.icon = Resources.Load<Sprite>($"Icons/Units/{id}") ?? Resources.Load<Sprite>("Icons/placeholder");
    }

   
}