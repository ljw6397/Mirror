using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Si : MonoBehaviour
{
    public int width = 21;
    public int height = 21;
    public GameObject wallPrefab;
    public GameObject floorPrefab;
    public GameObject pathPrefab; 

    int[,] map;
    bool[,] visited;
    List<Vector2Int> path = new List<Vector2Int>();
    Vector2Int goal;
    Vector2Int[] dirs = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
    System.Random rand = new System.Random();

    void Start()
    {
        RegenerateMaze();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            RegenerateMaze();

        if (Input.GetKeyDown(KeyCode.R))
            ShowPath();
    }

    void RegenerateMaze()
    {
        ClearOldObjects();
        GenerateMaze();

        visited = new bool[height, width];
        path.Clear();
        bool ok = SearchMaze(1, 1);

        if (!ok)
        {
            RegenerateMaze();
            return;
        }

        VisualizeMaze();
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
                map[y, x] = 1;

        MakePath(1, 1);

        float noiseRate = 0.18f; 
        for (int y = 1; y < height - 1; y++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                if (map[y, x] == 0 && rand.NextDouble() < noiseRate)
                {
                    map[y, x] = 1;
                }
            }
        }
        goal = new Vector2Int(width - 2, height - 2);

    }

    void MakePath(int x, int y, int depth = 0)
    {
        if (depth > width * height) return;

        map[y, x] = 0;
        Vector2Int[] shuffledDirs = (Vector2Int[])dirs.Clone();
        for (int i = 0; i < shuffledDirs.Length; i++)
        {
            int r = rand.Next(i, shuffledDirs.Length);
            (shuffledDirs[i], shuffledDirs[r]) = (shuffledDirs[r], shuffledDirs[i]);
        }

        foreach (var d in shuffledDirs)
        {
            int nx = x + d.x * 2;
            int ny = y + d.y * 2;

            if (ny <= 0 || ny >= height - 1 || nx <= 0 || nx >= width - 1)
                continue;

            if (map[ny, nx] == 1 && rand.NextDouble() < 0.8) 
            {
                map[y + d.y, x + d.x] = 0;
                MakePath(nx, ny, depth + 1);
            }
        }
        if (rand.NextDouble() < 0.15) 
        {
            int radius = rand.Next(1, 3); 
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int gx = x + dx;
                    int gy = y + dy;
                    if (gx > 0 && gy > 0 && gx < width - 1 && gy < height - 1)
                        map[gy, gx] = 0; 
                }
            }
        }

    }

    bool SearchMaze(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return false;
        if (map[y, x] == 1 || visited[y, x]) return false;

        visited[y, x] = true;
        path.Add(new Vector2Int(x, y));

        if (x == goal.x && y == goal.y)
            return true;

        foreach (var d in dirs)
            if (SearchMaze(x + d.x, y + d.y))
                return true;

        path.RemoveAt(path.Count - 1);
        return false;
    }

    void VisualizeMaze()
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                GameObject obj = map[y, x] == 1 ? wallPrefab : floorPrefab;
                Instantiate(obj, new Vector3(x, 0, y), Quaternion.identity, transform);
            }
        }
    }

    void ShowPath()
    {
        foreach (var p in path)
        {
            Instantiate(pathPrefab, new Vector3(p.x, 0.5f, p.y), Quaternion.identity, transform);
        }
    }
}
