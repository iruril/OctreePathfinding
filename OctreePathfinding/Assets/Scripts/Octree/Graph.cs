using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Octrees
{
    public class Node
    {
        static int nextId;
        public readonly int id;

        public float f, g, h;
        public Node from;

        public List<Edge> edges = new();

        public OctreeNode octreeNode;

        public Node(OctreeNode octreeNode)
        {
            this.id = nextId++;
            this.octreeNode = octreeNode;
        }

        public override bool Equals(object obj) => obj is Node other && other.id == id;
        public override int GetHashCode() => id.GetHashCode();
    }

    public class Edge
    {
        public readonly Node x, y;

        public Edge(Node x, Node y)
        {
            this.x = x;
            this.y = y;
        }

        public override bool Equals(object obj)
        {
            return obj is Edge other && ((x == other.x && y == other.y) || (x == other.y && y == other.x));
        }

        public override int GetHashCode() => x.GetHashCode() ^ y.GetHashCode();
    }

    public class Graph
    {
        public readonly Dictionary<OctreeNode, Node> nodes = new();
        public readonly HashSet<Edge> edges = new();

        List<Node> pathList = new();

        public int GetPathLength() => pathList.Count;

        public OctreeNode GetPathNode(int index)
        {
            if (pathList == null) return null;
            if(index < 0|| index >= pathList.Count)
            {
                Debug.LogError($"Index out of bounds. Path Legth : {GetPathLength()}, Index : {index}");
                return null;
            }
            return pathList[index].octreeNode;
        }
        
        public void AddNode(OctreeNode octreeNode)
        {
            if (!nodes.ContainsKey(octreeNode))
            {
                nodes.Add(octreeNode, new Node(octreeNode));
            }
        }

        const int maxIterations = 10000;

        public bool AStar(OctreeNode startNode, OctreeNode endNode)
        {
            pathList.Clear();
            Node start = FindNode(startNode);
            Node end = FindNode(endNode);

            if(start == null || end == null)
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
                if(++iterationCount > maxIterations)
                {
                    //Debug.LogError("A* exceeded maximun interations!!!");
                    return false;
                }

                Node current = openSet.First();
                openSet.Remove(current);

                if (current.Equals(end))
                {
                    ReconstructPath(current);
                    return true;
                }

                closedSet.Add(current);

                foreach(Edge edge in current.edges)
                {
                    Node neighbor = Equals(edge.x, current) ? edge.y : edge.x;

                    if(closedSet.Contains(neighbor)) continue;

                    float tentative_gScore = current.g + Heuristic(current, neighbor);

                    if(tentative_gScore < neighbor.g || !openSet.Contains(neighbor))
                    {
                        neighbor.g = tentative_gScore;
                        neighbor.h = Heuristic(neighbor, end);
                        neighbor.f = neighbor.g + neighbor.h;
                        neighbor.from = current;
                        openSet.Add(neighbor);
                    }
                }
            }
            //Debug.Log("No Path!!");
            return false;
        }

        void ReconstructPath(Node node)
        {
            while (node != null)
            {
                pathList.Add(node);
                node = node.from;
            }

            pathList.Reverse();
        }

        float Heuristic(Node a, Node b) => (a.octreeNode.bounds.center - b.octreeNode.bounds.center).sqrMagnitude;

        public class NodeComparer : IComparer<Node>
        {
            public int Compare(Node x, Node y)
            {
                if(x == null || y == null) return 0;

                int result = x.f.CompareTo(y.f);
                if(result == 0)
                {
                    return x.id.CompareTo(y.id);
                }
                return result;
            }
        }

        public void AddEdge(OctreeNode x, OctreeNode y)
        {
            Node nodeX = FindNode(x);
            Node nodeY = FindNode(y);

            if (nodeX == null || nodeY == null) return;

            Edge edge = new Edge(nodeX, nodeY);
            if (edges.Add(edge))
            {
                nodeX.edges.Add(edge);
                nodeY.edges.Add(edge);
            }
        }

        public void DrawGraph()
        {
            Gizmos.color = Color.red;
            foreach(Edge edge in edges)
            {
                Gizmos.DrawLine(edge.x.octreeNode.bounds.center, edge.y.octreeNode.bounds.center);
            }
            foreach(var node in nodes.Values)
            {
                Gizmos.DrawWireSphere(node.octreeNode.bounds.center, 0.2f);
            }
        }

        Node FindNode(OctreeNode octreeNode)
        {
            nodes.TryGetValue(octreeNode, out Node node);
            return node;
        }
    }
}