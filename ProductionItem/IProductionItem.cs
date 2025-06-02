public interface IProductionItem
{
    string Id { get; }
    ProductionType Type { get; }
    string DisplayName { get; }
    int ProductionCost { get; }
    int ProductionAccumulated { get; set; }
    bool IsCompleted { get; }
    int TurnsRemaining(int productionPerTurn);
}