using System.Collections.Generic;
using UnityEngine;

public class HexMapTester : MonoBehaviour
{
    public HexMapGenerator hexMap;
    public Color neighborColor = Color.green;
    public Color pathColor = Color.cyan;
    public Color rangeColor = Color.yellow;

    private Hex selectedHex;
    private List<Hex> highlightedHexes = new List<Hex>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Hex clickedHex = GetHexUnderMouse();
            if (clickedHex != null)
            {
                selectedHex = clickedHex;
                HighlightNeighbors(clickedHex);
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
                HighlightRange(selectedHex, 3); // Przyk³adowy zasiêg ruchu
            }
        }
    }

    Hex GetHexUnderMouse()
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int coords = hexMap.WorldToHex(worldPos);
        hexMap.hexes.TryGetValue(coords, out Hex hex);
        return hex;
    }

    void ClearHighlights()
    {
        foreach (Hex hex in highlightedHexes)
        {
            hex.GetComponent<SpriteRenderer>().color = Color.white;
        }
        highlightedHexes.Clear();
    }

    void HighlightNeighbors(Hex hex)
    {
        ClearHighlights();
        List<Hex> neighbors = hexMap.GetNeighbors(hex);
        foreach (Hex neighbor in neighbors)
        {
            neighbor.GetComponent<SpriteRenderer>().color = neighborColor;
            highlightedHexes.Add(neighbor);
        }
    }

    void HighlightPath(Hex start, Hex end)
    {
        ClearHighlights();
        List<Hex> path = hexMap.FindPath(start, end);
        foreach (Hex hex in path)
        {
            hex.GetComponent<SpriteRenderer>().color = pathColor;
            highlightedHexes.Add(hex);
        }
    }

    void HighlightRange(Hex start, int range)
    {
        ClearHighlights();
        List<Hex> reachable = hexMap.GetReachableHexes(start, range);
        foreach (Hex hex in reachable)
        {
            hex.GetComponent<SpriteRenderer>().color = rangeColor;
            highlightedHexes.Add(hex);
        }
    }
}

