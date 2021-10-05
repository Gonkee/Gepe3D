
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D.Physics
{
    public class Geometry
    {
        public readonly List<Vector3> vertices;
        public readonly List<Vector3i> triangleIDs;

        public Geometry()
        {
            vertices = new List<Vector3>();
            triangleIDs = new List<Vector3i>();
        }

        public Geometry(List<Vector3> vertices, List<Vector3i> triangleIDs)
        {
            this.vertices = vertices;
            this.triangleIDs = triangleIDs;
        }

        public int AddVertex(float x, float y, float z)
        {
            int id = vertices.Count;
            vertices.Add(new Vector3(x, y, z));
            return id;
        }

        public void AddTriangle(int v1_ID, int v2_ID, int v3_ID)
        {
            triangleIDs.Add(new Vector3i(v1_ID, v2_ID, v3_ID));
        }
    }
}