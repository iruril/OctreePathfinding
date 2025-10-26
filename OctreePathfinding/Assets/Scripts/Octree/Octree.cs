using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class Octree
    {
        public OctreeNode root;
        public Bounds bounds;
        public Graph graph;

        List<OctreeNode> emptyLeaves = new();

        public Octree(GameObject[] worldObjects, float minNodeSize, Graph graph)
        {
            this.graph = graph;

            CalculateBounds(worldObjects);
            CreateTree(worldObjects, minNodeSize);

            GetEmptyLeaves(root);
        }

        void GetEmptyLeaves(OctreeNode node)
        {
            if(node.IsLeaf && node.objects.Count == 0)
            {
                emptyLeaves.Add(node);
                graph.AddNode(node);
                return;
            }

            if (node.children == null) return;

            foreach (OctreeNode child in node.children)
            {
                GetEmptyLeaves(child);
            }

            for (int i = 0; i < node.children.Length; i++)
            {
                for (int j = i + 1; j < node.children.Length; j++)
                {
                    graph.AddEdge(node.children[i], node.children[j]);
                }
            }
        }

        void CreateTree(GameObject[] worldObjects, float minNodeSize)
        {
            root = new OctreeNode(bounds, minNodeSize);

            foreach(var obj in worldObjects)
            {
                root.Divide(obj);
            }
        }

        void CalculateBounds(GameObject[] worldObjects)
        {
            foreach (var obj in worldObjects)
            {
                bounds.Encapsulate(obj.GetComponent<Collider>().bounds);
            }

            Vector3 size = Vector3.one * Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.5f;
            bounds.SetMinMax(bounds.center - size, bounds.center + size);
        }
    }
}