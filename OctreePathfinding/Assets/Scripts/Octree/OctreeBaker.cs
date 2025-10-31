using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        public static OctreeBaker Instance = null;

        [SerializeField] Transform levelParent;
        public GameObject[] LevelObjects { get; private set; } = new GameObject[0];
        public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph graph = new();
        [SerializeField] private bool drawNodeGizmos = false;
        [SerializeField] private bool drawPathGizmos = false;

        //멀티스레드 환경에서 동시다발적인 Enqueue(), Dequeue()에 대응하기 위함 
        private readonly ConcurrentQueue<(OctreeAgent agent, List<Node> path)> succeedAgents = new(); 
        private readonly ConcurrentQueue<OctreeAgent> failedAgents = new();
        private readonly Queue<PathRequest> pendingRequests = new();
        private readonly List<Task> runningTasks = new();

        private const int MaxConcurrentTasks = 20;

        private PathfindingContextPool pool = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(this.gameObject);
                return;
            }

            LevelObjects = new GameObject[levelParent.childCount];
            for (int i = 0; i < levelParent.childCount; i++) LevelObjects[i] = levelParent.GetChild(i).gameObject;
            
            ot = new Octree(LevelObjects, minNodeSize, graph);
            pool.Init(ot.graph.nodes.Count, MaxConcurrentTasks);
        }

        private void Update()
        {
            ProcessPathfindingTasks();
        }

        void ProcessPathfindingTasks()
        {
            if (succeedAgents.Count > 0)
            {
                if (succeedAgents.TryDequeue(out var result)) result.agent.OnPathReady(result.path);
            }

            if (failedAgents.Count > 0)
            {
                if (failedAgents.TryDequeue(out var result)) result.OnPathFailed();
            }

            runningTasks.RemoveAll(t => t.IsCompleted);

            if (pendingRequests.Count > 0 && runningTasks.Count < MaxConcurrentTasks)
            {
                PathRequest req = pendingRequests.Dequeue();
                List<Node> path = new();
                Node start = graph.FindNode(req.startNode);
                Node end = graph.FindNode(req.endNode);
                if (start == null || end == null)
                {
                    req.agent.OnPathFailed();
                    return;
                }

                Task task = Task.Run(() =>
                {
                    PathfindingContext ctx = pool.Rent();
                    try
                    {
                        bool success = graph.AStar(start, end, ref path, ctx);
                        if (success)
                        {
                            succeedAgents.Enqueue((req.agent, path));
                        }
                        else
                        {
                            failedAgents.Enqueue(req.agent);
                        }
                    }
                    finally
                    {
                        pool.Return(ctx);
                    }
                });

                runningTasks.Add(task);
                Debug.Log($"[Pathfinding] runningTasks, {runningTasks.Count}");
            }
        }

        public void RequestPath(OctreeNode start, OctreeNode end, OctreeAgent agent)
        {
            pendingRequests.Enqueue(new PathRequest(start, end, agent));
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            if(drawNodeGizmos) ot.root.DrawNode();
            if(drawPathGizmos) ot.graph.DrawGraph();
        }
    }
}