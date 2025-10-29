using System.Collections.Generic;
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

    public partial class Graph
    {
        public readonly Dictionary<OctreeNode, Node> nodes = new();
        public readonly HashSet<Edge> edges = new();
        
        public void AddNode(OctreeNode octreeNode)
        {
            if (!nodes.ContainsKey(octreeNode))
            {
                nodes.Add(octreeNode, new Node(octreeNode));
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
    }
}