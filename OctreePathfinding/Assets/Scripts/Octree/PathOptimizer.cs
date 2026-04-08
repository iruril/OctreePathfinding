using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public static class PathOptimizer
    {
        private static readonly RaycastHit[] _hits = new RaycastHit[1];

        public static void Simplify(List<Node> originalPath, List<Node> resultPath, LayerMask obstacleMask)
        {
            resultPath.Clear(); // 에이전트의 기존 경로 비우기

            if (originalPath == null || originalPath.Count == 0)
                return;

            // 경로가 짧으면 그대로 복사하고 종료
            if (originalPath.Count < 2)
            {
                resultPath.AddRange(originalPath);
                return;
            }

            int mask = obstacleMask.value;
            int n = 0;
            resultPath.Add(originalPath[n]);

            while (n < originalPath.Count - 2)
            {
                Vector3 from = originalPath[n].octreeNode.bounds.center;
                int t;
                for (t = n + 2; t < originalPath.Count; t++)
                {
                    Vector3 to = originalPath[t].octreeNode.bounds.center;

                    if (HasObstacle(from, to, mask))
                    {
                        n = t - 1;
                        resultPath.Add(originalPath[n]);
                        break;
                    }

                    if (t == originalPath.Count - 1)
                    {
                        resultPath.Add(originalPath[t]);
                        n = t;
                        break;
                    }
                }
            }
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