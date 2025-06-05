using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Building : ProductionItem, IBuilding
{
    [SerializeField] private Yields yields;
    [SerializeField] private int specialistSlots;
    [SerializeField] private List<string> prerequisites;

    public override ProductionItemType ItemType => ProductionItemType.Building;
    public Yields Yields => yields;
    public int SpecialistSlots => specialistSlots;
    public List<string> Prerequisites => prerequisites;

    public Building(string id, string displayName, int productionCost, Yields yields, int specialistSlots = 0)
        : base(id, displayName, productionCost)
    {
        this.yields = yields;
        this.specialistSlots = specialistSlots;
        this.prerequisites = new List<string>();
        this.icon = Resources.Load<Sprite>($"Icons/Buildings/{id}");
    }

    
}