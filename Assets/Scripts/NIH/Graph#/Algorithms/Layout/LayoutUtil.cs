using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace GraphSharp.Algorithms.Layout
{
	[DebuggerDisplay("First = {First}, Second = {Second}")]
	public class Pair
	{
		public int First;
		public int Second;
        public int Weight = 1;
	}

	public static class LayoutUtil
	{

		public static int BiLayerCrossCount(IEnumerable<Pair> pairs, int firstLayerVertexCount, int secondLayerVertexCount)
        {
            if (pairs == null)
                return 0;

            //radix sort of the pair, order by First asc, Second asc

            #region Sort by Second ASC
            var radixBySecond = new List<Pair>[secondLayerVertexCount];
            List<Pair> r;
            int pairCount = 0;
            foreach (var pair in pairs)
            {
                //get the radix where the pair should be inserted
                r = radixBySecond[pair.Second];
                if (r == null)
                {
                    r = new List<Pair>();
                    radixBySecond[pair.Second] = r;
                }
                r.Add(pair);
                pairCount++;
            }
            #endregion

            #region Sort By First ASC
            var radixByFirst = new List<Pair>[firstLayerVertexCount];
            foreach (var list in radixBySecond)
            {
                if (list == null)
                    continue;

                foreach (var pair in list)
                {
                    //get the radix where the pair should be inserted
                    r = radixByFirst[pair.First];
                    if (r == null)
                    {
                        r = new List<Pair>();
                        radixByFirst[pair.First] = r;
                    }
                    r.Add(pair);
                }
            }
            #endregion

            //
            // Build the accumulator tree
            //
            int firstIndex = 1;
            while (firstIndex < pairCount)
                firstIndex *= 2;
            int treeSize = 2 * firstIndex - 1;
            firstIndex -= 1;
            int[] tree = new int[treeSize];

            //
            // Count the crossings
            //
            int crossCount = 0;
            int index;
            foreach (var list in radixByFirst)
            {
                if (list == null)
                    continue;

                foreach (var pair in list)
                {
                    index = pair.Second + firstIndex;
                    tree[index] += pair.Weight;
                    while (index > 0)
                    {
                        if (index % 2 > 0)
                            crossCount += tree[index + 1] * pair.Weight;
                        index = (index - 1) / 2;
                        tree[index] += pair.Weight;
                    }
                }
            }

            return crossCount;
        }

        public static float2 GetClippingPoint(float2 size, float2 s, float2 t)
        {
            float[] sides = new float[4];
            sides[0] = (s.x - size.x / 2.0f - t.x) / (s.x - t.x);
            sides[1] = (s.y - size.y / 2.0f - t.y) / (s.y - t.y);
            sides[2] = (s.x + size.x / 2.0f - t.x) / (s.x - t.x);
            sides[3] = (s.y + size.y / 2.0f - t.y) / (s.y - t.y);

            float fi = 0;
            for (int i = 0; i < 4; i++)
            {
                if (sides[i] <= 1)
                    fi = max(fi, sides[i]);
            }
            if (fi == 0)
            {
                fi = float.PositiveInfinity;
                for (int i = 0; i < 4; i++)
                    fi = min(fi, Math.Abs(sides[i]));
                fi *= -1;
            }

            return t + fi * (s - t);
        }


        public static bool IsSameDirection(float2 a, float2 b)
        {
            return Math.Sign(a.x) == Math.Sign(b.x) && Math.Sign(a.y) == Math.Sign(b.y);
        }

        public static int BiLayerCrossCount(List<Pair> edgePairs)
        {
            int[] firsts = edgePairs.Select(e => e.First).Distinct().OrderBy(f => f).ToArray();
            int[] seconds = edgePairs.Select(e => e.Second).Distinct().OrderBy(f => f).ToArray();
            Dictionary<int, int> firstMap = new Dictionary<int, int>(firsts.Length);
            Dictionary<int, int> secondMap = new Dictionary<int, int>(seconds.Length);
            for (int i = 0; i < firsts.Length; i++)
            {
                firstMap.Add(firsts[i], i);
            }
            for (int i = 0; i < seconds.Length; i++)
            {
                secondMap.Add(seconds[i], i);
            }
            foreach (var pair in edgePairs)
            {
                pair.First = firstMap[pair.First];
                pair.Second = secondMap[pair.Second];
            }
            return BiLayerCrossCount(edgePairs, firsts.Length, seconds.Length);
        }
    }


}