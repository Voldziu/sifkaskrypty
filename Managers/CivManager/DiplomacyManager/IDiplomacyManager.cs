using System.Collections.Generic;



public interface IDiplomacyManager
{
    ICivilization Civilization { get; }

    // Initialization
    void Initialize(ICivilization civilization, ICivsManager civsManager);

    // Diplomatic Actions
    bool ProposeDeal(ICivilization targetCiv, DiplomacyAction action, Dictionary<string, object> terms = null);
    bool AcceptDeal(string dealId);
    bool RejectDeal(string dealId);

    // Relationship Queries
    CivRelation GetRelationWith(ICivilization otherCiv);
    int GetDiplomaticPoints(ICivilization otherCiv);
    bool CanDeclareWar(ICivilization targetCiv);
    bool CanMakePeace(ICivilization targetCiv);

    // Deal Management
    List<IDiplomaticDeal> GetActiveDeals();
    List<IDiplomaticDeal> GetPendingDeals();

    // Turn Processing
    void ProcessTurn();

    // AI Behavior (for non-human civs)
    void ProcessAIDiplomacy();

    // Events
    event System.Action<IDiplomaticDeal> OnDealProposed;
    event System.Action<IDiplomaticDeal> OnDealAccepted;
    event System.Action<IDiplomaticDeal> OnDealRejected;
    event System.Action<ICivilization, CivRelation> OnRelationChanged;
}