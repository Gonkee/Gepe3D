using System;
using Gepe3D.Core;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Entities
{
    public class QuadMesh : Mesh
    {

        private float width, height;
        private bool _centeredOrigin;

        public QuadMesh(float width, float height, bool centeredOrigin) : base(4, 2)
        {
            this.width = width;
            this.height = height;
            this._centeredOrigin = centeredOrigin;
            
            int bl, br, tl, tr; // bottom left, borrom right, top left, top right
            if (_centeredOrigin)
            {
                bl = AddVertex(-width / 2, -height / 2, 0);
                tl = AddVertex(-width / 2,  height / 2, 0);
                tr = AddVertex( width / 2,  height / 2, 0);
                br = AddVertex( width / 2, -height / 2, 0);
            }
            else
            {
                bl = AddVertex(    0,      0, 0);
                tl = AddVertex(    0, height, 0);
                tr = AddVertex(width, height, 0);
                br = AddVertex(width,      0, 0);
            }
            // counter clockwise triangles
            AddTriangle(bl, tr, tl);
            AddTriangle(bl, br, tr);
        }

    }
}