using System;
using Gepe3D.Core;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Entities
{
    public class CubeMesh : Mesh
    {

        public CubeMesh(float width, float height, float depth, bool centeredOrigin) : base(8, 12)
        {
            
            if (centeredOrigin)
            {
                SetVertPos(0, -width / 2, -height / 2, -depth / 2);
                SetVertPos(1, -width / 2, -height / 2,  depth / 2);
                SetVertPos(2,  width / 2, -height / 2,  depth / 2);
                SetVertPos(3,  width / 2, -height / 2, -depth / 2);
                SetVertPos(4, -width / 2,  height / 2, -depth / 2);
                SetVertPos(5, -width / 2,  height / 2,  depth / 2);
                SetVertPos(6,  width / 2,  height / 2,  depth / 2);
                SetVertPos(7,  width / 2,  height / 2, -depth / 2);
            }
            else
            {
                SetVertPos(0,     0,      0,     0);
                SetVertPos(1,     0,      0, depth);
                SetVertPos(2, width,      0, depth);
                SetVertPos(3, width,      0,     0);
                SetVertPos(4,     0, height,     0);
                SetVertPos(5,     0, height, depth);
                SetVertPos(6, width, height, depth);
                SetVertPos(7, width, height,     0);
            }
            DeclareTriangle( 0, 0, 1, 2);
            DeclareTriangle( 1, 0, 3, 2);
            DeclareTriangle( 2, 0, 1, 5);
            DeclareTriangle( 3, 0, 4, 5);
            DeclareTriangle( 4, 1, 2, 6);
            DeclareTriangle( 5, 1, 5, 6);
            DeclareTriangle( 6, 2, 3, 7);
            DeclareTriangle( 7, 2, 6, 7);
            DeclareTriangle( 8, 3, 0, 4);
            DeclareTriangle( 9, 3, 7, 4);
            DeclareTriangle(10, 4, 5, 6);
            DeclareTriangle(11, 4, 7, 6);
        }

    }
}