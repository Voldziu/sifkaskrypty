using System.Collections.Generic;
using UnityEngine;

public enum ProductionItemType
{
    Building,
    Unit
}

public interface IProductionItem
{
    string Id { get; }
    string DisplayName { get; }
    int ProductionCost { get; }
    ProductionItemType ItemType { get; }
    Sprite Icon { get; }
    List<string> RequiredTechs { get; }
    bool IsCompleted { get; }

    public int ProductionAccumulated
    {
        get;
        set;
    }

    public ProductionItem AddRequiredTech(string techId);




}