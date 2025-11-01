using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public class PathOptimizer
    {
        private static readonly RaycastHit[] _hits = new RaycastHit[1];
        public static List<Node> Simplify(List<Node> path, LayerMask obstacleMask)
        {
            if (path == null || path.Count < 2)
                return path;

            List<Node> optimized = new(path.Count);
            int mask = obstacleMask.value;

            int n = 0;
            optimized.Add(path[n]);

            while (n < path.Count - 2)
            {
                Vector3 from = path[n].octreeNode.bounds.center;
                int t;
                for (t = n + 2; t < path.Count; t++)
                {
                    Vector3 to = path[t].octreeNode.bounds.center;

                    if (HasObstacle(from, to, mask))
                    {
                        n = t - 1;
                        optimized.Add(path[n]);
                        break;
                    }

                    if (t == path.Count - 1)
                    {
                        optimized.Add(path[t]);
                        n = t;
                        break;
                    }
                }
            }

            return optimized;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasObstacle(Vector3 from, Vector3 to, int mask)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            dir /= dist;
            return Physics.RaycastNonAlloc(from, dir, _hits, dist, mask) > 0;
        }
    }
}
