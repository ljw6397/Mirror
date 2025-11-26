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
    bool isMoving = false;
    private Transform player;

    float noiseRate = 0.18f;

    int[,] map;                    
    List<Vector2Int> path = new List<Vector2Int>();
    Vector2Int goal;
    System.Random rand = new System.Random();

    Vector2Int[] dirs =
    {
        new Vector2Int(1,0),
        new Vector2Int(-1,0),
        new Vector2Int(0,1),
        new Vector2Int(0,-1),
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

        // 기본 전체 벽
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                map[y, x] = 0;

        // DFS 미로 만들기
        MakePath(1, 1);

        // 방 생성 확률 확 줄임
        for (int y = 2; y < height - 2; y++)
        {
            for (int x = 2; x < width - 2; x++)
            {
                if (rand.NextDouble() < 0.04)  
                {
                    int radius = rand.Next(2, 3); 
                    for (int dy = -radius; dy <= radius; dy++)
                    {
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
        map[y, x] = 1; // 길

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
    public void ShowPath()
    {
        foreach (var p in path)
            Instantiate(pathPrefab, new Vector3(p.x, 0.5f, p.y), Quaternion.identity, transform);

        StartMove(); // <<--- 플레이어 경로 이동 실행
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

    public void StartMove() { if (!isMoving) StartCoroutine(MoveAlongPath()); }

    // 탈출 가능한지 확인 DFS
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

    // 시각화
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

                float posY = (map[y, x] == 0) ? 0.5f : 0f;  

                Instantiate(prefab, new Vector3(x, posY, y), Quaternion.identity, transform);
            }
        }
    }
    // 버튼 R 누르면 Dijkstra 최단 경로 시각화
    public void ShowShortestPath()
    {
        path = Dijkstra(new Vector2Int(1, 1), goal);
        foreach (var p in path)
            Instantiate(pathPrefab, new Vector3(p.x, 0.5f, p.y), Quaternion.identity, transform);
    }

    // Dijkstra
    List<Vector2Int> Dijkstra(Vector2Int start, Vector2Int goal)
    {
        int[,] dist = new int[height, width];
        bool[,] visited = new bool[height, width];
        Vector2Int?[,] parent = new Vector2Int?[height, width];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                dist[y, x] = int.MaxValue;

        dist[start.y, start.x] = 0;

        SimplePriorityQueue<Vector2Int> pq = new SimplePriorityQueue<Vector2Int>();
        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            Vector2Int cur = pq.Dequeue();
            if (visited[cur.y, cur.x]) continue;

            visited[cur.y, cur.x] = true;

            if (cur == goal) break;

            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;

                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                if (map[ny, nx] == 0) continue;

                int newDist = dist[cur.y, cur.x] + TileCost(map[ny, nx]);

                if (newDist < dist[ny, nx])
                {
                    dist[ny, nx] = newDist;
                    parent[ny, nx] = cur;
                    pq.Enqueue(new Vector2Int(nx, ny), newDist);
                }
            }
        }

        return ReconstructPath(parent, start, goal);
    }

    List<Vector2Int> ReconstructPath(Vector2Int?[,] parent, Vector2Int start, Vector2Int goal)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int? cur = goal;

        while (cur.HasValue)
        {
            path.Add(cur.Value);
            if (cur.Value == start) break;
            cur = parent[cur.Value.y, cur.Value.x];
        }

        path.Reverse();
        return path;
    }

    int TileCost(int tile)
    {
        return tile == 1 ? 1 :
               tile == 2 ? 3 :
               tile == 3 ? 5 :
               int.MaxValue;
    }
}
