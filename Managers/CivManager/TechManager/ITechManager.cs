using System.Collections.Generic;



public interface ITechManager
{

    public CivManager CivManager { get; }
    List<string> ResearchedTechs { get; }
    ITechnology CurrentResearch { get; }
    int ScienceAccumulated { get; }

    // Initialization
    void Initialize(ICivilization civilization);

    // Research Management
    bool StartResearch(string techId);
    bool CanResearch(string techId);
    List<ITechnology> GetAvailableTechs();
    bool HasTechnology(string techId);

    // Turn Processing
    void ProcessTurn(int sciencePoints);

    // Queries
    bool CanBuildBuilding(string buildingId);
    bool CanBuildUnit(string unitId);
    List<ITechnology> GetTechsByEra(TechEra era);

    // Events
    event System.Action<ITechnology> OnTechnologyResearched;
    event System.Action<ITechnology> OnResearchStarted;
}