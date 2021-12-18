
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class ParticleSimulator
    {
        
        
        public readonly int ParticleCount;
        public readonly float[] PosData;
        public readonly Particle[] particlePool;
        private int poolIndex = 0;
        
        
        private List<(Particle, Particle, float)> distanceConstraints;
        
        // two particles forming the connecting edge, then the two on both sides
        private List<(Particle, Particle, Particle, Particle)> bendConstraints;


        public ParticleSimulator(int particleCount)
        {
            this.ParticleCount = particleCount;
            particlePool = new Particle[particleCount];
            PosData = new float[particleCount * 3];
            
            for (int i = 0; i < particleCount; i++)
                particlePool[i] = new Particle(i);
            
            distanceConstraints = new List<(Particle, Particle, float)>();
            bendConstraints = new List<(Particle, Particle, Particle, Particle)>();
            
            UpdatePosData();
        }
        
        public Particle AddParticle(float x, float y, float z)
        {
            if (poolIndex >= particlePool.Length){
                System.Console.WriteLine("Max particle count reached!");
                return null;
            }
            particlePool[poolIndex].pos = new Vector3(x, y, z);
            particlePool[poolIndex].active = true;
            return particlePool[poolIndex++];
        }
        
        public void AddDistanceConstraint(Particle p1, Particle p2, float distance)
        {
            distanceConstraints.Add ( (p1, p2, distance) );
        }
        
        public void AddBendConstraint(Particle p1, Particle p2, Particle p3, Particle p4)
        {
            bendConstraints.Add ( (p1, p2, p3, p4) );
        }
        
        private void UpdatePosData()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                PosData[i * 3 + 0] = particlePool[i].pos.X;
                PosData[i * 3 + 1] = particlePool[i].pos.Y;
                PosData[i * 3 + 2] = particlePool[i].pos.Z;
            }
        }
        
        private (Vector3, Vector3) BoundCollision(float BOUNDING_RADIUS, Vector3 pos, Vector3 vel)
        {
            if (pos.X < -BOUNDING_RADIUS) {
                pos.X = -BOUNDING_RADIUS;
                vel.X = MathF.Max(0, vel.X); }
            if (pos.X > BOUNDING_RADIUS) {
                pos.X = BOUNDING_RADIUS;
                vel.X = MathF.Min(0, vel.X); }
            if (pos.Y < -BOUNDING_RADIUS) {
                pos.Y = -BOUNDING_RADIUS;
                vel.Y = MathF.Max(0, vel.Y); }
            if (pos.Y > BOUNDING_RADIUS) {
                pos.Y = BOUNDING_RADIUS;
                vel.Y = MathF.Min(0, vel.Y); }
            if (pos.Z < -BOUNDING_RADIUS) {
                pos.Z = -BOUNDING_RADIUS;
                vel.Z = MathF.Max(0, vel.Z); }
            if (pos.Z > BOUNDING_RADIUS) {
                pos.Z = BOUNDING_RADIUS;
                vel.Z = MathF.Min(0, vel.Z); }
            return (pos, vel);
        }
        
        public void Update(float delta)
        {
            
            
            // distanceConstraints.Clear();
            
            
            // ClearConstraints();
            // EstimatePositions(delta);
            // FindCollisionConstraints();
            
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
                p.vel.Y += -1 * delta; // apply external force
                p.posEstimate = p.pos + p.vel * delta; // predict position
            }
            
            int NUM_ITERATIONS = 2;
            
            float distanceStiffness = 0.1f;
            float distanceStiffnessFac = 1 - MathF.Pow( 1 - distanceStiffness, 1f / (float) NUM_ITERATIONS );
            
            float bendStiffness = 1f;
            float bendStiffnessFac = 1 - MathF.Pow( 1 - bendStiffness, 1f / (float) NUM_ITERATIONS );
            
            // 2 iterations constraint projection
            for (int i = 0; i < NUM_ITERATIONS; i++)
            {
                foreach( (Particle, Particle, float) constraint in distanceConstraints )
                {
                    Particle p1 = constraint.Item1;
                    Particle p2 = constraint.Item2;
                    
                    if ( (!p1.active) || (!p2.active) ) continue;
                    if ( p1.inverseMass == 0 && p2.inverseMass == 0 ) continue;
                    
                    float idealDistance = constraint.Item3;
                    
                    Vector3 posDiff = p1.posEstimate - p2.posEstimate;
                    float displacement = posDiff.Length - idealDistance;
                    Vector3 direction = posDiff.Normalized();
                    
                    float w1 = p1.inverseMass / (p1.inverseMass + p2.inverseMass);
                    float w2 = p2.inverseMass / (p1.inverseMass + p2.inverseMass);
                    
                    Vector3 correction1 = -w1 * displacement * direction;
                    Vector3 correction2 = +w2 * displacement * direction;
                    
                    p1.posEstimate += correction1 * distanceStiffnessFac;
                    p2.posEstimate += correction2 * distanceStiffnessFac;
                    
                }
                
                
                
                foreach ( (Particle, Particle, Particle, Particle) constraint in bendConstraints )
                {
                    // p1 is considered 0 and ignored, the other positions are relative to p1
                    Vector3 p2 = constraint.Item2.posEstimate - constraint.Item1.posEstimate;
                    Vector3 p3 = constraint.Item3.posEstimate - constraint.Item1.posEstimate;
                    Vector3 p4 = constraint.Item4.posEstimate - constraint.Item1.posEstimate;
                    
                    float w1 = constraint.Item1.inverseMass;
                    float w2 = constraint.Item2.inverseMass;
                    float w3 = constraint.Item3.inverseMass;
                    float w4 = constraint.Item4.inverseMass;
                    
                    Vector3 cross1 = Vector3.Cross( p2, p3 );
                    Vector3 cross2 = Vector3.Cross( p2, p4 );
                    float crossLen1 = cross1.Length;
                    float crossLen2 = cross2.Length;
                    Vector3 normal1 = cross1.Normalized();
                    Vector3 normal2 = cross2.Normalized();
                    
                    float d = Vector3.Dot( normal1, normal2 );
                    
                    Vector3 q3 = ( Vector3.Cross(p2, normal2) + Vector3.Cross(normal1, p2) * d ) / crossLen1;
                    Vector3 q4 = ( Vector3.Cross(p2, normal1) + Vector3.Cross(normal2, p2) * d ) / crossLen2;
                    Vector3 q2 = 
                        - ( Vector3.Cross(p3, normal2) + Vector3.Cross(normal1, p3) * d ) / crossLen1
                        - ( Vector3.Cross(p4, normal1) + Vector3.Cross(normal2, p4) * d ) / crossLen2;
                    Vector3 q1 = - q2 - q3 - q4;
                    
                    float denominatorSum =
                        w1 * Vector3.Dot(q1, q1) +
                        w2 * Vector3.Dot(q2, q2) +
                        w3 * Vector3.Dot(q3, q3) +
                        w4 * Vector3.Dot(q4, q4);
                    
                    float defaultAngle = MathF.PI;
                    float term = MathF.Sqrt(1 - d * d) * (MathF.Acos(d) - defaultAngle) / denominatorSum;
                    
                    Vector3 correction1 = - w1 * q1 * term;
                    Vector3 correction2 = - w2 * q2 * term;
                    Vector3 correction3 = - w3 * q3 * term;
                    Vector3 correction4 = - w4 * q4 * term;
                    
                    constraint.Item1.posEstimate += correction1 * bendStiffness;
                    constraint.Item2.posEstimate += correction2 * bendStiffness;
                    constraint.Item3.posEstimate += correction3 * bendStiffness;
                    constraint.Item4.posEstimate += correction4 * bendStiffness;
                    
                }
                
                
                
                
            }
            
            
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
                if (p.immovable) p.posEstimate = p.pos;
                
                p.vel = (p.posEstimate - p.pos) / delta;
                p.pos = p.posEstimate;
                
                (p.pos, p.vel) = BoundCollision(1.5f, p.pos, p.vel);
                
            }
            
            
            UpdatePosData();
        }
        
        
        private void EstimatePositions(float delta)
        {
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
                p.vel.Y += -1 * delta; // apply external force
                p.posEstimate = p.pos + p.vel * delta; // predict position
                // p.constraintCount = 0; // clear constraint count
                
                // 4) apply mass scaling
                // needed for stacked rigid bodies, won't implement yet
            }
        }
    }
}