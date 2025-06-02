using System.Collections.Generic;
using UnityEngine;

public class HexMapGenerator : MonoBehaviour
{
    public GameObject[] hexPrefabs;
    public int width = 10;
    public int height = 10;
    public float hexSize = 1f;

    public Dictionary<Vector2Int, Hex> hexes = new Dictionary<Vector2Int, Hex>();

    // Kierunki s¹siadów w uk³adzie axial
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(1, 0), new Vector2Int(1, -1), new Vector2Int(0, -1),
        new Vector2Int(-1, 0), new Vector2Int(-1, 1), new Vector2Int(0, 1)
    };

    void Start()
    {
        GenerateMap();
    }

    void GenerateMap()
    {
        for (int q = -width / 2; q <= width / 2; q++)
        {
            int r1 = Mathf.Max(-height / 2, -q - height / 2);
            int r2 = Mathf.Min(height / 2, -q + height / 2);
            for (int r = r1; r <= r2; r++)
            {
                Vector3 position = HexToWorld(q, r);
                GameObject prefab = hexPrefabs[Random.Range(0, hexPrefabs.Length)];
                GameObject hexGO = Instantiate(prefab, position, Quaternion.identity, this.transform);

                Hex hex = hexGO.GetComponent<Hex>();
                hex.Init(q, r);

                hexes[new Vector2Int(q, r)] = hex;
            }
        }
    }

    public Vector3 HexToWorld(int q, int r)
    {
        float x = hexSize * Mathf.Sqrt(3f) * (q + r / 2f);
        float y = hexSize * 1.5f * r;
        return new Vector3(x, y, 0);
    }

    public List<Hex> GetNeighbors(Hex hex)
    {
        List<Hex> neighbors = new List<Hex>();
        foreach (var dir in directions)
        {
            Vector2Int neighborCoords = hex.AxialCoords + dir;
            if (hexes.TryGetValue(neighborCoords, out Hex neighbor))
            {
                neighbors.Add(neighbor);
            }
        }
        return neighbors;
    }

    public int GetDistance(Hex a, Hex b)
    {
        return Mathf.Max(Mathf.Abs(a.Q - b.Q), Mathf.Abs(a.R - b.R), Mathf.Abs(a.S - b.S));
    }

    public List<Hex> FindPath(Hex start, Hex goal)
    {
        var frontier = new PriorityQueue<Hex>();
        frontier.Enqueue(start, 0);

        var cameFrom = new Dictionary<Hex, Hex>();
        var costSoFar = new Dictionary<Hex, int>();
        cameFrom[start] = null;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (Hex neighbor in GetNeighbors(current))
            {
                if (neighbor.isObstacle)
                    continue;

                int newCost = costSoFar[current] + neighbor.movementCost;
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    int priority = newCost + GetDistance(neighbor, goal);
                    frontier.Enqueue(neighbor, priority);
                    cameFrom[neighbor] = current;
                }
            }
        }

        List<Hex> path = new List<Hex>();
        Hex currentHex = goal;
        while (currentHex != null)
        {
            path.Add(currentHex);
            cameFrom.TryGetValue(currentHex, out currentHex);
        }
        path.Reverse();
        return path;
    }

    public List<Hex> GetReachableHexes(Hex start, int maxMovement)
    {
        var reachable = new List<Hex>();
        var frontier = new Queue<Hex>();
        var costSoFar = new Dictionary<Hex, int>();

        frontier.Enqueue(start);
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            Hex current = frontier.Dequeue();
            foreach (Hex neighbor in GetNeighbors(current))
            {
                if (neighbor.isObstacle)
                    continue;

                int newCost = costSoFar[current] + neighbor.movementCost;
                if (newCost <= maxMovement && (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor]))
                {
                    costSoFar[neighbor] = newCost;
                    frontier.Enqueue(neighbor);
                    reachable.Add(neighbor);
                }
            }
        }

        return reachable;
    }

    public List<Hex> GetLine(Hex start, Hex end)
    {
        int N = GetDistance(start, end);
        List<Hex> results = new List<Hex>();
        for (int i = 0; i <= N; i++)
        {
            float t = N == 0 ? 0f : (float)i / N;
            Vector3 lerp = Vector3.Lerp(HexToWorld(start.q, start.r), HexToWorld(end.q, end.r), t);
            Vector2Int coords = WorldToHex(lerp);
            if (hexes.TryGetValue(coords, out Hex hex))
            {
                results.Add(hex);
            }
        }
        return results;
    }

    public Vector2Int WorldToHex(Vector3 position)
    {
        float q = (Mathf.Sqrt(3f) / 3f * position.x - 1f / 3f * position.y) / hexSize;
        float r = (2f / 3f * position.y) / hexSize;
        return CubeRound(q, r);
    }

    Vector2Int CubeRound(float q, float r)
    {
        float s = -q - r;
        int rq = Mathf.RoundToInt(q);
        int rr = Mathf.RoundToInt(r);
        int rs = Mathf.RoundToInt(s);

        float q_diff = Mathf.Abs(rq - q);
        float r_diff = Mathf.Abs(rr - r);
        float s_diff = Mathf.Abs(rs - s);

        if (q_diff > r_diff && q_diff > s_diff)
        {
            rq = -rr - rs;
        }
        else if (r_diff > s_diff)
        {
            rr = -rq - rs;
        }
        else
        {
            rs = -rq - rr;
        }

        return new Vector2Int(rq, rr);
    }
}
