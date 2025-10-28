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
        struct SpatialHashMapJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<int3> cellCoords;
            public NativeParallelMultiHashMap<int, int>.ParallelWriter mapWriter;

            public void Execute(int index)
            {
                int key = (int)math.hash(cellCoords[index]);
                mapWriter.Add(key, index);
            }
        }

        [BurstCompile]
        struct SpatialHashMapCheckJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Bounds> bounds;
            [ReadOnly] public NativeArray<int3> cellCoords;
            [ReadOnly] public NativeParallelMultiHashMap<int, int> cellMap;
            public NativeList<int2>.ParallelWriter resultPairs;

            public void Execute(int i)
            {
                Bounds bound = bounds[i];
                int3 baseCell = cellCoords[i];

                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int3 neighbor = baseCell + new int3(dx, dy, dz);
                            int key = (int)math.hash(neighbor);

                            int iter;
                            if (cellMap.TryGetFirstValue(key, out iter, out var iterator))
                            {
                                int j = iter;
                                while (true)
                                {
                                    if (j > i)
                                    {
                                        if (bound.Intersects(bounds[j]))
                                        {
                                            resultPairs.AddNoResize(new int2(i, j));
                                        }
                                    }

                                    if (!cellMap.TryGetNextValue(out j, ref iterator)) break;
                                }
                            }
                        }
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

            GetEmptyLeaves();
            Debug.LogFormat("[{0:F3}s] [Octree] Creating Graph with Empty Leaf Node", Time.realtimeSinceStartup);
            
            BuildEdges(minNodeSize);
            Debug.LogFormat("[{0:F3}s] [Octree] Graph Created", Time.realtimeSinceStartup);
        }

        public OctreeNode FindClosestNode(Vector3 position) => FindClosestNode(root, position);

        public OctreeNode FindClosestNode(OctreeNode node, Vector3 position)
        {
            OctreeNode result = null;
            for (int i = 0; i < node.children.Length; i++)
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

        void BuildEdges(float minNodeSize)
        {
            int count = emptyLeaves.Count;
            if (count == 0) return;

            NativeArray<Bounds> boundsArray = new NativeArray<Bounds>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                boundsArray[i] = emptyLeaves[i].bounds;
            }

            NativeArray<int3> cellCoords = new NativeArray<int3>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                float3 center = boundsArray[i].center;
                int3 ci = new int3(
                    (int)math.floor(center.x / minNodeSize),
                    (int)math.floor(center.y / minNodeSize),
                    (int)math.floor(center.z / minNodeSize)
                );
                cellCoords[i] = ci;
            }

            var map = new NativeParallelMultiHashMap<int, int>((int)(count * 1.5f), Allocator.TempJob); //넉넉하게 1.5배 정도

            var insertJob = new SpatialHashMapJob
            {
                cellCoords = cellCoords,
                mapWriter = map.AsParallelWriter()
            };
            JobHandle insertHandle = insertJob.Schedule(count, 64);
            insertHandle.Complete();

            NativeList<int2> intersectPairs = new NativeList<int2>(count * 8, Allocator.TempJob);

            // 각 i에 대해 27개 이웃 셀을 조회하여 교차 검사
            var checkJob = new SpatialHashMapCheckJob
            {
                bounds = boundsArray,
                cellCoords = cellCoords,
                cellMap = map,
                resultPairs = intersectPairs.AsParallelWriter()
            };

            JobHandle checkHandle = checkJob.Schedule(count, 64);
            checkHandle.Complete();

            for (int i = 0; i < intersectPairs.Length; i++)
            {
                int2 pair = intersectPairs[i];
                graph.AddEdge(emptyLeaves[pair.x], emptyLeaves[pair.y]);
            }

            boundsArray.Dispose();
            cellCoords.Dispose();
            if (map.IsCreated) map.Dispose();
            intersectPairs.Dispose();
        }

        void CreateTree(GameObject[] worldObjects, float minNodeSize)
        {
            root = new OctreeNode(bounds, minNodeSize);

            foreach (var obj in worldObjects)
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

        #region  Obsoleted Logics
        [Obsolete("시간복잡도가 O(n^2)라서 노드가 1000개만 되어도 백만번 순회함. 사용하지 말 것." +
            "BuildEdgesWithJobs()를 대신 사용 요망.")]
        void BuildEdges()
        {
            foreach (OctreeNode leaf in emptyLeaves)
            {
                foreach (OctreeNode otherLeaf in emptyLeaves)
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
        #endregion
    }
}