

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

            private struct State
            {
                public float m1_posX, m1_posY, m1_posZ;
                public float m2_posX, m2_posY, m2_posZ;
                public float m1_velX, m1_velY, m1_velZ;
                public float m2_velX, m2_velY, m2_velZ;
            }

            private struct Derivative
            {
                public float m1_dPX, m1_dPY, m1_dPZ;
                public float m2_dPX, m2_dPY, m2_dPZ;
                public float m1_dVX, m1_dVY, m1_dVZ;
                public float m2_dVX, m2_dVY, m2_dVZ;
            }

            public Spring(PointMass m1, PointMass m2)
            {
                this.m1 = m1;
                this.m2 = m2;

                float dx2 = (m2.x - m1.x) * (m2.x - m1.x);
                float dy2 = (m2.y - m1.y) * (m2.y - m1.y);
                float dz2 = (m2.z - m1.z) * (m2.z - m1.z);
                initialLength = MathF.Sqrt( dx2 + dy2 + dz2 );
            }

            private Derivative GetDerivativeAtTime(State initialState, float delta, Derivative derivative)
            {
                State stateNew = new State();
                stateNew.m1_posX = initialState.m1_posX + derivative.m1_dPX;
                stateNew.m1_posY = initialState.m1_posY + derivative.m1_dPY;
                stateNew.m1_posZ = initialState.m1_posZ + derivative.m1_dPZ;
                stateNew.m1_velX = initialState.m1_velX + derivative.m1_dVX;
                stateNew.m1_velY = initialState.m1_velY + derivative.m1_dVY;
                stateNew.m1_velZ = initialState.m1_velZ + derivative.m1_dVZ;

                stateNew.m2_posX = initialState.m2_posX + derivative.m2_dPX;
                stateNew.m2_posY = initialState.m2_posY + derivative.m2_dPY;
                stateNew.m2_posZ = initialState.m2_posZ + derivative.m2_dPZ;
                stateNew.m2_velX = initialState.m2_velX + derivative.m2_dVX;
                stateNew.m2_velY = initialState.m2_velY + derivative.m2_dVY;
                stateNew.m2_velZ = initialState.m2_velZ + derivative.m2_dVZ;

                Derivative output = new Derivative();
                output.m1_dPX = stateNew.m1_velX;
                output.m1_dPY = stateNew.m1_velY;
                output.m1_dPZ = stateNew.m1_velZ;

                output.m2_dPX = stateNew.m2_velX;
                output.m2_dPY = stateNew.m2_velY;
                output.m2_dPZ = stateNew.m2_velZ;
                ApplyAccel(stateNew, output);
                return output;
            }

            private void ApplyAccel(State state, Derivative toFill)
            {
                Vector3 posDiff = new Vector3(
                    state.m2_posX - state.m1_posX,
                    state.m2_posY - state.m1_posY,
                    state.m2_posZ - state.m1_posZ
                );

                if (posDiff.Length == 0) return;

                Vector3 velDiff = new Vector3(
                    state.m2_velX - state.m1_velX,
                    state.m2_velY - state.m1_velY,
                    state.m2_velZ - state.m1_velZ
                );

                Vector3 posDiffNorm = posDiff.Normalized();

                float contractionForce = (posDiff.Length - initialLength)  * springConstant;
                float dampingForce     = Vector3.Dot(velDiff, posDiffNorm) * dampingConstant;
                float totalForce = contractionForce + dampingForce;

                Vector3 m1_force = totalForce * posDiffNorm;
                Vector3 m2_force = -m1_force;

                // return derivative
                // m1.ApplyForce(m1_force.X, m1_force.Y, m1_force.Z);
                // m2.ApplyForce(m2_force.X, m2_force.Y, m2_force.Z);

                toFill.m1_dVX = m1_force.X / m1.mass;
                toFill.m1_dVY = m1_force.Y / m1.mass;
                toFill.m1_dVZ = m1_force.Z / m1.mass;

                toFill.m2_dVX = m2_force.X / m2.mass;
                toFill.m2_dVY = m2_force.Y / m2.mass;
                toFill.m2_dVZ = m2_force.Z / m2.mass;
            }

            public void Update(float delta)
            {
                // Attempted Runge-Kutta 4

                State currentState = new State();
                currentState.m1_posX = m1.x;
                currentState.m1_posY = m1.y;
                currentState.m1_posZ = m1.z;
                currentState.m1_velX = m1.velX;
                currentState.m1_velY = m1.velY;
                currentState.m1_velZ = m1.velZ;

                currentState.m2_posX = m2.x;
                currentState.m2_posY = m2.y;
                currentState.m2_posZ = m2.z;
                currentState.m2_velX = m2.velX;
                currentState.m2_velY = m2.velY;
                currentState.m2_velZ = m2.velZ;

                Derivative a, b, c, d;
                a = GetDerivativeAtTime(currentState, 0, new Derivative() );
                b = GetDerivativeAtTime(currentState, delta * 0.5f, a);
                c = GetDerivativeAtTime(currentState, delta * 0.5f, b);
                d = GetDerivativeAtTime(currentState, delta * 0.5f, c);

                float m1_dPX = 1.0f / 6.0f * ( a.m1_dPX + 2.0f * ( b.m1_dPX + c.m1_dPX ) + d.m1_dPX );
                float m1_dPY = 1.0f / 6.0f * ( a.m1_dPY + 2.0f * ( b.m1_dPY + c.m1_dPY ) + d.m1_dPY );
                float m1_dPZ = 1.0f / 6.0f * ( a.m1_dPZ + 2.0f * ( b.m1_dPZ + c.m1_dPZ ) + d.m1_dPZ );
                float m2_dPX = 1.0f / 6.0f * ( a.m2_dPX + 2.0f * ( b.m2_dPX + c.m2_dPX ) + d.m2_dPX );
                float m2_dPY = 1.0f / 6.0f * ( a.m2_dPY + 2.0f * ( b.m2_dPY + c.m2_dPY ) + d.m2_dPY );
                float m2_dPZ = 1.0f / 6.0f * ( a.m2_dPZ + 2.0f * ( b.m2_dPZ + c.m2_dPZ ) + d.m2_dPZ );
                
                float m1_dVX = 1.0f / 6.0f * ( a.m1_dVX + 2.0f * ( b.m1_dVX + c.m1_dVX ) + d.m1_dVX );
                float m1_dVY = 1.0f / 6.0f * ( a.m1_dVY + 2.0f * ( b.m1_dVY + c.m1_dVY ) + d.m1_dVY );
                float m1_dVZ = 1.0f / 6.0f * ( a.m1_dVZ + 2.0f * ( b.m1_dVZ + c.m1_dVZ ) + d.m1_dVZ );
                float m2_dVX = 1.0f / 6.0f * ( a.m2_dVX + 2.0f * ( b.m2_dVX + c.m2_dVX ) + d.m2_dVX );
                float m2_dVY = 1.0f / 6.0f * ( a.m2_dVY + 2.0f * ( b.m2_dVY + c.m2_dVY ) + d.m2_dVY );
                float m2_dVZ = 1.0f / 6.0f * ( a.m2_dVZ + 2.0f * ( b.m2_dVZ + c.m2_dVZ ) + d.m2_dVZ );


                m1.x += m1_dPX * delta;
                m1.y += m1_dPY * delta;
                m1.z += m1_dPZ * delta;
                m2.x += m2_dPX * delta;
                m2.y += m2_dPY * delta;
                m2.z += m2_dPZ * delta;

                m1.velX += m1_dVX * delta;
                m1.velY += m1_dVY * delta;
                m1.velZ += m1_dVZ * delta;
                m2.velX += m2_dVX * delta;
                m2.velY += m2_dVY * delta;
                m2.velZ += m2_dVZ * delta;

                // return derivative
                // m1.ApplyForce(m1_force.X, m1_force.Y, m1_force.Z);
                // m2.ApplyForce(m2_force.X, m2_force.Y, m2_force.Z);
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
                spr.Update(Global.Delta);
            }

            float volume = GetVolume();
            volume = Math.Max(volume, 0.01f); // avoid divide by 0

            // foreach (Vector3i tri in triangles)
            // {
            //     Vector3 v1 = vertices[tri.X];
            //     Vector3 v2 = vertices[tri.Y];
            //     Vector3 v3 = vertices[tri.Z];

            //     Vector3 crossProduct = Vector3.Cross( v2 - v1, v3 - v1 );
            //     Vector3 normal = crossProduct.Normalized();
            //     float triangleArea = crossProduct.Length / 2;

            //     // PV = nRT   therefore   P = nRT / V
            //     // P = F / A  therefore   nRT / V = F / A
            //     // F = nRT * A / V
            //     float pressureForce = nRT_pressureConstant * triangleArea / volume;
            //     pressureForce /= 3; // distribute the triangle's force into the 3 vertices

            //     Vector3 forceVector = normal * pressureForce;
            //     masses[tri.X].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);
            //     masses[tri.Y].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);
            //     masses[tri.Z].ApplyForce(forceVector.X, forceVector.Y, forceVector.Z);

            // }

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