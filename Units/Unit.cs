using UnityEngine;

public enum UnitCategory
{
    Combat,     // Warriors, Archers, etc. - only one per hex
    Civilian    // Settlers, Workers, etc. - only one per hex
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
    public bool hasMoved;

    [Header("Visual")]
    public GameObject unitPrefab;
    public GameObject unitVisual;

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
    public bool HasMoved { get => hasMoved; set => hasMoved = value; }

    public void Initialize(string unitId, UnitType unitType, Hex startingHex, GameObject prefab = null)
    {
        this.unitId = unitId;
        this.unitType = unitType;
        this.currentHex = startingHex;
        this.hasMoved = false;
        this.unitPrefab = prefab;

        // Set stats based on unit type
        InitializeUnitStats();

        this.unitName = $"{unitType}_{unitId}";
        this.health = this.maxHealth;
        this.movement = this.maxMovement;

        // Create visual representation
        CreateVisualRepresentation();
    }

    void InitializeUnitStats()
    {
        switch (unitType)
        {
            case UnitType.Warrior:
                unitCategory = UnitCategory.Combat;
                maxHealth = 100;
                maxMovement = 2;
                attack = 8;
                defense = 8;
                break;

            case UnitType.Archer:
                unitCategory = UnitCategory.Combat;
                maxHealth = 80;
                maxMovement = 2;
                attack = 12;
                defense = 6;
                break;

            case UnitType.Spearman:
                unitCategory = UnitCategory.Combat;
                maxHealth = 110;
                maxMovement = 2;
                attack = 7;
                defense = 13;
                break;

            case UnitType.Scout:
                unitCategory = UnitCategory.Combat;
                maxHealth = 60;
                maxMovement = 3;
                attack = 4;
                defense = 4;
                break;

            case UnitType.Settler:
                unitCategory = UnitCategory.Civilian;
                maxHealth = 80;
                maxMovement = 2;
                attack = 0;
                defense = 0;
                break;

            case UnitType.Worker:
                unitCategory = UnitCategory.Civilian;
                maxHealth = 60;
                maxMovement = 2;
                attack = 0;
                defense = 0;
                break;

            default:
                unitCategory = UnitCategory.Combat;
                maxHealth = 100;
                maxMovement = 2;
                attack = 5;
                defense = 5;
                break;
        }
    }

    void CreateVisualRepresentation()
    {
        if (unitPrefab != null && currentHex != null)
        {
            Vector3 worldPos = GetWorldPosition();
            unitVisual = Instantiate(unitPrefab, worldPos, Quaternion.identity, transform);
            unitVisual.name = $"{unitName}_Visual";
        }
    }

    Vector3 GetWorldPosition()
    {
        Vector3 basePos = Vector3.zero;

        if (currentHex != null)
        {
            // Try to get map manager reference
            var mapManager = FindObjectOfType<MapManager>();
            if (mapManager != null && mapManager.HexMap != null)
            {
                basePos = mapManager.HexMap.HexToWorld(currentHex.Q, currentHex.R);
            }
            else
            {
                Debug.LogWarning($"Could not find MapManager for unit {unitName} positioning");
                // Fallback: use hex coordinates directly (scaled)
                basePos = new Vector3(currentHex.Q * 1.5f, currentHex.R * 1.3f, 0);
            }
        }

        // Offset combat and civilian units so they don't overlap on same hex
        if (unitCategory == UnitCategory.Combat)
        {
            basePos += new Vector3(-0.2f, 0.1f, -0.1f); // Combat units slightly left and forward
        }
        else
        {
            basePos += new Vector3(0.2f, -0.1f, -0.1f); // Civilian units slightly right and back
        }

        return basePos;
    }

    public void MoveTo(IHex hex)
    {
        if (hex != null && CanMoveTo(hex))
        {
            currentHex = (Hex)hex;
            hasMoved = true;
            movement = Mathf.Max(0, movement - 1);

            // Update visual position
            UpdateVisualPosition();
        }
    }

    void UpdateVisualPosition()
    {
        if (unitVisual != null)
        {
            Vector3 worldPos = GetWorldPosition();
            unitVisual.transform.position = worldPos;
            Debug.Log($"Unit {unitName} positioned at world coordinates: {worldPos}");
        }
        else if (gameObject != null)
        {
            // If no separate visual, position the main GameObject
            Vector3 worldPos = GetWorldPosition();
            transform.position = worldPos;
            Debug.Log($"Unit GameObject {unitName} positioned at: {worldPos}");
        }
    }

    public bool CanMoveTo(IHex hex)
    {
        if (hex == null || movement <= 0 || hasMoved) return false;
        if (hex.IsObstacle) return false;

        // Check unit stacking rules: only one combat and one civilian unit per hex
        // This check should be done by UnitsManager, not here

        return true;
    }

    public void ProcessTurn()
    {
        movement = maxMovement;
        hasMoved = false;

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