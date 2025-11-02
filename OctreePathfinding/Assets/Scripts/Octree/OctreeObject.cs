using System.Collections.Generic;
using UnityEngine;

namespace Octrees
{
    public class OctreeObject
    {
        public readonly List<Bounds> triangleBounds = new();

        public OctreeObject(MeshFilter meshFilter)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                return;

            Mesh mesh = meshFilter.sharedMesh;
            Transform t = meshFilter.transform;
            var vertices = mesh.vertices;
            var triangles = mesh.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = t.TransformPoint(vertices[triangles[i]]);
                Vector3 v1 = t.TransformPoint(vertices[triangles[i + 1]]);
                Vector3 v2 = t.TransformPoint(vertices[triangles[i + 2]]);

                Bounds triBounds = new Bounds(v0, Vector3.zero);
                triBounds.Encapsulate(v1);
                triBounds.Encapsulate(v2);

                triangleBounds.Add(triBounds);
            }
        }

        public bool Intersects(Bounds nodeBounds)
        {
            for (int i = 0; i < triangleBounds.Count; i++)
            {
                if (triangleBounds[i].Intersects(nodeBounds))
                    return true;
            }
            return false;
        }
    }
}
