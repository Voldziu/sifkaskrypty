using System.Collections.Generic;
using UnityEngine;

public enum TechEra
{
    Ancient, Classical, Medieval, Renaissance, Industrial, Modern, Information
}

public interface ITechnology
{
    string TechId { get; }
    string TechName { get; }
    string Description { get; }
    TechEra Era { get; }
    int ScienceCost { get; }
    List<string> Prerequisites { get; }
    List<string> UnlocksBuildings { get; }
    List<string> UnlocksUnits { get; }
}
