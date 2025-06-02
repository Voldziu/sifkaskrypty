public interface ICivilization
{
    string CivId { get; }
    string CivName { get; }
    string LeaderName { get; }
    bool IsHuman { get; }
    bool IsAlive { get; }

    // Civilization-wide resources
    int Gold { get; set; }
    int Science { get; set; }
    int Culture { get; set; }
    int Faith { get; set; }

    // Core systems
    ICivManager CivManager { get; }

    void AddGold(int amount);
    bool SpendGold(int amount);
    void AddScience(int amount);
    void AddCulture(int amount);
    void AddFaith(int amount);

    // Lifecycle
    void SetCivManager(ICivManager civManager);
    void DestroyCivilization();

    // Events
    event System.Action<ICivilization> OnCivilizationDestroyed;
}