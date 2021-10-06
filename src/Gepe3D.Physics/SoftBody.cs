

using System;
using System.Collections.Generic;
using Gepe3D.Core;
using OpenTK.Mathematics;

namespace Gepe3D.Physics
{
    public class SoftBody : PhysicsBody
    {

        public static readonly float GROUND_Y = -4;

        float gravity = 0;

        Vector3[] velocities;


        public SoftBody(Geometry geometry, Material material) : base(geometry, material)
        {
            velocities = new Vector3[geometry.vertices.Count];
            for (int i = 0; i < velocities.Length; i++)
            {
                velocities[i] = new Vector3();
            }
        }

        public override void Update()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                float x = vertices[i].X;
                float y = vertices[i].Y;
                float z = vertices[i].Z;

                velocities[i] = velocities[i] + new Vector3(0, -gravity * Global.Delta, 0);

                float nx = x + velocities[i].X * Global.Delta;
                float ny = y + velocities[i].Y * Global.Delta;
                float nz = z + velocities[i].Z * Global.Delta;

                if (ny < GROUND_Y)
                {
                    ny = GROUND_Y;
                    velocities[i] = new Vector3(velocities[i].X, MathHelper.Max(velocities[i].Y, 0), velocities[i].Z);
                }

                SetVertexPos(i,
                x + velocities[i].X * Global.Delta,
                y + velocities[i].Y * Global.Delta,
                z + velocities[i].Z * Global.Delta);


            }
        }

    }
}