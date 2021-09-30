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
            
            if (_centeredOrigin)
            {
                SetVertPos(0, -width / 2, -height / 2, 0);
                SetVertPos(1, -width / 2,  height / 2, 0);
                SetVertPos(2,  width / 2,  height / 2, 0);
                SetVertPos(3,  width / 2, -height / 2, 0);
            }
            else
            {
                SetVertPos(0,     0,      0, 0);
                SetVertPos(1,     0, height, 0);
                SetVertPos(2, width, height, 0);
                SetVertPos(3, width,      0, 0);
            }
            DeclareTriangle(0, 0, 1, 2);
            DeclareTriangle(1, 0, 3, 2);
        }

    }
}