
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class Geometry
    {
        public readonly List<Vector3> Vertices;
        public readonly List<Vector3i> TriangleIDs;
        public readonly int FloatsPerVertex = 6;

        public Geometry()
        {
            Vertices = new List<Vector3>();
            TriangleIDs = new List<Vector3i>();
        }

        public Geometry(Geometry geometry)
        {
            this.Vertices    = new List<Vector3 > ( geometry.Vertices );
            this.TriangleIDs = new List<Vector3i> ( geometry.TriangleIDs );
        }

        public Geometry(List<Vector3> vertices, List<Vector3i> triangleIDs)
        {
            this.Vertices = vertices;
            this.TriangleIDs = triangleIDs;
        }

        public int AddVertex(float x, float y, float z)
        {
            int id = Vertices.Count;
            Vertices.Add(new Vector3(x, y, z));
            return id;
        }

        public void AddTriangle(int v1_ID, int v2_ID, int v3_ID)
        {
            TriangleIDs.Add(new Vector3i(v1_ID, v2_ID, v3_ID));
        }

        public Geometry OffsetPosition(float x, float y, float z)
        {
            Vector3 offset = new Vector3(x, y, z);
            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] += offset;
            }
            return this;
        }

        public Geometry Rotate(Vector3 axis, float angleDegrees)
        {
            Matrix3 rotMat = Matrix3.CreateFromAxisAngle(axis, MathHelper.DegreesToRadians(angleDegrees));
            rotMat.Transpose(); // OpenTK matrices are transposed by default for some reason

            for (int i = 0; i < Vertices.Count; i++)
            {
                Vertices[i] = rotMat * Vertices[i];
            }
            return this;
        }

        public Geometry Duplicate()
        {
            return new Geometry(this);
        }

        public float[] GenerateVertexData()
        {
            float[] vertexData = new float[TriangleIDs.Count * 3 * FloatsPerVertex];

            int pointer = 0;
            Vector3 v1, v2, v3, normal;
            foreach (Vector3i tri in TriangleIDs)
            {
                v1 = Vertices[tri.X];
                v2 = Vertices[tri.Y];
                v3 = Vertices[tri.Z];

                normal = Vector3.Cross( v2 - v1, v3 - v1 ).Normalized();

                vertexData[pointer++] = v1.X;
                vertexData[pointer++] = v1.Y;
                vertexData[pointer++] = v1.Z;
                vertexData[pointer++] = normal.X;
                vertexData[pointer++] = normal.Y;
                vertexData[pointer++] = normal.Z;

                vertexData[pointer++] = v2.X;
                vertexData[pointer++] = v2.Y;
                vertexData[pointer++] = v2.Z;
                vertexData[pointer++] = normal.X;
                vertexData[pointer++] = normal.Y;
                vertexData[pointer++] = normal.Z;

                vertexData[pointer++] = v3.X;
                vertexData[pointer++] = v3.Y;
                vertexData[pointer++] = v3.Z;
                vertexData[pointer++] = normal.X;
                vertexData[pointer++] = normal.Y;
                vertexData[pointer++] = normal.Z;
            }
            return vertexData;
        }
    }
}