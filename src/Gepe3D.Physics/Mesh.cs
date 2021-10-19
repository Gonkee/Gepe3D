
using Gepe3D.Core;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace Gepe3D.Physics
{
    public class Mesh
    {

        public readonly Material Material;
        public readonly Geometry Geometry;
        public bool DrawWireframe = false;
        public bool Visible = true;

        private bool _dataDirty = false;
        
        private readonly int _vboID;
        private readonly int _vaoID;

        public Mesh (Geometry geometry, Material material)
        {
            this.Geometry = geometry;
            this.Material = material;

            _vboID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            
            float[] vertexData = geometry.GenerateVertexData();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, geometry.FloatsPerVertex * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, geometry.FloatsPerVertex * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        public void SetVertexPos(int id, float x, float y, float z)
        {
            Geometry.Vertices[id] = new Vector3(x, y, z);
            _dataDirty = true;
        }

        

        private void SendToGPU()
        {
            float[] vertexData = Geometry.GenerateVertexData();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferSubData<float>(BufferTarget.ArrayBuffer, new IntPtr(0), vertexData.Length * sizeof(float), vertexData);
            _dataDirty = false;
        }

        public void Draw()
        {
            if (_dataDirty) SendToGPU();
            GL.BindVertexArray(_vaoID);
            GL.DrawArrays(PrimitiveType.Triangles, 0, Geometry.TriangleIDs.Count * 3);
        }
    }
}