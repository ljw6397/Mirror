using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Si : MonoBehaviour
{
    public int width = 21;
    public int height = 21;

    public GameObject wallPrefab;
    public GameObject groundPrefab;
    public GameObject forestPrefab;
    public GameObject mudPrefab;
    public GameObject pathPrefab;
    public GameObject playerPrefab;

    private Transform player;
    private bool isMoving = false;

    int[,] map;
    List<Vector2Int> path = new List<Vector2Int>();
    Vector2Int goal;
    System.Random rand = new System.Random();

    Vector2Int[] dirs =
    {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1)
    };

    void Start()
    {
        Vector3 startPos = new Vector3(1, 0.5f, 1);
        player = Instantiate(playerPrefab, startPos, Quaternion.identity).transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            RegenerateMaze();

        if (Input.GetKeyDown(KeyCode.R))
            ShowShortestPath();
    }

    public void RegenerateMaze()
    {
        ClearOldObjects();
        GenerateMaze();

        bool ok = FindPathDFS(1, 1, new bool[height, width]);
        if (!ok)
        {
            RegenerateMaze();
            return;
        }

        Visualize();
    }

    void ClearOldObjects()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
    }

    void GenerateMaze()
    {
        map = new int[height, width];


        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[y, x] = 0;

        MakePath(1, 1);

        
        for (int y = 2; y < height - 2; y++)
        {
            for (int x = 2; x < width - 2; x++)
            {
                if (rand.NextDouble() < 0.04)
                {
                    int radius = rand.Next(2, 3);
                    for (int dy = -radius; dy <= radius; dy++)
                        for (int dx = -radius; dx <= radius; dx++)
                        {
                            int gx = x + dx;
                            int gy = y + dy;
                            if (gx > 0 && gy > 0 && gx < width - 1 && gy < height - 1)
                                map[gy, gx] = 1;
                        }
                }
            }
        }

        float noiseRate = 0.05f;
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (map[y, x] == 0 && rand.NextDouble() < noiseRate)
                    map[y, x] = 1;

      
        for (int y = 1; y < height - 1; y++)
            for (int x = 1; x < width - 1; x++)
                if (map[y, x] == 1)
                {
                    double r = rand.NextDouble();
                    if (r < 0.7) map[y, x] = 1;
                    else if (r < 0.9) map[y, x] = 2;
                    else map[y, x] = 3;
                }

        goal = new Vector2Int(width - 2, height - 2);
    }

    void MakePath(int x, int y)
    {
        map[y, x] = 1;

        Vector2Int[] shuffled = (Vector2Int[])dirs.Clone();
        for (int i = 0; i < shuffled.Length; i++)
        {
            int r = rand.Next(i, shuffled.Length);
            (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
        }

        foreach (var d in shuffled)
        {
            int nx = x + d.x * 2;
            int ny = y + d.y * 2;

            if (nx <= 0 || ny <= 0 || nx >= width - 1 || ny >= height - 1)
                continue;

            if (map[ny, nx] == 0)
            {
                map[y + d.y, x + d.x] = 1;
                MakePath(nx, ny);
            }
        }
    }
    bool FindPathDFS(int x, int y, bool[,] visited)
    {
        if (x == goal.x && y == goal.y)
            return true;

        visited[y, x] = true;

        foreach (var d in dirs)
        {
            int nx = x + d.x;
            int ny = y + d.y;

            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
            if (map[ny, nx] == 0 || visited[ny, nx]) continue;

            if (FindPathDFS(nx, ny, visited)) return true;
        }
        return false;
    }
    public void ShowShortestPath()
    {
        path = Astar(map, new Vector2Int(1, 1), goal);

        foreach (var p in path)
            Instantiate(pathPrefab, new Vector3(p.x, 0.5f, p.y), Quaternion.identity, transform);

        StartMove();
    }

    List<Vector2Int> Astar(int[,] map, Vector2Int start, Vector2Int goal)
    {
        int h = map.GetLength(0);
        int w = map.GetLength(1);

        int[,] gCost = new int[h, w];
        bool[,] visited = new bool[h, w];
        Vector2Int?[,] parent = new Vector2Int?[h, w];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                gCost[y, x] = int.MaxValue;

        gCost[start.y, start.x] = 0;

        List<Vector2Int> open = new List<Vector2Int>();
        open.Add(start);

        while (open.Count > 0)
        {
            int bestIndex = 0;
            int bestF = F(open[0], gCost, goal);

            for (int i = 1; i < open.Count; i++)
            {
                int f = F(open[i], gCost, goal);
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            Vector2Int cur = open[bestIndex];
            open.RemoveAt(bestIndex);

            if (visited[cur.y, cur.x]) continue;
            visited[cur.y, cur.x] = true;

            if (cur == goal)
                return Reconstruct(parent, start, goal);

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;

                if (!InBounds(nx, ny)) continue;
                if (map[ny, nx] == 0) continue;
                if (visited[ny, nx]) continue;

                int moveCost = TileCost(map[ny, nx]) + WallPenalty(nx, ny);
                int newG = gCost[cur.y, cur.x] + moveCost;

                if (newG < gCost[ny, nx])
                {
                    gCost[ny, nx] = newG;
                    parent[ny, nx] = cur;

                    Vector2Int next = new Vector2Int(nx, ny);
                    if (!open.Contains(next))
                        open.Add(next);
                }
            }
        }
        return null;
    }

    int F(Vector2Int pos, int[,] gCost, Vector2Int goal)
    {
        return gCost[pos.y, pos.x] + Manhattan(pos, goal);
    }

    int Manhattan(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    int TileCost(int tile)
    {
        return tile == 1 ? 1 :
               tile == 2 ? 3 :
               tile == 3 ? 5 :
               int.MaxValue;
    }

    int WallPenalty(int x, int y)
    {
        foreach (var d in dirs)
        {
            int nx = x + d.x;
            int ny = y + d.y;

            if (!InBounds(nx, ny)) continue;
            if (map[ny, nx] == 0)
                return 2;  
        }
        return 0;
    }

    bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }

    List<Vector2Int> Reconstruct(Vector2Int?[,] parent, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> p = new List<Vector2Int>();
        Vector2Int? cur = goal;

        while (cur.HasValue)
        {
            p.Add(cur.Value);
            if (cur.Value == start) break;
            cur = parent[cur.Value.y, cur.Value.x];
        }

        p.Reverse();
        return p;
    }
    void Visualize()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject prefab =
                    map[y, x] == 0 ? wallPrefab :
                    map[y, x] == 1 ? groundPrefab :
                    map[y, x] == 2 ? forestPrefab :
                    mudPrefab;

                float posY = map[y, x] == 0 ? 0.5f : 0f;

                Instantiate(prefab, new Vector3(x, posY, y), Quaternion.identity, transform);
            }
        }
    }

    public void StartMove()
    {
        if (!isMoving)
            StartCoroutine(MoveAlongPath());
    }

    IEnumerator MoveAlongPath()
    {
        isMoving = true;

        foreach (var p in path)
        {
            Vector3 targetPos = new Vector3(p.x, 0.5f, p.y);
            player.position = targetPos;

            yield return new WaitForSeconds(0.2f);
        }

        isMoving = false;
    }
}
