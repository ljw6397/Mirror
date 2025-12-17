using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Greedy : MonoBehaviour
{
    [System.Serializable]
    public class Stone
    {
        public string name;
        public int exp;
        public int price;

        public float Efficiency => (float)exp / price;
    }

    public class Result
    {
        public Dictionary<string, int> buyList = new();
        public int totalExp;
        public int totalGold;

        public int WasteExp => totalExp;

        public void Add(Stone stone, int count)
        {
            if (!buyList.ContainsKey(stone.name))
                buyList[stone.name] = 0;

            buyList[stone.name] += count;
            totalExp += stone.exp * count;
            totalGold += stone.price * count;
        }
    }

    List<Stone> stones = new()
    {
        new Stone { name = "소", exp = 3, price = 8 },
        new Stone { name = "중", exp = 5, price = 12 },
        new Stone { name = "대", exp = 12, price = 30 },
        new Stone { name = "특대", exp = 20, price = 45 }
    };

    int GetNeedExp(int level)
    {
        return 8 * level * level;
    }
    public Result BruteForce(int targetLevel)
    {
        int needExp = GetNeedExp(targetLevel);
        Result best = null;

        int maxCount = needExp / stones.Min(s => s.exp) + 2;

        for (int a = 0; a <= maxCount; a++)
            for (int b = 0; b <= maxCount; b++)
                for (int c = 0; c <= maxCount; c++)
                    for (int d = 0; d <= maxCount; d++)
                    {
                        int exp =
                            a * stones[0].exp +
                            b * stones[1].exp +
                            c * stones[2].exp +
                            d * stones[3].exp;

                        if (exp < needExp) continue;

                        int gold =
                            a * stones[0].price +
                            b * stones[1].price +
                            c * stones[2].price +
                            d * stones[3].price;

                        if (best == null || gold < best.totalGold)
                        {
                            best = new Result();
                            best.Add(stones[0], a);
                            best.Add(stones[1], b);
                            best.Add(stones[2], c);
                            best.Add(stones[3], d);
                        }
                    }

        return best;
    }
    Result Greedy1(int targetLevel, List<Stone> sorted)
    {
        int needExp = GetNeedExp(targetLevel);
        Result result = new();

        foreach (var stone in sorted)
        {
            int count = needExp / stone.exp;
            if (count > 0)
            {
                result.Add(stone, count);
                needExp -= stone.exp * count;
            }
        }

        if (needExp > 0)
        {
            Stone small = stones[0];
            int count = Mathf.CeilToInt((float)needExp / small.exp);
            result.Add(small, count);
        }

        return result;
    }
    public Result Greedy_MinWaste(int level)
    {
        return Greedy1(level, stones.OrderByDescending(s => s.exp).ToList());
    }
    public Result Greedy_MaxEfficiency(int level)
    {
        return Greedy1(level, stones.OrderByDescending(s => s.Efficiency).ToList());
    }

    public Result Greedy_LargeFirst(int level)
    {
        return Greedy1(level, stones.OrderByDescending(s => s.exp).ToList());
    }
    void Start()
    {
        int level = 3;

        Print("BruteForce", BruteForce(level));
        Print("MinWaste", Greedy_MinWaste(level));
        Print("Efficiency", Greedy_MaxEfficiency(level));
        Print("LargeFirst", Greedy_LargeFirst(level));
    }

    void Print(string title, Result r)
    {
        Debug.Log($"[{title}] Gold:{r.totalGold} Exp:{r.totalExp}");
        foreach (var kv in r.buyList)
            Debug.Log($"{kv.Key} x {kv.Value}");
    }
}
