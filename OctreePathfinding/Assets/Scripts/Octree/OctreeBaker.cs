using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        public static OctreeBaker Instance = null;

        [SerializeField] Transform levelParent;
        [SerializeField] LayerMask obstacleMask;
        public LayerMask obstacleMaskLayer => obstacleMask;
        public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph graph = new();
        [SerializeField] private bool drawNodeGizmos = false;
        [SerializeField] private bool drawPathGizmos = false;

        //멀티스레드 환경에서 동시다발적인 Enqueue(), Dequeue()에 대응하기 위함 
        private readonly ConcurrentQueue<(OctreeAgent agent, List<Node> path, bool result)> completeAgents = new();
        private BlockingCollection<PathRequest> requestQueue = new BlockingCollection<PathRequest>();

        private readonly ConcurrentStack<List<Node>> listPool = new(); // GC 방지 위해 리스트 재활용
        private PathfindingContextPool pool = new();

        private const int maxConcurrentTasks = 6;
        private Thread[] workerThreads; // Task 대신 고정 스레드 사용
        private bool isRunning = true;  // 스레드 종료 플래그


        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(this.gameObject);
                return;
            }

            MeshFilter[] LevelMeshs = levelParent.GetComponentsInChildren<MeshFilter>(includeInactive: false);

            ot = new Octree(LevelMeshs, minNodeSize, graph);
            graph.FinalizeGraph();
            pool.Init(ot.graph.nodes.Count, maxConcurrentTasks);

            // 리스트 풀 사전 할당
            int initialListCount = maxConcurrentTasks * 2;
            for (int i = 0; i < initialListCount; i++)
            {
                listPool.Push(new List<Node>(100)); // 100은 경로의 예상 평균 길이, 추후 임의로 세팅 가능
            }

            // 고정 워커 스레드 생성
            workerThreads = new Thread[maxConcurrentTasks];
            for (int i = 0; i < maxConcurrentTasks; i++)
            {
                workerThreads[i] = new Thread(WorkerLoop);
                workerThreads[i].IsBackground = true; // 메인 스레드 종료 시 함께 종료되도록 설정
                workerThreads[i].Start();
            }
        }

        private void OnGUI()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 16;
            style.normal.textColor = Color.white;

            // 배경 박스
            GUI.Box(new Rect(10, 10, 300, 120), "Octree Pathfinding Status");

            // 내부 텍스트 표시
            GUILayout.BeginArea(new Rect(20, 35, 280, 100));
            GUILayout.Label($"Max Concurrent Tasks: {maxConcurrentTasks}", style);
            //GUILayout.Label($"Running Tasks: {runningTasks.Count}", style);
            //GUILayout.Label($"Pending Requests: {pendingRequests.Count}", style);
            GUILayout.EndArea();
        }

        // 워커 스레드들이 도는 루프
        private void WorkerLoop()
        {
            foreach (PathRequest req in requestQueue.GetConsumingEnumerable())
            {
                if (!isRunning) break; // 에디터 종료 시 스레드 탈출

                Node start = graph.FindNode(req.startNode);
                Node end = graph.FindNode(req.endNode);

                if (start == null || end == null)
                {
                    // 메인 스레드에서 OnPathInvalid를 처리하도록 null로 보냄
                    completeAgents.Enqueue((req.agent, null, false));
                    continue;
                }

                if (!listPool.TryPop(out List<Node> path)) path = new List<Node>(100);

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
            }
        }

        private void Update()
        {
            ProcessPathfindingTasks();
        }

        void ProcessPathfindingTasks()
        {
            while (completeAgents.TryDequeue(out var result))
            {
                if (result.path == null)
                {
                    result.agent.OnPathInvaid();
                }
                else
                {
                    result.agent.OnPathReady(result.path, result.result);
                    result.path.Clear();
                    listPool.Push(result.path);
                }
            }
        }

        public void RequestPath(OctreeNode start, OctreeNode end, OctreeAgent agent)
        {
            requestQueue.Add(new PathRequest(start, end, agent));
        }

        private void OnDestroy()
        {
            isRunning = false;
            requestQueue?.CompleteAdding(); // 대기 중인 스레드들 깨우기

            if (workerThreads != null)
            {
                foreach (var thread in workerThreads)
                {
                    if (thread != null && thread.IsAlive) thread.Join(100);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(ot.bounds.center, ot.bounds.size);

            if (drawNodeGizmos) ot.root.DrawNode();
            if (drawPathGizmos) ot.graph.DrawGraph();
        }
    }
}