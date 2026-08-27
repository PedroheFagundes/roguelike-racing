using System.Collections.Generic;
using UnityEngine;

namespace RoguelikeRacing.Race
{
    /// <summary>Picks up to count distinct random elements from a list, without repeats.</summary>
    public static class RandomPick
    {
        public static List<T> Distinct<T>(IReadOnlyList<T> source, int count)
        {
            var pool = new List<T>(source);
            int actualCount = Mathf.Min(count, pool.Count);
            var picked = new List<T>(actualCount);

            for (int i = 0; i < actualCount; i++)
            {
                int index = Random.Range(0, pool.Count);
                picked.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return picked;
        }
    }
}
