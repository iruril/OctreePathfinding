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

        //��Ƽ������ ȯ�濡�� ���ôٹ����� Enqueue(), Dequeue()�� �����ϱ� ���� 
        private readonly ConcurrentQueue<(OctreeAgent agent, List<Node> path, bool result)> completeAgents = new();
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
        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = Color.white;

            // ��� �ڽ�
            GUI.Box(new Rect(10, 10, 300, 80), "Pathfinding Status");

            // ���� �ؽ�Ʈ ǥ��
            GUILayout.BeginArea(new Rect(20, 35, 280, 100));
            GUILayout.Label($"Running Tasks: {runningTasks.Count}", style);
            GUILayout.Label($"Pending Requests: {pendingRequests.Count}", style);
            GUILayout.EndArea();
        }

        private void Update()
        {
            ProcessPathfindingTasks();
        }

        void ProcessPathfindingTasks()
        {
            if (completeAgents.Count > 0)
            {
                if (completeAgents.TryDequeue(out var result)) result.agent.OnPathReady(result.path, result.result);
            }

            runningTasks.RemoveAll(t => t.IsCompleted);

            if (pendingRequests.Count > 0 && runningTasks.Count < MaxConcurrentTasks)
            {
                PathRequest req = pendingRequests.Dequeue();
                Node start = graph.FindNode(req.startNode);
                Node end = graph.FindNode(req.endNode);
                if (start == null || end == null)
                {
                    req.agent.OnPathInvaid();
                    return;
                }

                Task task = Task.Run(() =>
                {
                    List<Node> path = new();
                    PathfindingContext ctx = pool.Rent();
                    try
                    {
                        bool result = graph.AStar(start, end, ref path, ctx);
                        completeAgents.Enqueue((req.agent, path, result));
                    }
                    finally
                    {
                        pool.Return(ctx);
                    }
                });

                runningTasks.Add(task);
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