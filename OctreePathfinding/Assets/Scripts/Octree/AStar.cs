using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Octrees
{
    public partial class Graph
    {
        const int maxIterations = 5000;
        public bool AStar(OctreeNode startNode, OctreeNode endNode, ref List<Node> path)
        {
            path.Clear();
            Node start = FindNode(startNode);
            Node end = FindNode(endNode);

            if (start == null || end == null)
            {
                Debug.LogError($"Start or End node not found!!");
                return false;
            }

            SortedSet<Node> openSet = new(new NodeComparer());
            HashSet<Node> closedSet = new();
            int iterationCount = 0;

            start.g = 0;
            start.h = Heuristic(start, end);
            start.f = start.g + start.h;
            start.from = null;
            openSet.Add(start);

            while (openSet.Count > 0)
            {
                if (++iterationCount > maxIterations) return false;

                Node current = openSet.First();
                openSet.Remove(current);

                if (current.Equals(end))
                {
                    ReconstructPath(current, ref path);
                    return true;
                }

                closedSet.Add(current);

                foreach (Edge edge in current.edges)
                {
                    Node neighbor = Equals(edge.x, current) ? edge.y : edge.x;

                    if (closedSet.Contains(neighbor)) continue;

                    float tentative_gScore = current.g + Heuristic(current, neighbor);

                    if (tentative_gScore < neighbor.g || !openSet.Contains(neighbor))
                    {
                        neighbor.g = tentative_gScore;
                        neighbor.h = Heuristic(neighbor, end);
                        neighbor.f = neighbor.g + neighbor.h;
                        neighbor.from = current;
                        openSet.Add(neighbor);
                    }
                }
            }
            return false;
        }

        void ReconstructPath(Node node, ref List<Node> path)
        {
            while (node != null)
            {
                path.Add(node);
                node = node.from;
            }

            path.Reverse();
        }

        float Heuristic(Node a, Node b) => (a.octreeNode.bounds.center - b.octreeNode.bounds.center).sqrMagnitude;

        Node FindNode(OctreeNode octreeNode)
        {
            nodes.TryGetValue(octreeNode, out Node node);
            return node;
        }

        public class NodeComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                if (x == null || y == null) return 0;

                int result = x.f.CompareTo(y.f);
                if (result == 0)
                {
                    return x.id.CompareTo(y.id);
                }
                return result;
            }
        }
    }
}
