using System.Linq;
using UnityEngine;


namespace Octrees
{
    public class Traveler : MonoBehaviour
    {
        [SerializeField] private float speed = 5f;
        [SerializeField] private float accuracy = 1f;
        [SerializeField] private float turnSpeed = 5f;

        int currentWaypoint;
        OctreeNode currentNode;
        Vector3 destination;

        OctreeBaker octreeBaker;
        Graph graph;

        private void Awake()
        {
            octreeBaker = GameObject.FindFirstObjectByType<OctreeBaker>();
        }

        void Start()
        {
            graph = octreeBaker.waypoints;
            currentNode = GetClosestNode(transform.position);
            GetRandomDestination();
        }

        void Update()
        {
            if (graph == null) return;

            if (graph.GetPathLength() == 0 || currentWaypoint >= graph.GetPathLength())
            {
                GetRandomDestination();
                return;
            }

            if (Vector3.Distance(graph.GetPathNode(currentWaypoint).bounds.center, transform.position) < accuracy)
            {
                currentWaypoint++;
                Debug.Log($"Waypoint {currentWaypoint} reached");
            }

            if (currentWaypoint < graph.GetPathLength())
            {
                currentNode = graph.GetPathNode(currentWaypoint);
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
            return octreeBaker.ot.FindClosestNode(transform.position);
        }

        void GetRandomDestination()
        {
            OctreeNode destinationNode;
            do
            {
                destinationNode = graph.nodes.ElementAt(Random.Range(0, graph.nodes.Count)).Key;
            } while (!graph.AStar(currentNode, destinationNode));
            currentWaypoint = 0;
        }

        void OnDrawGizmos()
        {
            if (graph == null || graph.GetPathLength() == 0) return;

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(graph.GetPathNode(0).bounds.center, 0.7f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(graph.GetPathNode(graph.GetPathLength() - 1).bounds.center, 0.7f);

            for (int i = 0; i < graph.GetPathLength(); i++)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(graph.GetPathNode(i).bounds.center, 0.5f);
                if (i < graph.GetPathLength() - 1)
                {
                    Vector3 start = graph.GetPathNode(i).bounds.center;
                    Vector3 end = graph.GetPathNode(i + 1).bounds.center;
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawLine(start, end);
                }
            }
        }
    }
}
