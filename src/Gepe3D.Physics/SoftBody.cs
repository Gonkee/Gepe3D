

using System;
using System.Collections.Generic;
using Gepe3D.Core;
using OpenTK.Mathematics;

namespace Gepe3D.Physics
{
    public class SoftBody : PhysicsBody
    {

        private class Spring
        {
            public readonly PointMass m1;
            public readonly PointMass m2;

            private readonly float initialLength;

            private float springConstant = 2;
            private float dampingConstant = 0.2f;

            public Spring(PointMass m1, PointMass m2)
            {
                this.m1 = m1;
                this.m2 = m2;

                float dx2 = (m2.x - m1.x) * (m2.x - m1.x);
                float dy2 = (m2.y - m1.y) * (m2.y - m1.y);
                float dz2 = (m2.z - m1.z) * (m2.z - m1.z);
                initialLength = MathF.Sqrt( dx2 + dy2 + dz2 );
            }

            public void Update()
            {
                Vector3 posDiff = new Vector3(
                    m2.x - m1.x,
                    m2.y - m1.y,
                    m2.z - m1.z
                );

                if (posDiff.Length == 0) return;

                Vector3 velDiff = new Vector3(
                    m2.velX - m1.velX,
                    m2.velY - m1.velY,
                    m2.velZ - m1.velZ
                );

                Vector3 posDiffNorm = posDiff.Normalized();

                float contractionForce = (posDiff.Length - initialLength)  * springConstant;
                float dampingForce     = Vector3.Dot(velDiff, posDiffNorm) * dampingConstant;
                float totalForce = contractionForce + dampingForce;

                // if contraction & total forces are in opposite directions,
                // that means damping force is overpowering the contraction (spring) force
                // that will result in bugginess
                if (contractionForce * totalForce < 0) totalForce = 0;

                Vector3 m1_force = totalForce * posDiffNorm;
                Vector3 m2_force = -m1_force;

                m1.ApplyForce(m1_force.X, m1_force.Y, m1_force.Z);
                m2.ApplyForce(m2_force.X, m2_force.Y, m2_force.Z);
            }

        }

        public static readonly float GROUND_Y = -1.5f;

        float gravity = 1;

        PointMass[] masses;
        List<Spring> springs = new List<Spring>();
        Dictionary<long, int> existingSprings = new Dictionary<long, int>();

        float totalMass = 2;


        public SoftBody(Geometry geometry, Material material) : base(geometry, material)
        {
            int massCount = geometry.vertices.Count;
            masses = new PointMass[massCount];
            for (int i = 0; i < masses.Length; i++)
            {
                float x = geometry.vertices[i].X;
                float y = geometry.vertices[i].Y;
                float z = geometry.vertices[i].Z;
                masses[i] = new PointMass(totalMass / massCount, x, y, z);
            }

            foreach (Vector3i tri in geometry.triangleIDs)
            {
                int id1 = tri.X;
                int id2 = tri.Y;
                int id3 = tri.Z;

                GenSpringID(id1, id2);
                GenSpringID(id2, id3);
                GenSpringID(id3, id1);
            }

        }

        private int GenSpringID(int m1_ID, int m2_ID)
        {
            // check if spring has already been generated for these two masses
            long id0 = Math.Min(m1_ID, m2_ID);
            long id1 = Math.Max(m1_ID, m2_ID);
            long uniqueCombination = (id0 << 32) + id1;

            if ( existingSprings.ContainsKey(uniqueCombination) )
            {
                return existingSprings[uniqueCombination];
            }

            // generate new spring
            Spring spring = new Spring( masses[id0], masses[id1] );
            int newID = springs.Count; 
            existingSprings.Add(uniqueCombination, newID);
            springs.Add(spring);

            return newID;
        }

        public override void Update()
        {
            foreach (Spring spr in springs)
            {
                spr.Update();
            }

            for (int i = 0; i < masses.Length; i++)
            {
                PointMass p = masses[i];

                p.ApplyForce(0, -gravity * p.mass, 0);

                p.Update();
                p.ClearForces();

                if (p.y < GROUND_Y)
                {
                    p.y = GROUND_Y;
                    p.velY = MathHelper.Max(p.velY, 0);
                }

                SetVertexPos(i, p.x, p.y, p.z);

            }
        }

    }
}