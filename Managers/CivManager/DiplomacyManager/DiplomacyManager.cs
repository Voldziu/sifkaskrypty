using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiplomacyManager : MonoBehaviour, IDiplomacyManager
{
    [Header("Diplomacy Settings")]
    public int baseDiplomaticPoints = 50;
    public int maxDiplomaticPoints = 100;
    public int minDiplomaticPoints = -100;

    private ICivilization civilization;
    private ICivsManager civsManager;
    private Dictionary<string, int> diplomaticPoints = new Dictionary<string, int>();
    private List<IDiplomaticDeal> activeDeals = new List<IDiplomaticDeal>();
    private List<IDiplomaticDeal> pendingDeals = new List<IDiplomaticDeal>();
    private int dealIdCounter = 0;

    // Properties
    public ICivilization Civilization => civilization;

    // Events
    public event System.Action<IDiplomaticDeal> OnDealProposed;
    public event System.Action<IDiplomaticDeal> OnDealAccepted;
    public event System.Action<IDiplomaticDeal> OnDealRejected;
    public event System.Action<ICivilization, CivRelation> OnRelationChanged;

    public void Initialize(ICivilization civilization, ICivsManager civsManager)
    {
        this.civilization = civilization;
        this.civsManager = civsManager;

        // Initialize diplomatic points with all other civs
        InitializeDiplomaticPoints();

        Debug.Log($"DiplomacyManager initialized for {civilization.CivName}");
    }

    void InitializeDiplomaticPoints()
    {
        var allCivs = civsManager.Civilizations;
        foreach (var civ in allCivs)
        {
            if (civ.CivId != civilization.CivId)
            {
                diplomaticPoints[civ.CivId] = baseDiplomaticPoints;
            }
        }
    }

    public bool ProposeDeal(ICivilization targetCiv, DiplomacyAction action, Dictionary<string, object> terms = null)
    {
        if (targetCiv == null || targetCiv.CivId == civilization.CivId)
        {
            Debug.LogWarning("Cannot propose deal to self or null civilization");
            return false;
        }

        // Check if action is valid
        if (!CanPerformAction(targetCiv, action))
        {
            Debug.LogWarning($"Cannot perform {action} with {targetCiv.CivName}");
            return false;
        }

        string dealId = $"deal_{++dealIdCounter}";
        var deal = new DiplomaticDeal(dealId, civilization, targetCiv, action, terms);

        // Some actions are immediate (like war declarations)
        if (IsImmediateAction(action))
        {
            ExecuteImmediateAction(deal);
        }
        else
        {
            // Add to pending deals for AI/human decision
            pendingDeals.Add(deal);
            OnDealProposed?.Invoke(deal);

            // For AI civs, auto-process the deal
            if (!targetCiv.IsHuman)
            {
                ProcessAIDealResponse(deal);
            }
        }

        Debug.Log($"{civilization.CivName} proposed {action} to {targetCiv.CivName}");
        return true;
    }

    bool CanPerformAction(ICivilization targetCiv, DiplomacyAction action)
    {
        var currentRelation = GetRelationWith(targetCiv);

        switch (action)
        {
            case DiplomacyAction.DeclareWar:
                return currentRelation != CivRelation.War;

            case DiplomacyAction.MakePeace:
                return currentRelation == CivRelation.War;

            case DiplomacyAction.ProposeAlliance:
                return currentRelation == CivRelation.Peace || currentRelation == CivRelation.Neutral;

            case DiplomacyAction.BreakAlliance:
                return currentRelation == CivRelation.Allied;

            case DiplomacyAction.TradeAgreement:
            case DiplomacyAction.OpenBorders:
                return currentRelation != CivRelation.War;

            default:
                return false;
        }
    }

    bool IsImmediateAction(DiplomacyAction action)
    {
        return action == DiplomacyAction.DeclareWar || action == DiplomacyAction.BreakAlliance;
    }

    void ExecuteImmediateAction(IDiplomaticDeal deal)
    {
        switch (deal.Action)
        {
            case DiplomacyAction.DeclareWar:
                civsManager.SetRelation(deal.ProposingCiv, deal.TargetCiv, CivRelation.War);
                ModifyDiplomaticPoints(deal.TargetCiv, -30);
                break;

            case DiplomacyAction.BreakAlliance:
                civsManager.SetRelation(deal.ProposingCiv, deal.TargetCiv, CivRelation.Neutral);
                ModifyDiplomaticPoints(deal.TargetCiv, -20);
                break;
        }

        OnDealAccepted?.Invoke(deal);
    }

    void ProcessAIDealResponse(IDiplomaticDeal deal)
    {
        // Simple AI logic based on diplomatic points
        int points = GetDiplomaticPoints(deal.TargetCiv);
        bool shouldAccept = false;

        switch (deal.Action)
        {
            case DiplomacyAction.MakePeace:
                shouldAccept = points > -50; // Will make peace if not too hostile
                break;

            case DiplomacyAction.ProposeAlliance:
                shouldAccept = points > 60; // Requires good relations
                break;

            case DiplomacyAction.TradeAgreement:
                shouldAccept = points > 20; // Moderate relations needed
                break;

            case DiplomacyAction.OpenBorders:
                shouldAccept = points > 40; // Good relations needed
                break;
        }

        // Add some randomness
        if (Random.Range(0f, 1f) < 0.2f) // 20% chance to flip decision
        {
            shouldAccept = !shouldAccept;
        }

        if (shouldAccept)
        {
            AcceptDeal(deal.DealId);
        }
        else
        {
            RejectDeal(deal.DealId);
        }
    }

    public bool AcceptDeal(string dealId)
    {
        var deal = pendingDeals.FirstOrDefault(d => d.DealId == dealId);
        if (deal == null) return false;

        pendingDeals.Remove(deal);
        deal.Activate();
        activeDeals.Add(deal);

        // Apply the diplomatic action
        ApplyDiplomaticAction(deal);

        OnDealAccepted?.Invoke(deal);
        Debug.Log($"{deal.TargetCiv.CivName} accepted {deal.Action} from {deal.ProposingCiv.CivName}");

        return true;
    }

    public bool RejectDeal(string dealId)
    {
        var deal = pendingDeals.FirstOrDefault(d => d.DealId == dealId);
        if (deal == null) return false;

        pendingDeals.Remove(deal);

        // Rejecting deals can hurt relations
        ModifyDiplomaticPoints(deal.ProposingCiv, -5);

        OnDealRejected?.Invoke(deal);
        Debug.Log($"{deal.TargetCiv.CivName} rejected {deal.Action} from {deal.ProposingCiv.CivName}");

        return true;
    }

    void ApplyDiplomaticAction(IDiplomaticDeal deal)
    {
        switch (deal.Action)
        {
            case DiplomacyAction.MakePeace:
                civsManager.SetRelation(deal.ProposingCiv, deal.TargetCiv, CivRelation.Peace);
                ModifyDiplomaticPoints(deal.TargetCiv, 20);
                break;

            case DiplomacyAction.ProposeAlliance:
                civsManager.SetRelation(deal.ProposingCiv, deal.TargetCiv, CivRelation.Allied);
                ModifyDiplomaticPoints(deal.TargetCiv, 30);
                break;

            case DiplomacyAction.TradeAgreement:
                ModifyDiplomaticPoints(deal.TargetCiv, 10);
                // Additional trade benefits could be applied here
                break;

            case DiplomacyAction.OpenBorders:
                ModifyDiplomaticPoints(deal.TargetCiv, 5);
                // Allow units to pass through each other's territory
                break;
        }
    }

    public CivRelation GetRelationWith(ICivilization otherCiv)
    {
        return civsManager.GetRelation(civilization, otherCiv);
    }

    public int GetDiplomaticPoints(ICivilization otherCiv)
    {
        return diplomaticPoints.GetValueOrDefault(otherCiv.CivId, baseDiplomaticPoints);
    }

    void ModifyDiplomaticPoints(ICivilization otherCiv, int change)
    {
        if (!diplomaticPoints.ContainsKey(otherCiv.CivId))
        {
            diplomaticPoints[otherCiv.CivId] = baseDiplomaticPoints;
        }

        diplomaticPoints[otherCiv.CivId] = Mathf.Clamp(
            diplomaticPoints[otherCiv.CivId] + change,
            minDiplomaticPoints,
            maxDiplomaticPoints
        );
    }

    public bool CanDeclareWar(ICivilization targetCiv)
    {
        return GetRelationWith(targetCiv) != CivRelation.War;
    }

    public bool CanMakePeace(ICivilization targetCiv)
    {
        return GetRelationWith(targetCiv) == CivRelation.War;
    }

    public List<IDiplomaticDeal> GetActiveDeals()
    {
        return activeDeals.ToList();
    }

    public List<IDiplomaticDeal> GetPendingDeals()
    {
        return pendingDeals.ToList();
    }

    public void ProcessTurn()
    {
        // Process active deals
        foreach (var deal in activeDeals.ToList())
        {
            deal.ProcessTurn();

            if (!deal.IsActive)
            {
                activeDeals.Remove(deal);
                Debug.Log($"Deal expired: {deal.Action} between {deal.ProposingCiv.CivName} and {deal.TargetCiv.CivName}");
            }
        }

        // Gradual diplomatic point changes
        ProcessGradualDiplomacy();

        // AI diplomacy processing
        if (!civilization.IsHuman)
        {
            ProcessAIDiplomacy();
        }
    }

    void ProcessGradualDiplomacy()
    {
        // Diplomatic points slowly trend toward neutral over time
        var civIds = diplomaticPoints.Keys.ToList();
        foreach (var civId in civIds)
        {
            int current = diplomaticPoints[civId];
            if (current > baseDiplomaticPoints)
            {
                diplomaticPoints[civId] = Mathf.Max(baseDiplomaticPoints, current - 1);
            }
            else if (current < baseDiplomaticPoints)
            {
                diplomaticPoints[civId] = Mathf.Min(baseDiplomaticPoints, current + 1);
            }
        }
    }

    public void ProcessAIDiplomacy()
    {
        // Simple AI diplomatic behavior
        var otherCivs = civsManager.Civilizations.Where(c => c.CivId != civilization.CivId && c.IsAlive);

        foreach (var otherCiv in otherCivs)
        {
            int points = GetDiplomaticPoints(otherCiv);
            var relation = GetRelationWith(otherCiv);

            // Random chance to propose deals based on relationship
            if (Random.Range(0f, 1f) < 0.1f) // 10% chance per turn
            {
                if (relation == CivRelation.War && points > -20)
                {
                    ProposeDeal(otherCiv, DiplomacyAction.MakePeace);
                }
                else if (relation == CivRelation.Peace && points > 70)
                {
                    ProposeDeal(otherCiv, DiplomacyAction.ProposeAlliance);
                }
                else if (relation != CivRelation.War && points > 30)
                {
                    ProposeDeal(otherCiv, DiplomacyAction.TradeAgreement);
                }
            }
        }
    }
}