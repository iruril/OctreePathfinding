using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

namespace Octrees
{
    public class Octree
    {
        [BurstCompile]
        public struct AABB
        {
            public Vector3 center;
            public Vector3 extents;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Intersects(in AABB other)
            {
                return Mathf.Abs(center.x - other.center.x) <= (extents.x + other.extents.x) &&
                       Mathf.Abs(center.y - other.center.y) <= (extents.y + other.extents.y) &&
                       Mathf.Abs(center.z - other.center.z) <= (extents.z + other.extents.z);
            }
        }

        [BurstCompile]
        public struct BuildEdgesJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<AABB> boundsArray;
            public NativeList<int2>.ParallelWriter edgeWriter;

            public void Execute(int index)
            {
                var a = boundsArray[index];
                for (int j = index + 1; j < boundsArray.Length; j++)
                {
                    var b = boundsArray[j];
                    if (a.Intersects(b))
                    {
                        edgeWriter.AddNoResize(new int2(index, j));
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
            
            BuildEdgesWithJob();
            Debug.LogFormat("[{0:F3}s] [Octree] Graph Created", Time.realtimeSinceStartup);
            Debug.Log($"[Octree] {this.graph.nodes.Count} nodes created.");
            Debug.Log($"[Octree] {this.graph.edges.Count} edges created.");
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


        void BuildEdgesWithJob()
        {
            int count = emptyLeaves.Count;
            if (count == 0) return;

            var boundsArray = new NativeArray<AABB>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                var b = emptyLeaves[i].bounds;
                boundsArray[i] = new AABB
                {
                    center = b.center,
                    extents = b.extents
                };
            }

            var edgeList = new NativeList<int2>(count * 8, Allocator.TempJob);

            var job = new BuildEdgesJob
            {
                boundsArray = boundsArray,
                edgeWriter = edgeList.AsParallelWriter()
            };

            var handle = job.Schedule(count, 32);
            handle.Complete();

            for (int i = 0; i < edgeList.Length; i++)
            {
                var e = edgeList[i];
                graph.AddEdge(emptyLeaves[e.x], emptyLeaves[e.y]);
            }

            boundsArray.Dispose();
            edgeList.Dispose();
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
        [Obsolete("Octree 구조 상 이미 공간 분할이 잘 되어있어 의미가 없음.")]
        [BurstCompile]
        struct BuildEdgesSpatialHashJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<AABB> boundsArray;
            [ReadOnly] public NativeParallelMultiHashMap<ulong, int> spatialMap;
            [ReadOnly] public float cellSize;
            public NativeList<int2>.ParallelWriter edges;

            public void Execute(int index)
            {
                var aabb = boundsArray[index];
                int3 cell = (int3)math.floor(aabb.center / cellSize);

                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            int3 neighbor = cell + new int3(dx, dy, dz);
                            ulong hash = HashCell(neighbor);

                            if (!spatialMap.TryGetFirstValue(hash, out int otherIndex, out var it))
                                continue;

                            do
                            {
                                if (otherIndex <= index) continue;

                                var other = boundsArray[otherIndex];
                                if (aabb.Intersects(other))
                                {
                                    edges.AddNoResize(new int2(index, otherIndex));
                                }
                            }
                            while (spatialMap.TryGetNextValue(out otherIndex, ref it));
                        }
            }

            static ulong HashCell(int3 cell)
            {
                unchecked
                {
                    return (ulong)(
                        (cell.x * 73856093) ^
                        (cell.y * 19349663) ^
                        (cell.z * 83492791)
                    );
                }
            }
        }

        [Obsolete("Octree 구조 상 이미 공간 분할이 잘 되어있어 의미가 없음. BuildEdgesWithJob()을 사용할 것.")]
        public void BuildEdgesWithSpatialHashJobs()
        {
            int count = emptyLeaves.Count;
            if (count == 0) return;

            var boundsArray = new NativeArray<AABB>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                var b = emptyLeaves[i].bounds;
                boundsArray[i] = new AABB
                {
                    center = b.center,
                    extents = b.extents
                };
            }

            float cellSize = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z) / 4f;

            var spatialMap = new NativeParallelMultiHashMap<ulong, int>(count, Allocator.TempJob);
            for (int i = 0; i < count; i++)
            {
                int3 cell = (int3)math.floor(boundsArray[i].center / cellSize);
                ulong hash = (ulong)(
                    (cell.x * 73856093) ^
                    (cell.y * 19349663) ^
                    (cell.z * 83492791)
                );
                spatialMap.Add(hash, i);
            }

            var edgeList = new NativeList<int2>(count * 8, Allocator.TempJob);

            var job = new BuildEdgesSpatialHashJob
            {
                boundsArray = boundsArray,
                spatialMap = spatialMap,
                cellSize = cellSize,
                edges = edgeList.AsParallelWriter()
            };

            var handle = job.Schedule(count, 64);
            handle.Complete();

            for (int i = 0; i < edgeList.Length; i++)
            {
                var e = edgeList[i];
                graph.AddEdge(emptyLeaves[e.x], emptyLeaves[e.y]);
            }

            boundsArray.Dispose();
            spatialMap.Dispose();
            edgeList.Dispose();
        }

        [Obsolete("시간복잡도가 O(n^2)라서 노드가 1000개만 되어도 백만번 순회함. 사용하지 말 것." +
            "BuildEdgesWithJob()을 대신 사용 요망.")]
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