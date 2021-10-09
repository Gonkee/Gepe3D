

using System;
using System.Collections.Generic;
using Gepe3D.Core;
using OpenTK.Mathematics;

namespace Gepe3D.Physics
{
    public class SoftBody : PhysicsBody
    {

        private float springConstant = 40;
        private float dampingConstant = 0.7f;

        private struct Spring
        {
            public readonly int mass1;
            public readonly int mass2;
            public readonly float initialLength;

            public Spring(int mass1, int mass2, float initialLength)
            {
                this.mass1 = mass1;
                this.mass2 = mass2;
                this.initialLength = initialLength;
            }
        }

        public static readonly float GROUND_Y = -3f;

        private readonly float nRT_pressureConstant;

        float gravity = 1;

        List<Spring> springs = new List<Spring>();
        Dictionary<long, int> existingSprings = new Dictionary<long, int>();

        float totalMass = 4;

        private readonly float[] state;

        private readonly float massPerPoint;

        private int  x (int id) { return id * 6 + 0; }
        private int  y (int id) { return id * 6 + 1; }
        private int  z (int id) { return id * 6 + 2; }
        private int vx (int id) { return id * 6 + 3; }
        private int vy (int id) { return id * 6 + 4; }
        private int vz (int id) { return id * 6 + 5; }

        public SoftBody(Geometry geometry, Material material) : base(geometry, material)
        {

            state = new float[geometry.vertices.Count * 6];
            massPerPoint = totalMass / geometry.vertices.Count;

            for (int i = 0; i < vertices.Count; i++)
            {
                state[ x(i) ] = geometry.vertices[i].X;
                state[ y(i) ] = geometry.vertices[i].Y;
                state[ z(i) ] = geometry.vertices[i].Z;
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
            nRT_pressureConstant = volume * 50; // nRT is proportional to volume (Avogadro's Law)
        }

        private int GenSpringID(int m1_ID, int m2_ID)
        {
            // check if spring has already been generated for these two masses
            int id0 = Math.Min(m1_ID, m2_ID);
            int id1 = Math.Max(m1_ID, m2_ID);
            long uniqueCombination = ( (long) id0 << 32) + (long) id1;

            if ( existingSprings.ContainsKey(uniqueCombination) )
            {
                return existingSprings[uniqueCombination];
            }

            // generate new spring
            float dx2 = (state[ x(id1) ] - state[ x(id0) ]) * (state[ x(id1) ] - state[ x(id0) ]);
            float dy2 = (state[ y(id1) ] - state[ y(id0) ]) * (state[ y(id1) ] - state[ y(id0) ]);
            float dz2 = (state[ z(id1) ] - state[ z(id0) ]) * (state[ z(id1) ] - state[ z(id0) ]);
            float initialLength = MathF.Sqrt( dx2 + dy2 + dz2 );
            Spring spring = new Spring( (int) id0, (int) id1, initialLength );

            // add new spring
            int newID = springs.Count; 
            existingSprings.Add(uniqueCombination, newID);
            springs.Add(spring);

            return newID;
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

        public override float[] GetState()
        {
            return state;
        }

        public override float[] GetDerivative(float[] state)
        {
            float[] derivative = new float[state.Length];

            foreach (Spring spr in springs)
            {
                float  x1 = state[  x(spr.mass1) ];
                float  y1 = state[  y(spr.mass1) ];
                float  z1 = state[  z(spr.mass1) ];
                float vx1 = state[ vx(spr.mass1) ];
                float vy1 = state[ vy(spr.mass1) ];
                float vz1 = state[ vz(spr.mass1) ];

                float  x2 = state[  x(spr.mass2) ];
                float  y2 = state[  y(spr.mass2) ];
                float  z2 = state[  z(spr.mass2) ];
                float vx2 = state[ vx(spr.mass2) ];
                float vy2 = state[ vy(spr.mass2) ];
                float vz2 = state[ vz(spr.mass2) ];

                Vector3 posDiff = new Vector3( x2 - x1, y2 - y1, z2 - z1 );
                if (posDiff.Length == 0) continue;
                Vector3 velDiff = new Vector3( vx2 - vx1, vy2 - y1, vz2 - vz1 );
                Vector3 posDiffNorm = posDiff.Normalized();

                float contractionForce = (posDiff.Length - spr.initialLength)  * springConstant;
                float dampingForce     = Vector3.Dot(velDiff, posDiffNorm) * dampingConstant;
                float totalForce = contractionForce + dampingForce;

                Vector3 m1_force = totalForce * posDiffNorm;
                Vector3 m2_force = -m1_force;

                derivative[ vx(spr.mass1) ] += m1_force.X / massPerPoint;
                derivative[ vy(spr.mass1) ] += m1_force.Y / massPerPoint;
                derivative[ vz(spr.mass1) ] += m1_force.Z / massPerPoint;

                derivative[ vx(spr.mass2) ] += m2_force.X / massPerPoint;
                derivative[ vy(spr.mass2) ] += m2_force.Y / massPerPoint;
                derivative[ vz(spr.mass2) ] += m2_force.Z / massPerPoint;
            }

            float volume = GetVolume();
            volume = Math.Max(volume, 0.01f); // avoid divide by 0

            foreach (Vector3i tri in triangles)
            {
                Vector3 v1 = new Vector3( state[ x(tri.X) ], state[ y(tri.X) ], state[ z(tri.X) ] );
                Vector3 v2 = new Vector3( state[ x(tri.Y) ], state[ y(tri.Y) ], state[ z(tri.Y) ] );
                Vector3 v3 = new Vector3( state[ x(tri.Z) ], state[ y(tri.Z) ], state[ z(tri.Z) ] );

                Vector3 crossProduct = Vector3.Cross( v2 - v1, v3 - v1 );
                Vector3 normal = crossProduct.Normalized();
                float triangleArea = crossProduct.Length / 2;

                // PV = nRT   therefore   P = nRT / V
                // P = F / A  therefore   nRT / V = F / A
                // F = nRT * A / V
                float pressureForce = nRT_pressureConstant * triangleArea / volume;
                pressureForce /= 3; // distribute the triangle's force into the 3 vertices

                Vector3 forceVector = normal * pressureForce;

                derivative[ vx(tri.X) ] += forceVector.X / massPerPoint;
                derivative[ vy(tri.X) ] += forceVector.Y / massPerPoint;
                derivative[ vz(tri.X) ] += forceVector.Z / massPerPoint;
                
                derivative[ vx(tri.Y) ] += forceVector.X / massPerPoint;
                derivative[ vy(tri.Y) ] += forceVector.Y / massPerPoint;
                derivative[ vz(tri.Y) ] += forceVector.Z / massPerPoint;
                
                derivative[ vx(tri.Z) ] += forceVector.X / massPerPoint;
                derivative[ vy(tri.Z) ] += forceVector.Y / massPerPoint;
                derivative[ vz(tri.Z) ] += forceVector.Z / massPerPoint;
            }

            for (int i = 0; i < state.Length / 6; i++)
            {
                derivative[ vy(i) ] -= gravity;

                // set the position derivative to just equal the velocity values
                derivative[ x(i) ] = state[ vx(i) ];
                derivative[ y(i) ] = state[ vy(i) ];
                derivative[ z(i) ] = state[ vz(i) ];
            }

            return derivative;
        }

        public override void UpdateState(float[] change)
        {
            for (int i = 0; i < state.Length / 6; i++)
            {
                state[  x(i) ] += change[  x(i) ];
                state[  y(i) ] += change[  y(i) ];
                state[  z(i) ] += change[  z(i) ];
                state[ vx(i) ] += change[ vx(i) ];
                state[ vy(i) ] += change[ vy(i) ];
                state[ vz(i) ] += change[ vz(i) ];

                if (state[  y(i) ] < GROUND_Y)
                {
                    state[  y(i) ] = GROUND_Y;
                    state[ vy(i) ] = Math.Max( state[ vy(i) ], 0 );
                }

                SetVertexPos( i, state[ x(i) ], state[ y(i) ], state[ z(i) ] );
            }
        }

    }
}