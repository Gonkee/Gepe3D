
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


        public ParticleSimulator(int particleCount)
        {
            this.ParticleCount = particleCount;
            particlePool = new Particle[particleCount];
            PosData = new float[particleCount * 3];
            
            for (int i = 0; i < particleCount; i++)
                particlePool[i] = new Particle(i);
            
            distanceConstraints = new List<(Particle, Particle, float)>();
            
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
            
            // 2 iterations constraint projection
            for (int i = 0; i < 2; i++)
            {
                foreach( (Particle, Particle, float) tp in distanceConstraints )
                {
                    Particle p1 = tp.Item1;
                    Particle p2 = tp.Item2;
                    
                    if ( (!p1.active) || (!p2.active) ) continue;
                    
                    float idealDistance = tp.Item3;
                    
                    Vector3 posDiff = p1.posEstimate - p2.posEstimate;
                    float displacement = posDiff.Length - idealDistance;
                    Vector3 direction = posDiff.Normalized();
                    
                    float w1 = p1.inverseMass / (p1.inverseMass + p2.inverseMass);
                    float w2 = p2.inverseMass / (p1.inverseMass + p2.inverseMass);
                    
                    Vector3 correction1 = -w1 * displacement * direction;
                    Vector3 correction2 = +w2 * displacement * direction;
                    
                    p1.posEstimate += correction1;
                    p2.posEstimate += correction2;
                    
                    
                }
            }
            
            
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
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