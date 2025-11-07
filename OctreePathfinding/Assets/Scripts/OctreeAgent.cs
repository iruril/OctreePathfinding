using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Octrees
{
    [RequireComponent(typeof(Rigidbody))]
    public class OctreeAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float speed = 5f;
        [SerializeField] private float accuracy = 1f;
        [SerializeField] private float turnSpeed = 180f;

        [Header("Avoidance")]
        [SerializeField] private float sensorDistance = 3f;
        [SerializeField] private float avoidanceStrength = 1.5f;

        Rigidbody rb;
        int currentWaypoint;
        OctreeNode currentNode;

        Vector3 currentDirection = Vector3.forward;
        Vector3 pendingMove = Vector3.zero;
        Quaternion pendingRotation = Quaternion.identity;

        List<Node> path = new();

        public int GetPathLength() => path.Count; 
        bool isRequestingPath = false;

        public bool IsCompletePath { get; private set; } = false; 
        
        bool isAvoiding = false;
        Vector3 lastAvoidDir;

        static readonly Vector3[] Directions =
        {
            Vector3.forward,                               // F
            Vector3.left,                                  // L
            Vector3.right,                                 // R
            Vector3.up,                                    // U
            Vector3.down,                                  // D
            (Vector3.forward + Vector3.left).normalized,   // FL
            (Vector3.forward + Vector3.right).normalized,  // FR
            (Vector3.forward + Vector3.up).normalized,     // FU
            (Vector3.forward + Vector3.down).normalized,   // FD
        };

        struct SensorHit
        {
            public bool hit;
            public Vector3 normal;
            public Vector3 direction;
            public float weight;
        }

        OctreeNode GetPathNode(int index)
        {
            if (path == null) return null;
            if (index < 0 || index >= path.Count)
            {
                Debug.LogError($"Index out of bounds. Path Legth : {GetPathLength()}, Index : {index}");
                return null;
            }
            return path[index].octreeNode;
        }

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            currentNode = GetClosestNode(transform.position);
            RequestRandomPath();
        }

        Vector3 moveDirVelocity = Vector3.zero;
        void FixedUpdate()
        {
            if (OctreeBaker.Instance.graph == null) return;
            if (path == null || path.Count == 0 || currentWaypoint >= path.Count) return;

            Vector3 targetPos = currentNode.bounds.center;
            Vector3 desired = (targetPos - rb.position).normalized;

            // ---------- 회피 계산 ----------
            Vector3 avoidance = CalculateAvoidance(desired);
            bool hitObstacle = avoidance != Vector3.zero;

            // ---------- 회피 상태 관리 ----------
            if (hitObstacle)
            {
                if (!isAvoiding)
                {
                    isAvoiding = true;
                    lastAvoidDir = avoidance;
                }
                else
                {
                    // 회피 중이면 방향을 부드럽게 갱신
                    lastAvoidDir = Vector3.Slerp(lastAvoidDir, avoidance, Time.deltaTime * 5f);
                }
            }
            else
            {
                isAvoiding = false;
            }

            Vector3 targetDir = isAvoiding ? lastAvoidDir : desired;
            currentDirection = Vector3.SmoothDamp(
                currentDirection,     
                targetDir,           
                ref moveDirVelocity, 
                0.2f                 
            );

            currentDirection.Normalize();

            pendingRotation = Quaternion.LookRotation(currentDirection, Vector3.up);
            pendingMove = rb.position + currentDirection * speed * Time.fixedDeltaTime;

            if ((targetPos - rb.position).sqrMagnitude < accuracy * accuracy)
            {
                currentWaypoint++;
                if (currentWaypoint < GetPathLength())
                    currentNode = path[currentWaypoint].octreeNode;
            }
        }

        void Update()
        {
            if (OctreeBaker.Instance.graph == null) return;

            if (GetPathLength() == 0 || currentWaypoint >= GetPathLength())
            {
                if (!isRequestingPath) RequestRandomPath();
                return;
            }

            rb.MovePosition(pendingMove);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, pendingRotation, turnSpeed * Time.deltaTime));
        }

        OctreeNode GetClosestNode(Vector3 position)
        {
            return OctreeBaker.Instance.ot.FindClosestNode(transform.position);
        }

        void RequestRandomPath()
        {
            if (isRequestingPath) return;
            isRequestingPath = true;

            OctreeNode destinationNode;
            destinationNode = OctreeBaker.Instance.graph.nodes.ElementAt(Random.Range(0, OctreeBaker.Instance.graph.nodes.Count)).Key;
            OctreeBaker.Instance.RequestPath(currentNode, destinationNode, this);
        }

        public void OnPathReady(List<Node> newPath, bool result)
        {
            path = PathOptimizer.Simplify(newPath, OctreeBaker.Instance.obstacleMaskLayer);
            IsCompletePath = result;
            currentWaypoint = 0;
            isRequestingPath = false;
        }

        public void OnPathInvaid()
        {
            isRequestingPath = false; 
            RequestRandomPath();
        }

        RaycastHit[] raycastHits = new RaycastHit[1];
        SensorHit[] sensorBuffer = new SensorHit[Directions.Length];
        SensorHit[] ScanEnvironment(float distance, LayerMask mask)
        {
            SensorHit[] result = new SensorHit[Directions.Length];
            Vector3 origin = rb.position;

            for (int i = 0; i < Directions.Length; i++)
            {
                Vector3 dir = transform.TransformDirection(Directions[i]); 

                sensorBuffer[i].hit = false;
                sensorBuffer[i].normal = Vector3.zero;
                sensorBuffer[i].direction = dir;
                sensorBuffer[i].weight = 0f;

                int hitCount = Physics.RaycastNonAlloc(origin, dir, raycastHits, distance, mask);

                if (hitCount > 0)
                {
                    var hitInfo = raycastHits[0];

                    sensorBuffer[i].hit = true;
                    sensorBuffer[i].normal = hitInfo.normal;

                    float hitDist = hitInfo.distance;
                    float w = Mathf.Clamp01(hitDist / distance);
                    sensorBuffer[i].weight = w;
                }
            }

            return sensorBuffer;
        }

        Vector3 CalculateAvoidance(Vector3 desired)
        {
            LayerMask mask = OctreeBaker.Instance.obstacleMaskLayer;
            SensorHit[] scans = ScanEnvironment(sensorDistance, mask);

            Vector3 avoid = Vector3.zero;

            foreach (var s in scans)
            {
                if (!s.hit) continue;
                avoid += s.normal * avoidanceStrength;
                if (Vector3.Dot(s.direction, transform.forward) > 0.6f)
                    avoid += s.normal * avoidanceStrength;
            }

            Vector3 final = desired + avoid;
            return final == Vector3.zero ? desired : final.normalized;
        }

        void OnDrawGizmos()
        {
            if (!Application.isPlaying || OctreeBaker.Instance.graph == null || GetPathLength() == 0) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(GetPathNode(0).bounds.center, 0.7f);

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(GetPathNode(GetPathLength() - 1).bounds.center, 0.7f);

            for (int i = 0; i < GetPathLength(); i++)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(GetPathNode(i).bounds.center, 0.5f);
                if (i < GetPathLength() - 1)
                {
                    Vector3 start = GetPathNode(i).bounds.center;
                    Vector3 end = GetPathNode(i + 1).bounds.center;
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
