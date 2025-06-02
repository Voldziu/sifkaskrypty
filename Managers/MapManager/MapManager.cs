using System.Collections.Generic;
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

    [Header("Events")]
    public UnityEvent<Hex> onHexSelected = new UnityEvent<Hex>();
    public UnityEvent<Hex> onHexDeselected = new UnityEvent<Hex>();

    private Hex selectedHex;
    private List<Hex> highlightedHexes = new List<Hex>();
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
        if (Input.GetMouseButtonDown(0))
        {
            Hex clickedHex = GetHexUnderMouse();
            if (clickedHex != null)
            {
                SelectHex(clickedHex);
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            Hex clickedHex = GetHexUnderMouse();
            if (clickedHex != null && selectedHex != null)
            {
                HighlightPath(selectedHex, clickedHex);
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (selectedHex != null)
            {
                HighlightRange(selectedHex, 3);
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectHex();
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
        foreach (Hex hex in highlightedHexes)
        {
            ResetHexColor(hex);
        }
        highlightedHexes.Clear();
    }

    void HighlightHex(Hex hex, Color color)
    {
        var spriteRenderer = hex.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
            if (!highlightedHexes.Contains(hex))
                highlightedHexes.Add(hex);
        }
    }

    void ResetHexColor(Hex hex)
    {
        var spriteRenderer = hex.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
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
        if (!isInitialized || start == null) return;

        ClearHighlights();

        // Keep selected hex highlighted
        if (selectedHex != null)
            HighlightHex(selectedHex, selectedColor);

        List<Hex> reachable = GetReachableHexes(start, range);
        foreach (Hex hex in reachable)
        {
            HighlightHex(hex, rangeColor);
        }
    }

    // Utility methods that delegate to HexMapGenerator
    public List<Hex> GetNeighbors(Hex hex)
    {
        return isInitialized ? hexMap.GetNeighbors(hex) : new List<Hex>();
    }

    public int GetDistance(Hex a, Hex b)
    {
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

    
}