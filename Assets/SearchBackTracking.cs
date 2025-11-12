using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchBackTracking : MonoBehaviour
{

    int[] cards = { 2, 3, 5, 7, 9 };

    int limit = 15;

    
    // Start is called before the first frame update
    void Start()
    {
        Search(0, new List<int>(), 0);
    }

    void Search(int i, List<int> list, int sum)
    {
        if (sum > limit) return;
        if(i == cards.Length)
        {
            Debug.Log($"{string.Join(",", list)} = {sum}");
            return;
        }
        list.Add(cards[i]);
        Search(i + 1, list, sum + cards[i]);
        list.RemoveAt(list.Count - 1);
        Search(i + 1, list, sum);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
