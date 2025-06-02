using UnityEngine;

public interface IHex : IYielding, IWorkable
{
    int Q { get; }
    int R { get; }
    int S { get; }
    Vector2Int AxialCoords { get; }

    int MovementCost { get; }
    bool IsObstacle { get; }

    TerrainType Terrain { get; set; }
    ResourceType Resource { get; set; }
    ImprovementType Improvement { get; set; }

    string WorkedByCityId { get; }

    void Init(int q, int r);
}