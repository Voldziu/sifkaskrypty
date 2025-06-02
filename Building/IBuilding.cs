using System.Collections.Generic;
using static UnityEngine.Rendering.DebugUI;

public interface IBuilding
{
    string Id { get; }
    string DisplayName { get; }
    int ProductionCost { get; }
    Yields Yields { get; }
    int SpecialistSlots { get; }
    List<string> Prerequisites { get; }
}