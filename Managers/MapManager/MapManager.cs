using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class MapManager : MonoBehaviour, IMapManager
{
    [Header("Map Components")]
    public HexMapGenerator hexMap;

    [Header("Selection Visual")]
    public Color neighborColor = Color.green;
    public Color pathColor = Color.cyan;
    public Color rangeColor = Color.yellow;
    public Color selectedColor = Color.red;

    [Header("Movement Visual")]
    public Color validMoveColor = Color.green;
    public Color invalidMoveColor = Color.red;
    public Color movementRangeColor = new Color(0, 1, 1, 0.5f); // Transparent cyan

    [Header("Events")]
    public UnityEvent<Hex> onHexSelected = new UnityEvent<Hex>();
    public UnityEvent<Hex> onHexDeselected = new UnityEvent<Hex>();

    [Header("Input Settings")]
    public float dragThreshold = 0.1f; // Minimum distance to consider a drag

    private Vector3 mouseDownPosition;
    private bool isDragging = false;

    private Hex selectedHex;
    private List<Hex> highlightedHexes = new List<Hex>();
    private Dictionary<Hex, Color> originalColors = new Dictionary<Hex, Color>();
    private bool isInitialized = false;

    // Properties
    public HexMapGenerator HexMap => hexMap;
    public Hex SelectedHex => selectedHex;
    public bool HasSelection => selectedHex != null;
    public UnityEvent<Hex> OnHexSelected => onHexSelected;
    public UnityEvent<Hex> OnHexDeselected => onHexDeselected;

    void Update()
    {
        if (isInitialized)
        {
            HandleInput();
        }
    }

    public void Initialize()
    {
        Debug.Log("=== INITIALIZING MAP MANAGER ===");

        if (hexMap == null)
        {
            hexMap = GetComponent<HexMapGenerator>();
            if (hexMap == null)
            {
                Debug.LogError("MapManager: HexMapGenerator not found!");
                return;
            }
        }

        // Wait for hex map to be generated
        if (hexMap.hexes.Count == 0)
        {
            Debug.LogWarning("MapManager: Hex map not yet generated, waiting...");
            Invoke(nameof(DelayedInitialize), 0.1f);
            return;
        }

        isInitialized = true;
        Debug.Log($"MapManager initialized with {hexMap.hexes.Count} hexes");
    }

    void DelayedInitialize()
    {
        Initialize();
    }

    void HandleInput()
    {

        // Checks if pointer is over UI elements
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        // Track mouse down position
        if (Input.GetMouseButtonDown(0))
        {
            mouseDownPosition = Input.mousePosition;
            isDragging = false;
        }

        // Check if mouse has moved enough to be considered dragging
        if (Input.GetMouseButton(0))
        {
            float mouseMoveDistance = Vector3.Distance(Input.mousePosition, mouseDownPosition);
            if (mouseMoveDistance > dragThreshold * Screen.height) // Scale with screen size
            {
                isDragging = true;
            }
        }

        // Only select hex on mouse button UP and if we weren't dragging
        if (Input.GetMouseButtonUp(0))
        {
            if (!isDragging)
            {
                Hex clickedHex = GetHexUnderMouse();
                if (clickedHex != null)
                {
                    SelectHex(clickedHex);
                    Debug.Log($"Hex clicked (not dragged) at ({clickedHex.Q}, {clickedHex.R})");
                }
            }
            else
            {
                Debug.Log("Mouse was dragged, ignoring hex selection");
            }

            // Reset drag state
            isDragging = false;
        }


    }

    public Hex GetHexUnderMouse()
    {
        if (!isInitialized) return null;

        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int coords = hexMap.WorldToHex(worldPos);
        hexMap.hexes.TryGetValue(coords, out Hex hex);
        return hex;
    }

    public void SelectHex(Hex hex)
    {
        if (hex == null) return;

        // Deselect previous
        if (selectedHex != null)
        {
            OnHexDeselected?.Invoke(selectedHex);
        }

        // Clear all highlights when selecting a new hex
        ClearHighlights();

        // Select new hex
        selectedHex = hex;
        HighlightHex(selectedHex, selectedColor);

        OnHexSelected?.Invoke(selectedHex);
        Debug.Log($"Selected hex at ({hex.Q}, {hex.R}) - Terrain: {hex.Terrain}");
    }

    public void DeselectHex()
    {
        if (selectedHex != null)
        {
            OnHexDeselected?.Invoke(selectedHex);
            ClearHighlights();
            selectedHex = null;
            Debug.Log("Hex deselected");
        }
    }

    public void ClearHighlights()
    {
        int clearedCount = highlightedHexes.Count;

        foreach (Hex hex in highlightedHexes)
        {
            RestoreHexColor(hex);
        }

        highlightedHexes.Clear();
        originalColors.Clear();

        Debug.Log($"Cleared {clearedCount} highlighted hexes");
    }

    // Enhanced highlighting methods for unit movement
    public void HighlightValidMoves(List<IHex> validMoves, Color color)
    {
        foreach (var hex in validMoves)
        {
            if (hex is Hex concreteHex)
            {
                HighlightHex(concreteHex, color);
            }
        }
    }

    public void HighlightValidMovesWithCosts(List<IHex> validMoves, Color color, IUnitsManager unitsManager, IUnit unit)
    {
        foreach (var hex in validMoves)
        {
            if (hex is Hex concreteHex)
            {
                int moveCost = unitsManager.GetMovementCostTo(unit, hex);
                
                HighlightHex(concreteHex, color);
            }
        }
    }

    public void HighlightMovementRange(IHex centerHex, int range, Color color)
    {
        if (centerHex is Hex hex)
        {
            HighlightRange(hex, range, color);
        }
    }

    public void HighlightUnitMovementRange(IUnit unit, Color color, IUnitsManager unitsManager)
    {
        if (unit == null || unitsManager == null) return;

        var validMoves = unitsManager.GetValidMoves(unit);
        HighlightValidMovesWithCosts(validMoves, color, unitsManager, unit);
    }

    public void HighlightMovementModeRange(IUnit unit, Color moveColor, IUnitsManager unitsManager)
    {
        if (unit == null || unitsManager == null) return;

        var validMoves = unitsManager.GetValidMoves(unit);
        HighlightValidMoves(validMoves, moveColor);
    }

    // Highlight hexes around a center point (for city borders, unit vision, etc.)
    public void HighlightArea(IHex centerHex, int radius, Color color)
    {
        if (centerHex is Hex hex)
        {
            HighlightRange(hex, radius, color);
        }
    }

    // Highlight multiple specific hexes with the same color
    public void HighlightHexes(List<IHex> hexes, Color color)
    {
        foreach (var hex in hexes)
        {
            if (hex is Hex concreteHex)
            {
                HighlightHex(concreteHex, color);
            }
        }
    }

    // Highlight attack range for combat units
    public void HighlightAttackRange(IUnit unit, Color attackColor, IUnitsManager unitsManager)
    {
        if (unit == null || !unit.IsCombatUnit() || unitsManager == null) return;

        // For now, melee units attack adjacent hexes
        if (unit.CurrentHex is Hex currentHex)
        {
            var neighbors = GetNeighbors(currentHex);
            foreach (var neighbor in neighbors)
            {
                HighlightHex(neighbor, attackColor);
            }
        }
    }

    // Highlight with different colors for different terrain difficulties
    public void HighlightTerrainAwareness(List<IHex> hexes, Color easyColor, Color hardColor, Color impassableColor)
    {
        foreach (var hex in hexes)
        {
            if (hex is Hex concreteHex)
            {
                Color color;
                if (concreteHex.IsObstacle)
                    color = impassableColor;
                else if (concreteHex.MovementCost > 1)
                    color = hardColor;
                else
                    color = easyColor;

                HighlightHex(concreteHex, color);
            }
        }
    }

    // Quick methods for common use cases
    public void ShowUnitMovementOptions(IUnit unit, IUnitsManager unitsManager, Color color)
    {
        ClearHighlights();
        HighlightUnitMovementRange(unit, color, unitsManager);
    }

    public void ShowUnitInMovementMode(IUnit unit, IUnitsManager unitsManager, Color moveColor)
    {
        ClearHighlights();
        HighlightMovementModeRange(unit, moveColor, unitsManager);
    }

    public void ShowCityWorkRadius(ICity city, Color workColor)
    {
        if (city?.CenterHex != null)
        {
            ClearHighlights();
            HighlightArea(city.CenterHex, 3, workColor);
        }
    }

    // Public method for external highlighting
    public void HighlightHex(Hex hex, Color color)
    {
        if (hex == null)
        {
           // Debug.LogWarning("Trying to highlight null hex");
            return;
        }

        var spriteRenderer = hex.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            // Try to find SpriteRenderer in children
            spriteRenderer = hex.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Debug.Log($"Found SpriteRenderer in children for hex ({hex.Q}, {hex.R})");
            }
        }

        if (spriteRenderer != null)
        {
            // Store original color if not already stored
            if (!originalColors.ContainsKey(hex))
            {
                originalColors[hex] = spriteRenderer.color;
                //Debug.Log($"Stored original color for hex ({hex.Q}, {hex.R}): {spriteRenderer.color}");
            }

            spriteRenderer.color = color;
            if (!highlightedHexes.Contains(hex))
            {
                highlightedHexes.Add(hex);
            }

            //Debug.Log($"Highlighted hex ({hex.Q}, {hex.R}) with color {color}");
        }
        else
        {
            Debug.LogError($"No SpriteRenderer found on hex ({hex.Q}, {hex.R}) or its children! Cannot highlight.");
        }
    }

    void RestoreHexColor(Hex hex)
    {
        if (hex == null)
        {
            Debug.LogWarning("Trying to restore color of null hex");
            return;
        }

        var spriteRenderer = hex.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (originalColors.ContainsKey(hex))
            {
                Color originalColor = originalColors[hex];
                spriteRenderer.color = originalColor;
                //Debug.Log($"Restored hex ({hex.Q}, {hex.R}) to original color: {originalColor}");
            }
            else
            {
                // Default fallback to white
                spriteRenderer.color = Color.white;
               // Debug.Log($"Restored hex ({hex.Q}, {hex.R}) to default white (no original color stored)");
            }
        }
        else
        {
            Debug.LogWarning($"No SpriteRenderer found when trying to restore hex ({hex.Q}, {hex.R}) - checking for SpriteRenderer in children");

            // Try to find SpriteRenderer in children
            var childRenderer = hex.GetComponentInChildren<SpriteRenderer>();
            if (childRenderer != null)
            {
                if (originalColors.ContainsKey(hex))
                {
                    childRenderer.color = originalColors[hex];
                    //Debug.Log($"Restored hex ({hex.Q}, {hex.R}) via child SpriteRenderer");
                }
                else
                {
                    childRenderer.color = Color.white;
                   // Debug.Log($"Restored hex ({hex.Q}, {hex.R}) to white via child SpriteRenderer");
                }
            }
            else
            {
                Debug.LogError($"No SpriteRenderer found at all for hex ({hex.Q}, {hex.R})");
            }
        }
    }

    public void HighlightNeighbors(Hex hex)
    {
        if (!isInitialized || hex == null) return;

        ClearHighlights();

        // Keep selected hex highlighted
        if (selectedHex != null)
            HighlightHex(selectedHex, selectedColor);

        List<Hex> neighbors = GetNeighbors(hex);
        foreach (Hex neighbor in neighbors)
        {
            HighlightHex(neighbor, neighborColor);
        }
    }

    public void HighlightPath(Hex start, Hex end)
    {
        if (!isInitialized || start == null || end == null) return;

        ClearHighlights();

        List<Hex> path = FindPath(start, end);
        foreach (Hex hex in path)
        {
            HighlightHex(hex, pathColor);
        }
    }

    public void HighlightRange(Hex start, int range)
    {
        HighlightRange(start, range, rangeColor);
    }

    public void HighlightRange(Hex start, int range, Color color)
    {
        if (!isInitialized || start == null) return;

        // Don't clear highlights - let caller manage this
        // ClearHighlights();

        List<Hex> reachable = GetReachableHexes(start, range);
        foreach (Hex hex in reachable)
        {
            HighlightHex(hex, color);
        }
    }

    // Enhanced reachable hexes that considers movement costs
    public List<Hex> GetReachableHexesWithMovement(Hex start, int movementPoints)
    {
        if (!isInitialized || start == null) return new List<Hex>();

        var reachable = new List<Hex>();
        var visited = new HashSet<Hex>();
        var queue = new Queue<(Hex hex, int remainingMovement)>();

        queue.Enqueue((start, movementPoints));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (currentHex, remainingMovement) = queue.Dequeue();

            if (currentHex != start) // Don't include starting hex
            {
                reachable.Add(currentHex);
            }

            if (remainingMovement > 0)
            {
                var neighbors = GetNeighbors(currentHex);
                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor) && !neighbor.IsObstacle)
                    {
                        int moveCost = neighbor.MovementCost;
                        if (remainingMovement >= moveCost)
                        {
                            visited.Add(neighbor);
                            queue.Enqueue((neighbor, remainingMovement - moveCost));
                        }
                    }
                }
            }
        }

        return reachable;
    }

    // Utility methods that delegate to HexMapGenerator
    public List<Hex> GetNeighbors(Hex hex)
    {
        return isInitialized ? hexMap.GetNeighbors(hex) : new List<Hex>();
    }

    public int GetDistance(Hex a, Hex b)
    {
        Debug.Log($"A exists: {a!=null}, B exists: {b!=null}");
        return isInitialized ? hexMap.GetDistance(a, b) : 0;
    }

    public List<Hex> FindPath(Hex start, Hex end)
    {
        return isInitialized ? hexMap.FindPath(start, end) : new List<Hex>();
    }

    public List<Hex> GetReachableHexes(Hex start, int range)
    {
        return isInitialized ? hexMap.GetReachableHexes(start, range) : new List<Hex>();
    }

    public Hex GetHex(Vector2Int coords)
    {
        return isInitialized && hexMap.hexes.TryGetValue(coords, out Hex hex) ? hex : null;
    }

    public Dictionary<Vector2Int, Hex> GetAllHexes()
    {
        return isInitialized ? hexMap.hexes : new Dictionary<Vector2Int, Hex>();
    }

    // Utility method to check if a hex can be reached with current movement
    public bool CanReachHex(Hex start, Hex target, int movementPoints)
    {
        if (start == null || target == null) return false;

        var reachableHexes = GetReachableHexesWithMovement(start, movementPoints);
        return reachableHexes.Contains(target);
    }

    // Get movement cost for a path
    public int GetPathMovementCost(List<Hex> path)
    {
        int totalCost = 0;
        for (int i = 1; i < path.Count; i++) // Skip first hex (starting position)
        {
            totalCost += path[i].MovementCost;
        }
        return totalCost;
    }

    // Debug method to check highlighting system
    public void DiagnoseHighlightingSystem()
    {
        Debug.Log($"=== HIGHLIGHTING SYSTEM DIAGNOSIS ===");
        Debug.Log($"Total highlighted hexes: {highlightedHexes.Count}");
        Debug.Log($"Total stored original colors: {originalColors.Count}");

        foreach (var hex in highlightedHexes)
        {
            var spriteRenderer = hex.GetComponent<SpriteRenderer>();
            var childRenderer = hex.GetComponentInChildren<SpriteRenderer>();

            Debug.Log($"Hex ({hex.Q}, {hex.R}): " +
                      $"HasSpriteRenderer={spriteRenderer != null}, " +
                      $"HasChildRenderer={childRenderer != null}, " +
                      $"CurrentColor={spriteRenderer?.color ?? Color.clear}, " +
                      $"HasStoredColor={originalColors.ContainsKey(hex)}");
        }
        Debug.Log($"=== END DIAGNOSIS ===");
    }

    // Test method to verify highlighting works
    public void TestHighlighting()
    {
        Debug.Log("=== TESTING HIGHLIGHTING ===");

        // Find first hex and try to highlight it
        if (hexMap.hexes.Count > 0)
        {
            var firstHex = hexMap.hexes.Values.First();
            Debug.Log($"Testing with hex ({firstHex.Q}, {firstHex.R})");

            var spriteRenderer = firstHex.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                Debug.Log($"Original color: {spriteRenderer.color}");
                spriteRenderer.color = Color.red;
                Debug.Log($"Set to red: {spriteRenderer.color}");

                // Wait a frame then restore
                spriteRenderer.color = Color.white;
                Debug.Log($"Restored to white: {spriteRenderer.color}");
            }
            else
            {
                Debug.LogError("No SpriteRenderer found for testing!");
            }
        }
        else
        {
            Debug.LogError("No hexes available for testing!");
        }
    }
}