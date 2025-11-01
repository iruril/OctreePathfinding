using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Octrees
{
    [RequireComponent(typeof(Rigidbody))]
    public class OctreeAgent : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float accuracy = 1f;
        [SerializeField] private float turnSpeed = 180f;
        [SerializeField] private float size = 0.25f;
        
        Rigidbody rb;
        int currentWaypoint;
        OctreeNode currentNode; 
        
        Vector3 pendingMove = Vector3.zero;
        Quaternion pendingRotation = Quaternion.identity;

        List<Node> path = new();

        public int GetPathLength() => path.Count; 
        bool isRequestingPath = false;

        public bool IsCompletePath { get; private set; } = false;

        public OctreeNode GetPathNode(int index)
        {
            if (path == null) return null;
            if (index < 0 || index >= path.Count)
            {
                Debug.LogError($"Index out of bounds. Path Legth : {GetPathLength()}, Index : {index}");
                return null;
            }
            return path[index].octreeNode;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Start()
        {
            currentNode = GetClosestNode(transform.position);
            RequestRandomPath();
        }

        void FixedUpdate()
        {
            if (OctreeBaker.Instance.graph == null) return;
            if (path == null || path.Count == 0 || currentWaypoint >= path.Count) return;

            Vector3 targetPos = currentNode.bounds.center;
            Vector3 dirToTarget = (targetPos - rb.position);
            Vector3 desiredDir = dirToTarget.normalized;

            pendingRotation = Quaternion.LookRotation(desiredDir, Vector3.up);
           
            Vector3 moveDir = rb.transform.forward;

            if (Physics.SphereCast(rb.position, size * 2f, moveDir, out var hit, size * 2f, OctreeBaker.Instance.obstacleMaskLayer))
            {
                moveDir = Vector3.ProjectOnPlane(moveDir, hit.normal).normalized;
            }

            pendingMove = rb.position + moveDir * speed * Time.fixedDeltaTime;

            if ((targetPos - rb.position).sqrMagnitude < accuracy * accuracy)
            {
                currentWaypoint++;
                if (currentWaypoint < path.Count)
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
            path = PathOptimizer.Simplify(newPath, size, OctreeBaker.Instance.obstacleMaskLayer);
            IsCompletePath = result;
            currentWaypoint = 0;
            isRequestingPath = false;
        }

        public void OnPathInvaid()
        {
            isRequestingPath = false; 
            RequestRandomPath();
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
