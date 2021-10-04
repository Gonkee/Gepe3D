
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Gepe3D.Core
{
    public abstract class Mesh
    {

        private readonly List<Vector3 > vertices  = new List<Vector3 >();
        private readonly List<Vector3i> triangles = new List<Vector3i>();

        private readonly int floatsPerVertex = 6;
        private float[] _vertexData;
        private bool _dataDirty = false;
        
        private int _vboID;
        private int _vaoID;

        public Mesh ()
        {
            _vboID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
        }

        public int AddVertex(float x, float y, float z)
        {
            int id = vertices.Count;
            vertices.Add(new Vector3(x, y, z));
            return id;
        }

        public void AddTriangle(int v1_ID, int v2_ID, int v3_ID)
        {
            triangles.Add(new Vector3i(v1_ID, v2_ID, v3_ID));
            _dataDirty = true;
        }

        private void GenerateVertexData()
        {
            _vertexData = new float[triangles.Count * 3 * floatsPerVertex];

            int pointer = 0;
            Vector3 v1, v2, v3, normal;
            foreach (Vector3i tri in triangles)
            {
                v1 = vertices[tri.X];
                v2 = vertices[tri.Y];
                v3 = vertices[tri.Z];

                normal = Vector3.Cross( v2 - v1, v3 - v1 ).Normalized();

                _vertexData[pointer++] = v1.X;
                _vertexData[pointer++] = v1.Y;
                _vertexData[pointer++] = v1.Z;
                _vertexData[pointer++] = normal.X;
                _vertexData[pointer++] = normal.Y;
                _vertexData[pointer++] = normal.Z;

                _vertexData[pointer++] = v2.X;
                _vertexData[pointer++] = v2.Y;
                _vertexData[pointer++] = v2.Z;
                _vertexData[pointer++] = normal.X;
                _vertexData[pointer++] = normal.Y;
                _vertexData[pointer++] = normal.Z;

                _vertexData[pointer++] = v3.X;
                _vertexData[pointer++] = v3.Y;
                _vertexData[pointer++] = v3.Z;
                _vertexData[pointer++] = normal.X;
                _vertexData[pointer++] = normal.Y;
                _vertexData[pointer++] = normal.Z;
            }
        }

        private void SendToGPU()
        {
            GenerateVertexData();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexData.Length * sizeof(float), _vertexData, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            _dataDirty = false;
        }

        public void Draw()
        {
            if (_dataDirty) SendToGPU();
            GL.BindVertexArray(_vaoID);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);
        }

    }
}