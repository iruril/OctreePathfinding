using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace Octrees
{
    public partial class Graph
    {
        int maxIterations; 
        public void FinalizeGraph()
        {
            maxIterations = nodes.Count * 3;
            bakedNodes = nodes.Keys.ToArray();
        }

        public bool AStar(Node start, Node end, ref List<Node> path, PathfindingContext ctx)
        {
            path.Clear();
            ctx.BeginNewSearch();

            ctx.Activate(start.id);
            ctx.g[start.id] = 0;
            ctx.h[start.id] = Heuristic(start.octreeNode.bounds.center, end.octreeNode.bounds.center);
            ctx.f[start.id] = ctx.h[start.id]; 
            ctx.openQueue.Enqueue(start, ctx.f[start.id]);

            int iterations = 0; 
            Node bestSoFar = start;
            float bestDistance = ctx.h[start.id];

            while (ctx.openQueue.Count > 0)
            {
                if (++iterations > maxIterations)
                {
                    ReconstructPath(bestSoFar, ref path, ctx.from);
                    return false;
                }

                Node current = ctx.openQueue.Dequeue();
                if (ctx.closed[current.id]) continue; //이미 처리된 노드가 나올 수 있으므로 스킵 처리

                if (current == end)
                {
                    ReconstructPath(current, ref path, ctx.from);
                    return true;
                }

                ctx.closed[current.id] = true; 

                float distToEnd = Heuristic(current.octreeNode.bounds.center, end.octreeNode.bounds.center);
                if (distToEnd < bestDistance)
                {
                    bestDistance = distToEnd;
                    bestSoFar = current;
                }

                foreach (var edge in current.edges)
                {
                    Node neighbor = edge.x == current ? edge.y : edge.x;

                    if (!ctx.IsActive(neighbor.id))
                        ctx.Activate(neighbor.id);

                    if (ctx.closed[neighbor.id]) continue;

                    float tentativeG = ctx.g[current.id] + Heuristic(current.octreeNode.bounds.center, neighbor.octreeNode.bounds.center);
                    if (tentativeG < ctx.g[neighbor.id])
                    {
                        ctx.from[neighbor.id] = current;
                        ctx.g[neighbor.id] = tentativeG;
                        ctx.h[neighbor.id] = Heuristic(neighbor.octreeNode.bounds.center, end.octreeNode.bounds.center);
                        ctx.f[neighbor.id] = ctx.g[neighbor.id] + ctx.h[neighbor.id];
                        ctx.openQueue.Enqueue(neighbor, ctx.f[neighbor.id]);
                    }
                }
            }

            //Debug.Log($"[A Star] Fail - no path : {end.octreeNode.bounds.center}");
            ReconstructPath(bestSoFar, ref path, ctx.from);
            return false;
        }

        void ReconstructPath(Node node, ref List<Node> path, Node[] from)
        {
            while (node != null)
            {
                path.Add(node);
                node = from[node.id];
            }
            path.Reverse();
        }

        float Heuristic(float3 a, float3 b) => math.distance(a, b);

        public Node FindNode(OctreeNode octreeNode)
        {
            nodes.TryGetValue(octreeNode, out Node node);
            return node;
        }
    }
}
