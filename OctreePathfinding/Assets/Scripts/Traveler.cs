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

        public OctreeBaker octreeBaker;
        Graph graph;

        void Start()
        {
            graph = octreeBaker.waypoints;
            currentNode = GetClosestNode(transform.position);
            GetRandomDestination();
        }

        void Update()
        {

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
    }
}
