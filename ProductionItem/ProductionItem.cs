using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public abstract class ProductionItem : IProductionItem
{
    public string id;
    public  ProductionItemType type;
    public string displayName;
    public int productionCost;
    public int productionAccumulated;
    public Sprite icon;
    public List<string> requiredTechs;

    public string Id => id;
    public abstract ProductionItemType ItemType { get; }
    public string DisplayName => displayName;
    public int ProductionCost => productionCost;
    public virtual int ProductionAccumulated
    {
        get => productionAccumulated;
        set => productionAccumulated = value;
    }
    public Sprite Icon => icon;

    public List<string> RequiredTechs => requiredTechs;



    public bool IsCompleted => productionAccumulated >= productionCost;

    protected ProductionItem(string id, string displayName, int productionCost)
    {
        this.id = id;
        this.displayName = displayName;
        this.productionCost = productionCost;
        this.productionAccumulated = 0;
        this.requiredTechs = new List<string>();
    }

    public ProductionItem AddRequiredTech(string techId)
    {
        if (!requiredTechs.Contains(techId))
        {
            requiredTechs.Add(techId);
        }
        return this;
    }




}