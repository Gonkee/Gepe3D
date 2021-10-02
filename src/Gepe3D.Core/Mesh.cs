using System;
using Gepe3D.Util;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Core
{
    public abstract class Mesh
    {

        private readonly float[] _vertPos;
        private readonly uint[] _indices;
        private bool _dataDirty = false;
        
        private int _vboID;
        private int _vaoID;
        private int _eboID;

        public Mesh (int vertexCount, int triangleCount)
        {
            _vertPos = new float[vertexCount * 3];
            _indices = new uint[triangleCount * 3];

            _vboID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            _eboID = GL.GenBuffer();

        }

        public void SetVertPos(int vertexID, float x, float y, float z)
        {
            _vertPos[vertexID * 3    ] = x;
            _vertPos[vertexID * 3 + 1] = y;
            _vertPos[vertexID * 3 + 2] = z;
            _dataDirty = true;
        }

        public void DeclareTriangle(int triangleID, uint v1, uint v2, uint v3)
        {
            _indices[triangleID * 3    ] = v1;
            _indices[triangleID * 3 + 1] = v2;
            _indices[triangleID * 3 + 2] = v3;
            _dataDirty = true;
        }

        private void SendToGPU()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertPos.Length * sizeof(float), _vertPos, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _eboID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

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
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            shader.SetInt("drawStyle", 1);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            GL.Enable(EnableCap.PolygonOffsetLine);
            GL.PolygonOffset(-1, -1);
            GL.LineWidth(4f);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.Disable(EnableCap.PolygonOffsetLine);
            
            shader.SetInt("drawStyle", 2);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            GL.Enable(EnableCap.PolygonOffsetPoint);
            GL.PolygonOffset(-2, -2);
            GL.PointSize(10f);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
            GL.Disable(EnableCap.PolygonOffsetPoint);

        }
    }
}