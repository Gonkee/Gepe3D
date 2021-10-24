

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class SoftBody : PhysicsBody, Renderable
    {

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

        private static readonly float GRAVITY = 9.8f;
        private static readonly float SPRING_CONSTANT = 60;
        private static readonly float DAMPING_CONSTANT = 1.3f;
        private readonly float nRT_pressureConstant;

        List<Spring> springs = new List<Spring>();
        Dictionary<long, int> existingSprings = new Dictionary<long, int>();
        private float _maxX, _minX, _maxY, _minY, _maxZ, _minZ;

        float totalMass = 4;
        private readonly float massPerPoint;
        private readonly ParticleData state;

        private readonly Mesh mesh;
        
        public override float MaxX() { return _maxX; }
        public override float MinX() { return _minX; }
        public override float MaxY() { return _maxY; }
        public override float MinY() { return _minY; }
        public override float MaxZ() { return _maxZ; }
        public override float MinZ() { return _minZ; }

        public SoftBody(Geometry geometry, Material material)
        {

            state = new ParticleData(geometry.Vertices.Count);
            massPerPoint = totalMass / geometry.Vertices.Count;

            mesh = new Mesh(geometry, material);

            for (int i = 0; i < geometry.Vertices.Count; i++)
            {
                state.SetPos(
                    i,
                    geometry.Vertices[i].X,
                    geometry.Vertices[i].Y,
                    geometry.Vertices[i].Z
                );
            }

            foreach (Vector3i tri in geometry.TriangleIDs)
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
            float initialLength = ( state.GetPos(id1) - state.GetPos(id0) ).Length;
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
            foreach (Vector3i tri in mesh.Geometry.TriangleIDs)
            {
                Vector3 v1 = state.GetPos(tri.X);
                Vector3 v2 = state.GetPos(tri.Y);
                Vector3 v3 = state.GetPos(tri.Z);
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

        public override PhysicsData GetState()
        {
            return state;
        }

        public override PhysicsData GetDerivative(PhysicsData pstate)
        {
            ParticleData state = new ParticleData(pstate);
            ParticleData derivative = new ParticleData(state.ParticleCount);

            foreach (Spring spr in springs)
            {
                Vector3 posDiff = state.GetPos(spr.mass2) - state.GetPos(spr.mass1);
                if (posDiff.Length == 0) continue;
                Vector3 velDiff = state.GetVel(spr.mass2) - state.GetVel(spr.mass1);
                Vector3 posDiffNorm = posDiff.Normalized();

                float contractionForce = (posDiff.Length - spr.initialLength)  * SPRING_CONSTANT;
                float dampingForce     = Vector3.Dot(velDiff, posDiffNorm) * DAMPING_CONSTANT;
                float totalForce = contractionForce + dampingForce;

                Vector3 m1_force = totalForce * posDiffNorm;
                Vector3 m2_force = -m1_force;

                derivative.AddVel( spr.mass1, m1_force / massPerPoint );
                derivative.AddVel( spr.mass2, m2_force / massPerPoint );
            }

            float volume = GetVolume();
            volume = Math.Max(volume, 0.01f); // avoid divide by 0

            foreach (Vector3i tri in mesh.Geometry.TriangleIDs)
            {
                Vector3 v1 = state.GetPos(tri.X);
                Vector3 v2 = state.GetPos(tri.Y);
                Vector3 v3 = state.GetPos(tri.Z);

                Vector3 crossProduct = Vector3.Cross( v2 - v1, v3 - v1 );
                Vector3 normal = crossProduct.Normalized();
                float triangleArea = crossProduct.Length / 2;

                // PV = nRT   therefore   P = nRT / V
                // P = F / A  therefore   nRT / V = F / A
                // F = nRT * A / V
                float pressureForce = nRT_pressureConstant * triangleArea / volume;
                pressureForce /= 3; // distribute the triangle's force into the 3 vertices

                Vector3 forceVector = normal * pressureForce;

                derivative.AddVel( tri.X, forceVector / massPerPoint );
                derivative.AddVel( tri.Y, forceVector / massPerPoint );
                derivative.AddVel( tri.Z, forceVector / massPerPoint );
            }

            for (int i = 0; i < state.ParticleCount; i++)
            {
                derivative.AddVel( i, 0, -GRAVITY, 0 );
                derivative.SetPos( i, state.GetVel(i) );
            }

            return derivative;
        }

        public override void UpdateState(PhysicsData pchange, List<PhysicsBody> bodies)
        {
            ParticleData change = new ParticleData(pchange);

            _maxX = float.MinValue;
            _minX = float.MaxValue;
            _maxY = float.MinValue;
            _minY = float.MaxValue;
            _maxZ = float.MinValue;
            _minZ = float.MaxValue;

            for (int i = 0; i < state.ParticleCount; i++)
            {
                Vector3 current  = state.GetPos(i);
                Vector3 movement = change.GetPos(i);
                Vector3 velocity = state.GetVel(i);

                foreach (PhysicsBody body in bodies) // keep clipping the movement if colliding
                {
                    if (body == this) continue;
                    Mesh bodyMesh = body.GetMesh();
                    if (bodyMesh == null) continue;

                    if ( !(
                        current.X + movement.X > body.MinX() &&
                        current.X + movement.X < body.MaxX() &&
                        current.Y + movement.Y > body.MinY() &&
                        current.Y + movement.Y < body.MaxY() &&
                        current.Z + movement.Z > body.MinZ() &&
                        current.Z + movement.Z < body.MaxZ() )
                    ) continue;

                    Vector3 A, B, C, normal, crossProduct;
                    foreach (Vector3i tri in bodyMesh.Geometry.TriangleIDs)
                    {
                        A = bodyMesh.Geometry.Vertices[tri.X];
                        B = bodyMesh.Geometry.Vertices[tri.Y];
                        C = bodyMesh.Geometry.Vertices[tri.Z];
                        crossProduct = Vector3.Cross(B - A, C - A);
                        normal = crossProduct.Normalized();
                        float triangleArea = crossProduct.Length / 2;

                        // line-plane intersection
                        float normalDotToPlane  = Vector3.Dot(A - current, normal);
                        float normalDotMovement = Vector3.Dot(movement, normal);

                        if (
                            normalDotToPlane  >= 0 ||    // to plane is same direction as normal, currently behind plane
                            normalDotMovement >= 0 ||    // movement is same direction as normal, moving away from plane
                            // to plane must be less negative than movement, so intersection is within range
                            normalDotToPlane < normalDotMovement
                        ) continue;

                        // P = point of intersection with plane
                        Vector3 P = current + (normalDotToPlane / normalDotMovement) * movement;

                        float areaPAB = Vector3.Cross(A - P, B - P).Length / 2;
                        float areaPAC = Vector3.Cross(A - P, C - P).Length / 2;
                        float areaPBC = Vector3.Cross(B - P, C - P).Length / 2;
                        float barycentricA = areaPBC / triangleArea;
                        float barycentricB = areaPAC / triangleArea;
                        float barycentricC = areaPAB / triangleArea;
                        float totalBarycentric = barycentricA + barycentricB + barycentricC;
                        if (    // check if point of intersection is within triangle
                            !(
                                barycentricA > 0 && barycentricA < 1 &&
                                barycentricB > 0 && barycentricB < 1 &&
                                barycentricC > 0 && barycentricC < 1 &&
                                totalBarycentric > 0.99 && totalBarycentric < 1.01
                            )
                        ) continue;

                        P += normal * 0.01f; // small buffer distance
                        movement = P - current;
                        velocity -= Vector3.Dot(velocity, normal) * normal;
                        velocity *= 0.5f; // friction? i dont even know
                        // velocity = new Vector3();
                    }

                }
                state.AddPos(i, movement);
                state.SetVel(i, velocity + change.GetVel(i) );

                Vector3 pos = state.GetPos(i);
                float x = pos.X, y = pos.Y, z = pos.Z;

                mesh.SetVertexPos( i, x, y, z );
                
                _maxX = Math.Max( _maxX, x );
                _minX = Math.Min( _minX, x );
                _maxY = Math.Max( _maxY, y );
                _minY = Math.Min( _minY, y );
                _maxZ = Math.Max( _maxZ, z );
                _minZ = Math.Min( _minZ, z );
            }
        }

        public override Mesh GetMesh()
        {
            return mesh;
        }
        
        public override void Draw()
        {
            mesh.Draw();
        }
        
        public override void Render(Renderer renderer)
        {
            Shader shader = renderer.UseShader("entity");
            shader.SetVector3("lightPos", renderer.LightPos);
            shader.SetVector3("ambientLight", renderer.AmbientLight);
            shader.SetVector3("viewPos", renderer.CameraPos);
            shader.SetMatrix4("cameraMatrix", renderer.CameraMatrix);
            shader.SetVector3("fillColor", mesh.Material.color);
            mesh.Draw();
        }
    }
}