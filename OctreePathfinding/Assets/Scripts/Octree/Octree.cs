using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;
using System;

namespace Octrees
{
    public class Octree
    {
        //Busrt Compile 구조체
        [BurstCompile]
        struct BoundsIntersectJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Bounds> bounds;
            public NativeList<int2>.ParallelWriter resultPairs;

            public void Execute(int i)
            {
                Bounds a = bounds[i];
                for (int j = i + 1; j < bounds.Length; j++)
                {
                    if (a.Intersects(bounds[j]))
                    {
                        resultPairs.AddNoResize(new int2(i, j));
                    }
                }
            }
        }

        public OctreeNode root;
        public Bounds bounds;
        public Graph graph;

        List<OctreeNode> emptyLeaves = new();

        public Octree(GameObject[] worldObjects, float minNodeSize, Graph graph)
        {
            this.graph = graph;
            Debug.LogFormat("[{0:F3}s] [Octree] Bound Calc Start", Time.realtimeSinceStartup);
            CalculateBounds(worldObjects); 
            Debug.LogFormat("[{0:F3}s] [Octree] Bound Calc End", Time.realtimeSinceStartup);
            CreateTree(worldObjects, minNodeSize);
            Debug.LogFormat("[{0:F3}s] [Octree] Tree Created", Time.realtimeSinceStartup);

            //GetEmptyLeaves(root); 
            GetEmptyLeaves();
            Debug.LogFormat("[{0:F3}s] [Octree] Creating Graph with Empty Leaf Node", Time.realtimeSinceStartup);
            //BuildEdges();
            BuildEdgesWithJobs();
            Debug.LogFormat("[{0:F3}s] [Octree] Graph Created", Time.realtimeSinceStartup);
        }

        public OctreeNode FindClosestNode(Vector3 position) => FindClosestNode(root, position);

        public OctreeNode FindClosestNode(OctreeNode node, Vector3 position)
        {
            OctreeNode result = null;
            for(int i = 0; i < node.children.Length; i++)
            {
                if (node.children[i].bounds.Contains(position))
                {
                    if (node.children[i].IsLeaf)
                    {
                        result = node.children[i];
                        break;
                    }
                    result = FindClosestNode(node.children[i], position);
                }
            }
            return result;
        }

        [Obsolete("시간복잡도가 O(n^2)라서 노드가 1000개만 되어도 백만번 순회함. 사용하지 말 것." +
            "BuildEdgesWithJobs()를 대신 사용 요망.")]
        void BuildEdges()
        {
            foreach (OctreeNode leaf in emptyLeaves)
            {
                foreach(OctreeNode otherLeaf in emptyLeaves)
                {
                    if (leaf.Equals(otherLeaf)) continue;
                    if (leaf.bounds.Intersects(otherLeaf.bounds))
                    {
                        graph.AddEdge(leaf, otherLeaf);
                    }
                }
            }
        }

        [Obsolete("재귀호출 방식으로 구성되어 있음. 가급적 사용하지 말 것. GetEmptyLeaves()를 대신 사용 요망.")]
        void GetEmptyLeaves(OctreeNode node)
        {
            if (node.IsLeaf && node.objects.Count == 0)
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
                    if (i == j) continue;
                    graph.AddEdge(node.children[i], node.children[j]);
                }
            }
        }

        void GetEmptyLeaves()
        {
            emptyLeaves.Clear();

            Stack<OctreeNode> stack = new Stack<OctreeNode>(1024);
            stack.Push(root);

            List<(OctreeNode, OctreeNode)> edgeBuffer = new List<(OctreeNode, OctreeNode)>(1024);

            while (stack.Count > 0)
            {
                OctreeNode node = stack.Pop();

                if (node.IsLeaf)
                {
                    if (node.objects.Count == 0)
                    {
                        emptyLeaves.Add(node);
                    }
                    continue;
                }

                OctreeNode[] children = node.children;
                if (children == null || children.Length == 0) continue;

                // push children
                for (int i = 0; i < children.Length; i++)
                {
                    stack.Push(children[i]);
                }

                for (int i = 0; i < children.Length; i++)
                {
                    for (int j = i + 1; j < children.Length; j++)
                    {
                        edgeBuffer.Add((children[i], children[j]));
                    }
                }
            }

            foreach (var leaf in emptyLeaves)
            {
                graph.AddNode(leaf);
            }

            foreach (var edge in edgeBuffer)
            {
                graph.AddEdge(edge.Item1, edge.Item2);
            }

            edgeBuffer.Clear();
        }

        void BuildEdgesWithJobs()
        {
            int count = emptyLeaves.Count;
            if (count == 0) return;

            // NativeArray로 변환
            NativeArray<Bounds> boundsArray = new NativeArray<Bounds>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                boundsArray[i] = emptyLeaves[i].bounds;
            }

            // NativeList 초기화 (충분한 capacity 확보)
            NativeList<int2> intersectPairs = new NativeList<int2>(count * 8, Allocator.TempJob);

            try
            {
                // Job 생성
                var job = new BoundsIntersectJob
                {
                    bounds = boundsArray,
                    resultPairs = intersectPairs.AsParallelWriter()
                };

                // Job 실행
                JobHandle handle = job.Schedule(count, 64);
                handle.Complete();

                for (int i = 0; i < intersectPairs.Length; i++)
                {
                    int2 p = intersectPairs[i];
                    graph.AddEdge(emptyLeaves[p.x], emptyLeaves[p.y]);
                }
            }
            finally
            {
                // 메모리 해제
                if (boundsArray.IsCreated) boundsArray.Dispose();
                if (intersectPairs.IsCreated) intersectPairs.Dispose();
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

            Vector3 size = Vector3.one * Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) * 0.6f;
            bounds.SetMinMax(bounds.center - size, bounds.center + size);
        }
    }
}