using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class UnitPrefabData
{
    public UnitType unitType;
    public GameObject prefab;           // Your unit texture/model
    public int productionCost;
    public List<string> requiredTechs;  // Tech requirements
}