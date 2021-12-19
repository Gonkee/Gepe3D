
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
        
        private int NUM_ITERATIONS = 2;
        
        // private List<(Particle, Particle, float)> distanceConstraints;
        private List<DistanceConstraint> distanceConstraints;
        
        public List<FluidConstraint> fluidConstraints;
        


        public ParticleSimulator(int particleCount)
        {
            this.ParticleCount = particleCount;
            particlePool = new Particle[particleCount];
            PosData = new float[particleCount * 3];
            
            for (int i = 0; i < particleCount; i++)
                particlePool[i] = new Particle(i);
            
            distanceConstraints = new List<DistanceConstraint>();
            fluidConstraints = new List<FluidConstraint>();
            
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
            distanceConstraints.Add ( new DistanceConstraint(p1, p2, distance, 0.1f, NUM_ITERATIONS) );
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
            for (int i = 0; i < NUM_ITERATIONS; i++)
            {
                
                foreach (DistanceConstraint constraint in distanceConstraints) constraint.Project();
                
                // currently also passes inactive particles, must fix
                foreach (FluidConstraint constraint in fluidConstraints) constraint.Project(particlePool);
                
                
                // fluid stuff
                
                
                
                
                
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