using System.Collections.Generic;

[System.Serializable]
public class DiplomaticDeal : IDiplomaticDeal
{
    public string dealId;
    public ICivilization proposingCiv;
    public ICivilization targetCiv;
    public DiplomacyAction action;
    public Dictionary<string, object> terms;
    public bool isActive;
    public int turnsRemaining;

    // Properties
    public string DealId => dealId;
    public ICivilization ProposingCiv => proposingCiv;
    public ICivilization TargetCiv => targetCiv;
    public DiplomacyAction Action => action;
    public Dictionary<string, object> Terms => terms;
    public bool IsActive => isActive;
    public int TurnsRemaining => turnsRemaining;

    public DiplomaticDeal(string dealId, ICivilization proposingCiv, ICivilization targetCiv, DiplomacyAction action, Dictionary<string, object> terms = null)
    {
        this.dealId = dealId;
        this.proposingCiv = proposingCiv;
        this.targetCiv = targetCiv;
        this.action = action;
        this.terms = terms ?? new Dictionary<string, object>();
        this.isActive = false;
        this.turnsRemaining = GetDefaultDuration(action);
    }

    int GetDefaultDuration(DiplomacyAction action)
    {
        switch (action)
        {
            case DiplomacyAction.MakePeace:
                return 10; // Peace treaties last 10 turns minimum
            case DiplomacyAction.ProposeAlliance:
                return 30; // Alliances last 30 turns
            case DiplomacyAction.TradeAgreement:
                return 20; // Trade agreements last 20 turns
            case DiplomacyAction.OpenBorders:
                return 15; // Open borders last 15 turns
            default:
                return 0; // War declarations and alliance breaks are immediate
        }
    }

    public void Activate()
    {
        isActive = true;
    }

    public void ProcessTurn()
    {
        if (isActive && turnsRemaining > 0)
        {
            turnsRemaining--;

            if (turnsRemaining <= 0)
            {
                ExpireDeal();
            }
        }
    }

    void ExpireDeal()
    {
        isActive = false;
        // Additional logic for when deals expire
        // (e.g., alliances revert to neutral, trade agreements end)
    }
}