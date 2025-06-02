using UnityEngine;

[System.Serializable]
public class Civilization : ICivilization
{
    [Header("Basic Info")]
    public string civId;
    public string civName;
    public string leaderName;
    public bool isHuman;
    public bool isAlive = true;

    [Header("Civilization Resources")]
    public int gold = 0;
    public int science = 0;
    public int culture = 0;
    public int faith = 0;

    private ICivManager civManager;

    // Properties
    public string CivId => civId;
    public string CivName => civName;
    public string LeaderName => leaderName;
    public bool IsHuman => isHuman;
    public bool IsAlive => isAlive;

    public int Gold
    {
        get => gold;
        set => gold = Mathf.Max(0, value);
    }

    public int Science
    {
        get => science;
        set => science = Mathf.Max(0, value);
    }

    public int Culture
    {
        get => culture;
        set => culture = Mathf.Max(0, value);
    }

    public int Faith
    {
        get => faith;
        set => faith = Mathf.Max(0, value);
    }

    public ICivManager CivManager => civManager;

    // Events
    public event System.Action<ICivilization> OnCivilizationDestroyed;

    public Civilization(string civId, string civName, string leaderName, bool isHuman = false)
    {
        this.civId = civId;
        this.civName = civName;
        this.leaderName = leaderName;
        this.isHuman = isHuman;
        this.isAlive = true;

        // Starting resources
        this.gold = 50;
        this.science = 0;
        this.culture = 0;
        this.faith = 0;
    }

    public void SetCivManager(ICivManager civManager)
    {
        this.civManager = civManager;
    }

    public void DestroyCivilization()
    {
        if (isAlive)
        {
            isAlive = false;
            OnCivilizationDestroyed?.Invoke(this);
            Debug.Log($"Civilization {civName} has been destroyed!");
        }
    }

    public void AddGold(int amount)
    {
        Gold += amount;
    }

    public bool SpendGold(int amount)
    {
        if (gold >= amount)
        {
            Gold -= amount;
            return true;
        }
        return false;
    }

    public void AddScience(int amount)
    {
        Science += amount;
    }

    public void AddCulture(int amount)
    {
        Culture += amount;
    }

    public void AddFaith(int amount)
    {
        Faith += amount;
    }

    public override string ToString()
    {
        return $"Civilization: {civName} (Leader: {leaderName}, Human: {isHuman}, Alive: {isAlive})";
    }
}