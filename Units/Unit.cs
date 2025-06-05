using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.FilePathAttribute;

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
    public bool HasMoved => movement ==0;

    public void Initialize(string unitId, UnitType unitType, Hex startingHex, GameObject prefab = null)
    {
        this.unitId = unitId;
        this.unitType = unitType;
        this.currentHex = startingHex;
       
        this.unitPrefab = prefab;

        this.unitName = $"{unitType}_{unitId}";
        this.health = this.maxHealth;
        this.movement = this.maxMovement;

     

        UpdateVisualPosition();
      
    }

    

   

    Vector3 GetVisualPosition()
    {
        // Start with the main GameObject's world position (hex center)
        Vector3 basePos = transform.position;

        // Add visual offset based on unit category
        float yOffset = 0f;
        if (unitCategory == UnitCategory.Civilian)
        {
            yOffset = -0.2f; // Lower civilians
        }
        else
        {
            yOffset = 0.2f; // Raise combat units
        }

        return new Vector3(basePos.x, basePos.y + yOffset, basePos.z);
    }

    public void MoveTo(IHex hex)
    {
        if (hex != null && CanMoveTo(hex))
        {
            currentHex = (Hex)hex;
            
            movement = Mathf.Max(0, movement);

            // Update visual position

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
            Debug.Log($"Unit {unitName} visual positioned at: {visualPos}");
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
                transform.position = worldPos; // No offset for main GameObject
                Debug.Log($"Unit {unitName} main GameObject positioned at: {worldPos}");
            }
        }
    }

    public bool CanMoveTo(IHex hex)
    {
        if (hex == null || movement <= 0) return false;
        if (hex.IsObstacle) return false;

        // Check unit stacking rules: only one combat and one civilian unit per hex
        // This check should be done by UnitsManager, not here

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