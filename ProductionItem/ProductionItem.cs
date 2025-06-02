using UnityEngine;

public enum ProductionType
{
    Building,
    Unit
}

[System.Serializable]
public class ProductionItem : IProductionItem
{
    public string id;
    public ProductionType type;
    public string displayName;
    public int productionCost;
    public int productionAccumulated;

    public string Id => id;
    public ProductionType Type => type;
    public string DisplayName => displayName;
    public int ProductionCost => productionCost;
    public int ProductionAccumulated
    {
        get => productionAccumulated;
        set => productionAccumulated = value;
    }

    public bool IsCompleted => productionAccumulated >= productionCost;

    public ProductionItem(string id, ProductionType type, string displayName, int productionCost)
    {
        this.id = id;
        this.type = type;
        this.displayName = displayName;
        this.productionCost = productionCost;
        this.productionAccumulated = 0;
    }

    public int TurnsRemaining(int productionPerTurn)
    {
        return productionPerTurn > 0 ? Mathf.CeilToInt((float)(productionCost - productionAccumulated) / productionPerTurn) : int.MaxValue;
    }
}