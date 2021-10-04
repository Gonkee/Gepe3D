
using Gepe3D.Entities;
using Gepe3D.Util;
using OpenTK.Graphics.OpenGL4;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D.Core
{
    public class SkyBox : Entity
    {

        private class SkyBoxMesh : Mesh
        {
            private static readonly float SIDE_LENGTH = 200;

            public SkyBoxMesh()
            {

                // first letter = b or t (bottom/top)
                // second letter = l or r (left/right)
                // third letter = f or b (front/back)
                int blf, blb, brb, brf, tlf, tlb, trb, trf;
                
                blf = AddVertex(-SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                blb = AddVertex(-SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                brb = AddVertex( SIDE_LENGTH / 2, -SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                brf = AddVertex( SIDE_LENGTH / 2, -SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                tlf = AddVertex(-SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                tlb = AddVertex(-SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                trb = AddVertex( SIDE_LENGTH / 2,  SIDE_LENGTH / 2,  SIDE_LENGTH / 2);
                trf = AddVertex( SIDE_LENGTH / 2,  SIDE_LENGTH / 2, -SIDE_LENGTH / 2);
                
                // counter clockwise specification, faces facing inward
                AddTriangle(blf, blb, brb);
                AddTriangle(blf, brb, brf);
                AddTriangle(blf, tlb, blb);
                AddTriangle(blf, tlf, tlb);
                AddTriangle(blb, trb, brb);
                AddTriangle(blb, tlb, trb);
                AddTriangle(brb, trf, brf);
                AddTriangle(brb, trb, trf);
                AddTriangle(brf, tlf, blf);
                AddTriangle(brf, trf, tlf);
                AddTriangle(tlf, trb, tlb);
                AddTriangle(tlf, trf, trb);
            }
        }

        public SkyBox() : base(new SkyBoxMesh(), new Material())
        {
        }
        
    }
}