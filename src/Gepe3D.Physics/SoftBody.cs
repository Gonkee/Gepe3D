

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

            private float springConstant = 10;
            private float dampingConstant = 0.4f;

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

                // magnitude of damp force must be less than magnitude of contraction (spring) force
                // float dampMagnitude = Math.Min( Math.Abs(dampingForce), 0.99f * Math.Abs(contractionForce) );
                // dampingForce = Math.Sign(dampingForce) * dampMagnitude;

                // if contraction & total forces are in opposite directions,
                // that means damping force is overpowering the contraction (spring) force
                // that will result in bugginess
                float totalForce = contractionForce + dampingForce;
                // if (contractionForce * totalForce < 0) totalForce = 0;

                Vector3 m1_force = totalForce * posDiffNorm;
                Vector3 m2_force = -m1_force;

                m1.ApplyForce(m1_force.X, m1_force.Y, m1_force.Z);
                m2.ApplyForce(m2_force.X, m2_force.Y, m2_force.Z);
            }

        }

        public static readonly float GROUND_Y = -1.5f;

        private readonly float nRT_pressureConstant;

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

            float volume = GetVolume();
            nRT_pressureConstant = volume * 20; // nRT is proportional to volume (Avogadro's Law)
            System.Console.WriteLine(volume);
            System.Console.WriteLine(nRT_pressureConstant);
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

            float volume = GetVolume();
            volume = Math.Max(volume, 0.01f); // avoid divide by 0

            foreach (Vector3i tri in triangles)
            {
                Vector3 v1 = vertices[tri.X];
                Vector3 v2 = vertices[tri.Y];
                Vector3 v3 = vertices[tri.Z];

                Vector3 crossProduct = Vector3.Cross( v2 - v1, v3 - v1 );
                Vector3 normal = crossProduct.Normalized();
                float triangleArea = crossProduct.Length / 2;

                // PV = nRT   therefore   P = nRT / V
                // P = F / A  therefore   nRT / V = F / A
                // F = nRT * A / V
                float pressureForce = nRT_pressureConstant * triangleArea / volume;
                pressureForce /= 3; // distribute the triangle's force into the 3 vertices

                Vector3 forceVector = normal * pressureForce;
                masses[tri.X].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);
                masses[tri.Y].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);
                masses[tri.Z].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);

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

        private float GetVolume()
        {
            float volume = 0;
            foreach (Vector3i tri in triangles)
            {
                Vector3 v1 = vertices[tri.X];
                Vector3 v2 = vertices[tri.Y];
                Vector3 v3 = vertices[tri.Z];
                volume += SignedVolumeOfTriangle(v1, v2, v3);
            }
            return Math.Abs(volume);
        }

        public float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
            var v321 = p3.X * p2.Y * p1.Z;
            var v231 = p2.X * p3.Y * p1.Z;
            var v312 = p3.X * p1.Y * p2.Z;
            var v132 = p1.X * p3.Y * p2.Z;
            var v213 = p2.X * p1.Y * p3.Z;
            var v123 = p1.X * p2.Y * p3.Z;
            return (1.0f / 6.0f) * (-v321 + v231 + v312 - v132 - v213 + v123);
        }

    }
}