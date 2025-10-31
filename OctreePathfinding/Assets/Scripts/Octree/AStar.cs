using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public partial class Graph
    {
        const int maxIterations = 10000;
        public bool AStar(Node start, Node end, ref List<Node> path, PathfindingContext ctx)
        {
            Debug.Log($"[A Star] Start");
            path.Clear();
            ctx.BeginNewSearch();

            var open = new SortedSet<Node>(new NodeComparer(ctx));

            ctx.Activate(start.id);
            ctx.g[start.id] = 0;
            ctx.h[start.id] = Heuristic(start, end);
            ctx.f[start.id] = ctx.h[start.id];
            open.Add(start);

            int iterations = 0;
            while (open.Count > 0)
            {
                if (++iterations > maxIterations)
                {
                    Debug.Log($"[A Star] Fail - exceed clamp");
                    return false;
                }

                Node current = open.Min;
                open.Remove(current);

                if (current == end)
                {
                    ReconstructPath(current, ref path, ctx.from);
                    return true;
                }

                ctx.closed[current.id] = true;

                foreach (var edge in current.edges)
                {
                    Node neighbor = edge.x == current ? edge.y : edge.x;
                    if (ctx.closed[neighbor.id]) continue;

                    if (!ctx.IsActive(neighbor.id))
                        ctx.Activate(neighbor.id);

                    float tentativeG = ctx.g[current.id] + Heuristic(current, neighbor);
                    if (tentativeG < ctx.g[neighbor.id])
                    {
                        ctx.from[neighbor.id] = current;
                        ctx.g[neighbor.id] = tentativeG;
                        ctx.h[neighbor.id] = Heuristic(neighbor, end);
                        ctx.f[neighbor.id] = ctx.g[neighbor.id] + ctx.h[neighbor.id];
                        open.Add(neighbor);
                    }
                }
            }

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

        float Heuristic(Node a, Node b) => (a.octreeNode.bounds.center - b.octreeNode.bounds.center).sqrMagnitude;

        public Node FindNode(OctreeNode octreeNode)
        {
            nodes.TryGetValue(octreeNode, out Node node);
            return node;
        }

        public class NodeComparer : IComparer<Node>
        {
            private readonly PathfindingContext ctx; 
            public NodeComparer(PathfindingContext ctx)
            {
                this.ctx = ctx;
            }

            public int Compare(Node x, Node y)
            {
                if (x == null || y == null) return 0;

                int result = ctx.f[x.id].CompareTo(ctx.f[y.id]);
                if (result == 0)
                    return x.id.CompareTo(y.id);
                return result;
            }
        }
    }
}
