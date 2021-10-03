using System;
using Gepe3D.Core;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D.Entities
{
    public class CubeMesh : Mesh
    {

        public CubeMesh(float width, float height, float depth, bool centeredOrigin)
        {
            // first letter = b or t (bottom/top)
            // second letter = l or r (left/right)
            // third letter = f or b (front/back)
            int blf, blb, brb, brf, tlf, tlb, trb, trf;
            if (centeredOrigin)
            {
                blf = AddVertex(-width / 2, -height / 2, -depth / 2);
                blb = AddVertex(-width / 2, -height / 2,  depth / 2);
                brb = AddVertex( width / 2, -height / 2,  depth / 2);
                brf = AddVertex( width / 2, -height / 2, -depth / 2);
                tlf = AddVertex(-width / 2,  height / 2, -depth / 2);
                tlb = AddVertex(-width / 2,  height / 2,  depth / 2);
                trb = AddVertex( width / 2,  height / 2,  depth / 2);
                trf = AddVertex( width / 2,  height / 2, -depth / 2);
            }
            else
            {
                blf = AddVertex(    0,      0,     0);
                blb = AddVertex(    0,      0, depth);
                brb = AddVertex(width,      0, depth);
                brf = AddVertex(width,      0,     0);
                tlf = AddVertex(    0, height,     0);
                tlb = AddVertex(    0, height, depth);
                trb = AddVertex(width, height, depth);
                trf = AddVertex(width, height,     0);
            }
            AddTriangle(blf, brb, blb);
            AddTriangle(blf, brf, brb);
            AddTriangle(blf, blb, tlb);
            AddTriangle(blf, tlb, tlf);
            AddTriangle(blb, brb, trb);
            AddTriangle(blb, trb, tlb);
            AddTriangle(brb, brf, trf);
            AddTriangle(brb, trf, trb);
            AddTriangle(brf, blf, tlf);
            AddTriangle(brf, tlf, trf);
            AddTriangle(tlf, tlb, trb);
            AddTriangle(tlf, trb, trf);
        }

    }
}