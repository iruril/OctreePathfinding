using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Octrees
{
    public class OctreeAgent : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float accuracy = 1f;
        [SerializeField] private float turnSpeed = 5f;

        int currentWaypoint;
        OctreeNode currentNode;
        Vector3 destination;

        List<Node> path = new();

        public int GetPathLength() => path.Count;

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

        void Start()
        {
            currentNode = GetClosestNode(transform.position);
            GetRandomDestination();
        }

        void Update()
        {
            if (OctreeBaker.Instance.graph == null) return;

            if (GetPathLength() == 0 || currentWaypoint >= GetPathLength())
            {
                GetRandomDestination();
                return;
            }

            if (Vector3.Distance(GetPathNode(currentWaypoint).bounds.center, transform.position) < accuracy)
            {
                currentWaypoint++;
            }

            if (currentWaypoint < GetPathLength())
            {
                currentNode = GetPathNode(currentWaypoint);
                destination = currentNode.bounds.center;

                Vector3 direction = destination - transform.position;
                direction.Normalize();

                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSpeed * Time.deltaTime);
                transform.Translate(0, 0, speed * Time.deltaTime);
            }
            else
            {
                GetRandomDestination();
            }
        }

        OctreeNode GetClosestNode(Vector3 position)
        {
            return OctreeBaker.Instance.ot.FindClosestNode(transform.position);
        }

        void GetRandomDestination()
        {
            OctreeNode destinationNode;
            do
            {
                destinationNode = OctreeBaker.Instance.graph.nodes.ElementAt(Random.Range(0, OctreeBaker.Instance.graph.nodes.Count)).Key;
            } while (!OctreeBaker.Instance.graph.AStar(currentNode, destinationNode, ref path));
            currentWaypoint = 0;
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
