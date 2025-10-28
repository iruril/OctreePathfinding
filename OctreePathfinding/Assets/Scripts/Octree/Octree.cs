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

            GetEmptyLeaves(root); 
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

        [Obsolete("시간복잡도가 O(n^2)라서 노드가 1000개만 되어도 백만번 순회한다. 사용하지 말 것.")]
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

        // Job, Burst로 메모리 효율성 극대화
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

            // 한 리프 노드당 최대 8개 정도의 후보만 교차한다고 가정
            NativeList<int2> intersectPairs = new NativeList<int2>(count * 8, Allocator.TempJob);

            // Job 생성
            var job = new BoundsIntersectJob
            {
                bounds = boundsArray,
                resultPairs = intersectPairs.AsParallelWriter()
            };

            // Job 실행 (Burst 병렬)
            JobHandle handle = job.Schedule(count, 64);
            handle.Complete();

            // 결과 반영
            foreach (var pair in intersectPairs)
            {
                graph.AddEdge(emptyLeaves[pair.x], emptyLeaves[pair.y]);
            }

            // 메모리 해제
            boundsArray.Dispose();
            intersectPairs.Dispose();
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