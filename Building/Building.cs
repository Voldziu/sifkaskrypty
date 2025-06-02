using System.Collections.Generic;

[System.Serializable]
public class Building : IBuilding
{
    public string id;
    public string displayName;
    public int productionCost;
    public Yields yields;
    public int specialistSlots;
    public List<string> prerequisites;

    public string Id => id;
    public string DisplayName => displayName;
    public int ProductionCost => productionCost;
    public Yields Yields => yields;
    public int SpecialistSlots => specialistSlots;
    public List<string> Prerequisites => prerequisites;

    public Building(string id, string displayName, int productionCost, Yields yields, int specialistSlots = 0)
    {
        this.id = id;
        this.displayName = displayName;
        this.productionCost = productionCost;
        this.yields = yields;
        this.specialistSlots = specialistSlots;
        this.prerequisites = new List<string>();
    }
}