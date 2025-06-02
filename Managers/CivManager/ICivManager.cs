public interface ICivManager
{
    ICivilization Civilization { get; }

    // Sub-managers
    ICitiesManager CitiesManager { get; }
    IUnitsManager UnitsManager { get; }
    ITechManager TechManager { get; }
    IDiplomacyManager DiplomacyManager { get; }
    IResourceManager ResourceManager { get; }

    // Initialization
    void Initialize(ICivilization civilization, IMapManager mapManager, ICivsManager civsManager);

    // Turn Processing
    void ProcessTurn();

    // Resource Management
    void ProcessResources();
    void CollectYields();

    // Statistics
    int GetCityCount();
    int GetTotalPopulation();
    Yields GetTotalYields();

    // Game State
    bool IsAlive();
    void DestroyCivilization();
}