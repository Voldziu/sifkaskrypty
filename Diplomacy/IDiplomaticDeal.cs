using System.Collections.Generic;

public enum DiplomacyAction
{
    DeclareWar, MakePeace, ProposeAlliance, BreakAlliance, TradeAgreement, OpenBorders
}

public interface IDiplomaticDeal
{
    string DealId { get; }
    ICivilization ProposingCiv { get; }
    ICivilization TargetCiv { get; }
    DiplomacyAction Action { get; }
    Dictionary<string, object> Terms { get; }
    bool IsActive { get; }
    int TurnsRemaining { get; }

    void Activate();
    void ProcessTurn();
}