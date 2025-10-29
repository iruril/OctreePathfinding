using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class OctreeBaker : MonoBehaviour
    {
        public static OctreeBaker Instance = null;

        [SerializeField] Transform _levelParent;
        public GameObject[] LevelObjects { get; private set; } = new GameObject[0];
        public float minNodeSize = 1f;
        public Octree ot;

        public readonly Graph graph = new();
        [SerializeField] private bool drawNodeGizmos = false;
        [SerializeField] private bool drawPathGizmos = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Debug.LogWarning("OctreeBaker is Duplicated!! Remaining One Instance ...");
                Destroy(this.gameObject);
                return;
            }

            Debug.LogFormat("[{0:F3}s] Finding Levels...", Time.realtimeSinceStartup);
            LevelObjects = new GameObject[_levelParent.childCount];
            for (int i = 0; i < _levelParent.childCount; i++)
            {
                LevelObjects[i] = _levelParent.GetChild(i).gameObject;
            }
            ot = new Octree(LevelObjects, minNodeSize, graph);
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