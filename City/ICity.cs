using System.Collections.Generic;

public interface ICity : IYielding
{

    CivManager CivManager { get; }
    string CityId { get; }
    string CityName { get; }
    int Population { get; }
    float FoodStored { get; }
    IHex CenterHex { get; }

    List<IHex> WorkableHexes { get; }
    HashSet<IHex> WorkedHexes { get; }

    List<string> ConstructedBuildings { get; }
    Dictionary<string, int> Specialists { get; }
    List<IProductionItem> ProductionQueue { get; }

    List<IProductionItem> AvailableBuildings { get; }

    List<IProductionItem> AvailableUnits { get; }

    void Initialize(string id, string name, IHex centerHex, HexMapGenerator mapGenerator,CivManager civManager);

    int GetFoodRequiredForGrowth();
    bool CanGrow();
    void ProcessGrowth();

    bool WorkHex(IHex hex);
    bool StopWorkingHex(IHex hex);
    List<IHex> GetAvailableWorkableHexes();
    void OptimizeHexWork();

    void AddToProductionQueue(IProductionItem item);
    IProductionItem GetCurrentProduction();
    bool ProcessProduction(int productionYield);

    public List<IProductionItem> GetAvailableBuildingsForProduction();
    public List<IProductionItem> GetAvailableUnitsForProduction();

    public bool CanProduceBuilding(string buildingId);

    public bool CanProduceUnit(UnitType unitType);


    public void StartProductionById(string itemId);
    public void StartProduction(IProductionItem item);
    public void ChangeProduction(IProductionItem item);

    public int GetTurnsRemaining();










    bool HasBuilding(string buildingId);
    bool AssignSpecialist(string buildingId);

    void ProcessTurn();
}