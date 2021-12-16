
using System.Collections.Generic;
using OpenTK.Mathematics;
using System;

namespace Gepe3D
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

            float[] vertexData = geometry.GenerateVertexData();
            _vboID = GLUtils.GenVBO(vertexData);
            _vaoID = GLUtils.GenVAO();
            GLUtils.VaoFloatAttrib(_vaoID, _vboID, 0, 3, geometry.FloatsPerVertex, 0);
            GLUtils.VaoFloatAttrib(_vaoID, _vboID, 1, 3, geometry.FloatsPerVertex, 3);
        }

        public void SetVertexPos(int id, float x, float y, float z)
        {
            Geometry.Vertices[id] = new Vector3(x, y, z);
            _dataDirty = true;
        }

        

        private void SendToGPU()
        {
            float[] vertexData = Geometry.GenerateVertexData();
            GLUtils.ReplaceBufferData(_vboID, vertexData);
            _dataDirty = false;
        }

        public void Draw()
        {
            if (_dataDirty) SendToGPU();
            GLUtils.DrawVAO(_vaoID, Geometry.TriangleIDs.Count * 3);
        }
    }
}