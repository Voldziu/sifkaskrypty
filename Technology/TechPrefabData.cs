using System.Collections.Generic;
using UnityEngine;

public class TechPrefabData
{
    public string techId;
    public string techName;
    public string description;
    public TechEra era;
    public int scienceCost;
    public List<string> prerequisites;
    public List<string> unlocksBuildings;
    public List<string> unlocksUnits;
    public Sprite techIcon;
    public GameObject techPrefab; // For UI or visual representation
}