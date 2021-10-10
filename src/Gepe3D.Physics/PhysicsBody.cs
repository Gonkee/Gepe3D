
using Gepe3D.Core;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace Gepe3D.Physics
{
    public abstract class PhysicsBody
    {
        
        public readonly Material Material;
        public bool DrawWireframe = false;
        public bool Visible = true;
        public readonly List<Vector3 > vertices  = new List<Vector3 >();
        public readonly List<Vector3i> triangles = new List<Vector3i>();

        private readonly int floatsPerVertex = 6;
        private float[] _vertexData;
        private bool _dataDirty = true;
        
        private int _vboID;
        private int _vaoID;

        public PhysicsBody (Geometry geometry, Material material)
        {
            this.vertices  = new List<Vector3 >( geometry.vertices    );
            this.triangles = new List<Vector3i>( geometry.triangleIDs );
            this.Material = material;

            _vboID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            
            _vertexData = new float[triangles.Count * 3 * floatsPerVertex];
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertexData.Length * sizeof(float), _vertexData, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
        }

        public void SetVertexPos(int id, float x, float y, float z)
        {
            vertices[id] = new Vector3(x, y, z);
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
            GL.BufferSubData<float>(BufferTarget.ArrayBuffer, new IntPtr(0), _vertexData.Length * sizeof(float), _vertexData);
            _dataDirty = false;
        }

        public void Draw()
        {
            if (_dataDirty) SendToGPU();
            GL.BindVertexArray(_vaoID);
            GL.DrawArrays(PrimitiveType.Triangles, 0, _vertexData.Length / floatsPerVertex);
        }


        public abstract float[] GetState();

        public abstract float[] GetDerivative(float[] state);

        public abstract void UpdateState(float[] change, List<PhysicsBody> bodies);

        public abstract float MaxX();
        public abstract float MinX();
        public abstract float MaxY();
        public abstract float MinY();
        public abstract float MaxZ();
        public abstract float MinZ();
    }
}