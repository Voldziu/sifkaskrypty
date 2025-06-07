using UnityEngine;

[System.Serializable]
public struct CombatPrediction
{
    public int attackerDamage;
    public int defenderDamage;
    public int attackerHealthAfter;
    public int defenderHealthAfter;
    public bool attackerDies;
    public bool defenderDies;
    public bool canAttack;
    public string failureReason;
}

public class CombatManager : MonoBehaviour
{
    [Header("Combat Settings")]
    public int baseRandomVariance = 15; // ±15% damage variance
    public float defenseMultiplier = 0.1f; // Defense reduces damage by 50%

    public MapManager mapManager;

    public void Initialize(MapManager mapManager)
    {
        this.mapManager = mapManager;
        Debug.Log("CombatManager initialized");
    }

    public bool CanAttack(IUnit attacker, IUnit defender)
    {
        Debug.Log($"Checking if {attacker.UnitName} can attack {defender.UnitName}");
        if (attacker == null || defender == null) return false;
        if (!attacker.IsCombatUnit()) return false;
        if (attacker.HasMoved) return false;
        if (!defender.IsAlive() || !attacker.IsAlive()) return false;

        // Check if units are enemies (different civilizations)
        if (!AreEnemies(attacker, defender)) return false;

        // Check range
        Debug.Log($"Attacker position: {attacker.CurrentHex}, ");
        Debug.Log($"Defender position: {defender.CurrentHex}");
        int distance = mapManager.GetDistance((Hex)attacker.CurrentHex, (Hex)defender.CurrentHex);
        
        return distance <= attacker.AttackRange;
    }

    private bool AreEnemies(IUnit unit1, IUnit unit2)
    {
        // Simple check: if units have different civilization names in their GameObject names
        if (unit1 is Unit u1 && unit2 is Unit u2)
        {
            return !u1.name.Split('_')[1].Equals(u2.name.Split('_')[1]);
        }
        return true; // Assume enemies if can't determine
    }

    public CombatPrediction PredictCombat(IUnit attacker, IUnit defender)
    {
        var prediction = new CombatPrediction();

        if (!CanAttack(attacker, defender))
        {
            prediction.canAttack = false;
            prediction.failureReason = GetAttackFailureReason(attacker, defender);
            return prediction;
        }

        prediction.canAttack = true;

        // Calculate attacker damage to defender
        int attackerDamage = CalculateDamage(attacker.Attack, defender.Defense);
        prediction.attackerDamage = attackerDamage;
        prediction.defenderHealthAfter = Mathf.Max(0, defender.Health - attackerDamage);
        prediction.defenderDies = prediction.defenderHealthAfter <= 0;

        // Calculate counter-attack (only for melee vs melee/ranged)
        if (attacker.CombatType == CombatType.Melee && defender.IsCombatUnit())
        {
            int defenderDamage = CalculateDamage(defender.Attack, attacker.Defense);
            prediction.defenderDamage = defenderDamage;
            prediction.attackerHealthAfter = Mathf.Max(0, attacker.Health - defenderDamage);
            prediction.attackerDies = prediction.attackerHealthAfter <= 0;
        }
        else
        {
            // Ranged attacks don't trigger counter-attacks
            prediction.defenderDamage = 0;
            prediction.attackerHealthAfter = attacker.Health;
            prediction.attackerDies = false;
        }

        return prediction;
    }

    private int CalculateDamage(int attack, int defense)
    {
        // Base damage calculation
        float baseDamage = attack * (1f - (defense * defenseMultiplier / 100f));

        // Add random variance
        float variance = Random.Range(-baseRandomVariance, baseRandomVariance + 1) / 100f;
        float finalDamage = baseDamage * (1f + variance);

        return Mathf.Max(1, Mathf.RoundToInt(finalDamage)); // Minimum 1 damage
    }

    private string GetAttackFailureReason(IUnit attacker, IUnit defender)
    {
        if (attacker == null) return "No attacker";
        if (defender == null) return "No defender";
        if (!attacker.IsCombatUnit()) return "Attacker is not a combat unit";
        if (attacker.HasMoved) return "Unit has already moved";
        if (!attacker.IsAlive()) return "Attacker is dead";
        if (!defender.IsAlive()) return "Defender is dead";

        if (!AreEnemies(attacker, defender)) return "Cannot attack friendly units";

        int distance = mapManager.GetDistance((Hex)attacker.CurrentHex, (Hex)defender.CurrentHex);
        if (distance > attacker.AttackRange) return $"Target out of range ({distance} > {attacker.AttackRange})";

        return "Unknown reason";
    }

    public void ExecuteCombat(IUnit attacker, IUnit defender)
    {
        var prediction = PredictCombat(attacker, defender);
        if (!prediction.canAttack)
        {
            Debug.LogWarning($"Combat failed: {prediction.failureReason}");
            return;
        }

        Debug.Log($"Combat: {attacker.UnitName} attacks {defender.UnitName}");

        // Apply damage to defender
        if (prediction.attackerDamage > 0)
        {
            defender.TakeDamage(prediction.attackerDamage);
            Debug.Log($"{defender.UnitName} takes {prediction.attackerDamage} damage ({defender.Health}/{defender.MaxHealth})");
        }

        // Apply counter-attack damage if applicable
        if (prediction.defenderDamage > 0 && defender.IsAlive())
        {
            attacker.TakeDamage(prediction.defenderDamage);
            Debug.Log($"{attacker.UnitName} takes {prediction.defenderDamage} counter-attack damage ({attacker.Health}/{attacker.MaxHealth})");
        }

        // Mark attacker as having moved
        attacker.Movement = 0;

        // Handle unit deaths and movement
        HandleCombatResolution(attacker, defender, prediction);
    }

    private void HandleCombatResolution(IUnit attacker, IUnit defender, CombatPrediction prediction)
    {
        // If defender dies and attacker is melee, move attacker to defender's hex
        if (prediction.defenderDies && attacker.CombatType == CombatType.Melee && attacker.IsAlive())
        {
            var defenderHex = defender.CurrentHex;
            attacker.MoveTo(defenderHex);
            Debug.Log($"{attacker.UnitName} advances to {defenderHex.Q}, {defenderHex.R}");
        }
    }
}