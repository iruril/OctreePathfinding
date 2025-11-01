using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public class PathOptimizer
    {
        public static List<Node> Simplify(List<Node> path, float size, LayerMask obstacleMask)
        {
            if (path == null || path.Count < 2)
                return path;

            List<Node> optimized = new(path.Count);
            int mask = obstacleMask.value;

            int n = 0;
            optimized.Add(path[n]);

            while (n < path.Count - 1)
            {
                int t = n + 1;
                Vector3 from = path[n].octreeNode.bounds.center;

                for (; t < path.Count; t++)
                {
                    Vector3 to = path[t].octreeNode.bounds.center;

                    if (HasObstacle(from, to, size, mask))
                    {
                        n = t - 1;
                        optimized.Add(path[n]);
                        break;
                    }

                    if (t == path.Count - 1)
                    {
                        optimized.Add(path[t]);
                        n = t;
                    }
                }

                if (t >= path.Count)
                    break;
            }

            return optimized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasObstacle(Vector3 from, Vector3 to, float size, int mask)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            dir /= dist;
            return Physics.SphereCast(from, size, dir, out var hitInfo, dist, mask);
        }
    }
}
