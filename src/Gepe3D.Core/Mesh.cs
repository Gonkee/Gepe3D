using System.Collections;
using System.Collections.Generic;
using Gepe3D.Util;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Gepe3D.Core
{
    public abstract class Mesh
    {
        Vector3[] colors = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(0, 1, 1),
            new Vector3(1, 0, 1)
        };
        int colorID = 0;

        private readonly List<Vector3 > vertices  = new List<Vector3 >();
        private readonly List<Vector3i> triangles = new List<Vector3i>();

        private readonly int floatsPerVertex = 6; // just position
        private float[] _vertexData;
        private bool _dataDirty = false;
        
        private int _vboID;
        private int _vaoID;

        public Mesh (int vertexCount, int triangleCount)
        {
            _vertexData = new float[vertexCount * 3];

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

        public void GenerateVertexData()
        {
            _vertexData = new float[triangles.Count * 3 * floatsPerVertex];

            int pointer = 0;
            foreach (Vector3i tri in triangles)
            {
                Vector3 v1 = vertices[tri.X];
                Vector3 v2 = vertices[tri.Y];
                Vector3 v3 = vertices[tri.Z];

                _vertexData[pointer++] = v1.X;
                _vertexData[pointer++] = v1.Y;
                _vertexData[pointer++] = v1.Z;
                _vertexData[pointer++] = colors[colorID].X;
                _vertexData[pointer++] = colors[colorID].Y;
                _vertexData[pointer++] = colors[colorID].Z;

                _vertexData[pointer++] = v2.X;
                _vertexData[pointer++] = v2.Y;
                _vertexData[pointer++] = v2.Z;
                _vertexData[pointer++] = colors[colorID].X;
                _vertexData[pointer++] = colors[colorID].Y;
                _vertexData[pointer++] = colors[colorID].Z;

                _vertexData[pointer++] = v3.X;
                _vertexData[pointer++] = v3.Y;
                _vertexData[pointer++] = v3.Z;
                _vertexData[pointer++] = colors[colorID].X;
                _vertexData[pointer++] = colors[colorID].Y;
                _vertexData[pointer++] = colors[colorID].Z;

                System.Console.WriteLine(colors[colorID]);
                colorID = (colorID + 1) % colors.Length;
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


        public void Draw(Shader shader)
        {
            if (_dataDirty) SendToGPU();
            GL.BindVertexArray(_vaoID);

            // drawStyle
            // 0 = fill
            // 1 = line
            // 2 = point

            shader.SetInt("drawStyle", 0);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);

            shader.SetInt("drawStyle", 1);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1, -1);
            GL.LineWidth(4f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);
            GL.Disable(EnableCap.PolygonOffsetLine);
            
            shader.SetInt("drawStyle", 2);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            GL.Enable(EnableCap.PolygonOffsetPoint);
            GL.PolygonOffset(-2, -2);
            GL.PointSize(10f);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);
            GL.Disable(EnableCap.PolygonOffsetPoint);

        }
    }
}