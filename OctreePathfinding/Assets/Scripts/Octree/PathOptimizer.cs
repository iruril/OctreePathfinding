using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public class PathOptimizer
    {
        private static readonly RaycastHit[] _hits = new RaycastHit[1];

        public static List<Node> Simplify(List<Node> path, float size, LayerMask obstacleMask, float dirThreshold = 0.995f)
        {
            if (path == null || path.Count < 2)
                return path;

            List<Node> simplified = new(path.Count);
            simplified.Add(path[0]);

            int n = 0;
            Vector3 lastDir = Vector3.zero;
            int mask = obstacleMask.value;

            while (n < path.Count - 1)
            {
                int lastVisible = n + 1;
                Vector3 from = path[n].octreeNode.bounds.center;

                for (int t = n + 1; t < path.Count; t++)
                {
                    Vector3 to = path[t].octreeNode.bounds.center;
                    Vector3 dir = (to - from).normalized;

                    if (t > n + 1 && Vector3.Dot(lastDir, dir) > dirThreshold)
                    {
                        lastVisible = t;
                        continue;
                    }

                    if (HasObstacle(from, to, size, mask))
                        break;

                    lastDir = dir;
                    lastVisible = t;
                }

                simplified.Add(path[lastVisible]);
                n = lastVisible;
            }

            return simplified;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasObstacle(Vector3 from, Vector3 to, float size, int mask)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            dir /= dist;
            return Physics.SphereCastNonAlloc(from, size, dir, _hits, dist, mask) > 0;
        }
    }
}
