using Unity.VisualScripting;
using UnityEngine;

public enum UnitCategory
{
    Combat,     // Warriors, Archers, etc. - only one per hex
    Civilian    // Settlers, Workers, etc. - only one per hex
}

public enum CombatType
{
    Melee,      // Close combat units
    Ranged,     // Archers and other ranged units
    Siege       // Siege units (not implemented yet)
}   

[System.Serializable]
public class Unit : MonoBehaviour, IUnit
{
    [Header("Basic Info")]
    public string unitId;
    public string unitName;
    public UnitType unitType;
    public UnitCategory unitCategory;

    [Header("Position")]
    public Hex currentHex;

    [Header("Stats")]
    public int health;
    public int maxHealth;
    public int movement;
    public int maxMovement;
    public int attack;
    public int defense;

    [Header("Combat")]
    public CombatType combatType;
    public int attackRange;

    [Header("Visual")]
    public GameObject unitPrefab;
    public GameObject unitVisual;

    [Header("UI")]
    public Sprite icon;

    // Properties
    public string UnitId => unitId;
    public string UnitName => unitName;
    public UnitType UnitType => unitType;
    public UnitCategory UnitCategory => unitCategory;
    public IHex CurrentHex => currentHex;
    public int Health { get => health; set => health = Mathf.Clamp(value, 0, maxHealth); }
    public int MaxHealth => maxHealth;
    public int Movement { get => movement; set => movement = Mathf.Max(0, value); }
    public int MaxMovement => maxMovement;
    public int Attack => attack;
    public int Defense => defense;
    public bool HasMoved => movement == 0;
    public CombatType CombatType => combatType;
    public int AttackRange => attackRange;

    public void Initialize(string unitId, UnitType unitType, Hex startingHex, GameObject prefab = null)
    {
        this.unitId = unitId;
        this.unitType = unitType;
        this.currentHex = startingHex;
        this.unitPrefab = prefab;
        this.unitName = $"{unitType}_{unitId}";

        // Set combat type and range based on unit type
        SetCombatProperties();

        this.health = this.maxHealth;
        this.movement = this.maxMovement;

        UpdateVisualPosition();
    }

    private void SetCombatProperties()
    {
        switch (unitType)
        {
            case UnitType.Warrior:
            case UnitType.Spearman:
            case UnitType.Swordsman:
            case UnitType.Horseman:
                combatType = CombatType.Melee;
                attackRange = 1;
                break;

            case UnitType.Archer:
                combatType = CombatType.Ranged;
                attackRange = 2;
                break;

            case UnitType.Scout:
                combatType = CombatType.Melee;
                attackRange = 1;
                break;

            default: // Civilian units
                combatType = CombatType.Melee; // Default, though they shouldn't attack
                attackRange = 0;
                break;
        }
    }

    Vector3 GetVisualPosition()
    {
        Vector3 basePos = transform.position;
        float yOffset = unitCategory == UnitCategory.Civilian ? -0.2f : 0.2f;
        return new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
    }

    public void MoveTo(IHex hex)
    {
        if (hex != null && CanMoveTo(hex))
        {
            currentHex = (Hex)hex;
            movement = Mathf.Max(0, movement);
            UpdateMainPosition();
            UpdateVisualPosition();
        }
    }

    void UpdateVisualPosition()
    {
        if (unitVisual != null)
        {
            Vector3 visualPos = GetVisualPosition();
            unitVisual.transform.position = visualPos;
        }
    }

    void UpdateMainPosition()
    {
        if (currentHex != null)
        {
            var mapManager = FindObjectOfType<MapManager>();
            if (mapManager != null && mapManager.HexMap != null)
            {
                Vector3 worldPos = mapManager.HexMap.HexToWorld(currentHex.Q, currentHex.R);
                transform.position = worldPos;
            }
        }
    }

    public bool CanMoveTo(IHex hex)
    {
        if (hex == null || movement <= 0) return false;
        if (hex.IsObstacle) return false;
        return true;
    }

    public void ProcessTurn()
    {
        movement = maxMovement;

        if (health < maxHealth)
        {
            Heal(5);
        }
    }

    public void TakeDamage(int damage)
    {
        Health -= damage;

        if (Health <= 0)
        {
            Debug.Log($"{unitName} has been destroyed!");
            DestroyUnit();
        }
    }

    public void Heal(int amount)
    {
        Health += amount;
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    public bool IsCombatUnit()
    {
        return unitCategory == UnitCategory.Combat && attack > 0;
    }

    public bool IsCivilianUnit()
    {
        return unitCategory == UnitCategory.Civilian;
    }

    void DestroyUnit()
    {
        if (unitVisual != null)
        {
            Destroy(unitVisual);
        }
        Destroy(gameObject);
    }

    public override string ToString()
    {
        return $"{unitName} ({health}/{maxHealth} HP, {movement}/{maxMovement} MP)";
    }
}