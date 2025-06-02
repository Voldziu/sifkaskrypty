using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public interface IMapManager
{
    // Map Components
    HexMapGenerator HexMap { get; }

    // Selection
    Hex SelectedHex { get; }
    bool HasSelection { get; }

    // Initialization
    void Initialize();

    // Hex Operations
    Hex GetHexUnderMouse();
    void SelectHex(Hex hex);
    void DeselectHex();

    // Map Queries
    List<Hex> GetNeighbors(Hex hex);
    int GetDistance(Hex a, Hex b);
    List<Hex> FindPath(Hex start, Hex end);
    List<Hex> GetReachableHexes(Hex start, int range);
    Hex GetHex(Vector2Int coords);
    Dictionary<Vector2Int, Hex> GetAllHexes();

    // Visual
    void ClearHighlights();
    void HighlightNeighbors(Hex hex);
    void HighlightPath(Hex start, Hex end);
    void HighlightRange(Hex start, int range);

    // Events
    UnityEvent<Hex> OnHexSelected { get; }
    UnityEvent<Hex> OnHexDeselected { get; }
}