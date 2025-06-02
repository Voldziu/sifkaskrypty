using System.Collections.Generic;

public enum CivRelation
{
    War,
    Peace,
    Allied,
    Neutral
}

public interface ICivsManager
{
    List<ICivilization> Civilizations { get; }
    int CivCount { get; }

    // Initialization
    void Initialize(IMapManager mapManager);

    // Civ Management
    ICivilization CreateCivilization(string civName, string leaderName, bool isHuman = false);
    ICivilization GetCivilization(string civId);
    ICivilization GetCivilizationByName(string civName);
    List<ICivilization> GetAliveCivilizations();
    int GetAliveCivCount();

    // Turn Processing
    void ProcessTurn();

    // Inter-Civ Relations
    CivRelation GetRelation(ICivilization civ1, ICivilization civ2);
    void SetRelation(ICivilization civ1, ICivilization civ2, CivRelation relation);
    bool AreAtWar(ICivilization civ1, ICivilization civ2);
    bool AreAllied(ICivilization civ1, ICivilization civ2);

    // Victory Conditions
    ICivilization CheckVictoryConditions();

    // Game Stats
    Dictionary<string, object> GetGameStats();

    // Events
    event System.Action<ICivilization> OnCivilizationCreated;
    event System.Action<ICivilization> OnCivilizationDestroyed;
    event System.Action<ICivilization, ICivilization, CivRelation> OnRelationChanged;
}