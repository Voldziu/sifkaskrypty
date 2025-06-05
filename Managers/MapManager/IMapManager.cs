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
    List<Hex> GetReachableHexesWithMovement(Hex start, int movementPoints);
    Hex GetHex(Vector2Int coords);
    Dictionary<Vector2Int, Hex> GetAllHexes();

    // Basic Visual Highlighting
    void ClearHighlights();
    void HighlightNeighbors(Hex hex);
    void HighlightPath(Hex start, Hex end);
    void HighlightRange(Hex start, int range);

    // Advanced Unit & City Highlighting
    void ShowUnitMovementOptions(IUnit unit, IUnitsManager unitsManager, Color color);
    void ShowUnitInMovementMode(IUnit unit, IUnitsManager unitsManager, Color moveColor);
    void ShowCityWorkRadius(ICity city, Color workColor);
    void HighlightAttackRange(IUnit unit, Color attackColor, IUnitsManager unitsManager);

    // Events
    UnityEvent<Hex> OnHexSelected { get; }
    UnityEvent<Hex> OnHexDeselected { get; }
}