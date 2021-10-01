using System;
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


        public void Draw()
        {
            if (_dataDirty) SendToGPU();
            GL.BindVertexArray(_vaoID);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);
        }
    }
}