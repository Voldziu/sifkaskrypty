using System.Collections.Generic;

[System.Serializable]
public class Technology : ITechnology
{
    public string techId;
    public string techName;
    public string description;
    public TechEra era;
    public int scienceCost;
    public List<string> prerequisites;
    public List<string> unlocksBuildings;
    public List<string> unlocksUnits;

    // Properties
    public string TechId => techId;
    public string TechName => techName;
    public string Description => description;
    public TechEra Era => era;
    public int ScienceCost => scienceCost;
    public List<string> Prerequisites => prerequisites;
    public List<string> UnlocksBuildings => unlocksBuildings;
    public List<string> UnlocksUnits => unlocksUnits;

    public Technology(string techId, string techName, string description, TechEra era, int scienceCost)
    {
        this.techId = techId;
        this.techName = techName;
        this.description = description;
        this.era = era;
        this.scienceCost = scienceCost;
        this.prerequisites = new List<string>();
        this.unlocksBuildings = new List<string>();
        this.unlocksUnits = new List<string>();
    }

    public Technology AddPrerequisite(string prereqTechId)
    {
        if (!prerequisites.Contains(prereqTechId))
        {
            prerequisites.Add(prereqTechId);
        }
        return this;
    }

    public Technology UnlockBuilding(string buildingId)
    {
        if (!unlocksBuildings.Contains(buildingId))
        {
            unlocksBuildings.Add(buildingId);
        }
        return this;
    }

    public Technology UnlockUnit(string unitId)
    {
        if (!unlocksUnits.Contains(unitId))
        {
            unlocksUnits.Add(unitId);
        }
        return this;
    }
}