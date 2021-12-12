
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class PBD_data
    {
        
        public class Particle
        {
            public Vector3 pos;
            public Vector3 vel;
            public Vector3 posEstimate;
            public float inverseMass;
            public int phase;
            public int constraintCount;
        }
        
        public abstract class Constraint
        {
            public abstract void Project(bool stabilize);
            
            public abstract float Evaluate();
        }
        
        // constraint types
        private static readonly int STABILIZE             = 0;
        private static readonly int CONTACT               = 1;
        private static readonly int STANDARD              = 2;
        private static readonly int SHAPE                 = 3;
        private static readonly int NUM_CONSTRAINT_TYPES  = 4;
        
        private List<Constraint>[] constraintGroups = new List<Constraint>[ NUM_CONSTRAINT_TYPES ];
        
        public static readonly float PARTICLE_RADIUS = 0.05f;
        
        public readonly Particle[] particles;
        public readonly int ParticleCount;
        public readonly float[] PosData;
        

        public PBD_data(int particleCount)
        {
            this.ParticleCount = particleCount;
            particles = new Particle[particleCount];
            PosData = new float[particleCount * 3];
            for (int i = 0; i < particleCount; i++)
            {
                particles[i] = new Particle();
                particles[i].pos = new Vector3();
                particles[i].vel = new Vector3();
                particles[i].posEstimate = new Vector3();
                particles[i].inverseMass = 1;
                particles[i].phase = 0;
                particles[i].constraintCount = 0;
            }
            
            for (int i = 0; i < constraintGroups.Length; i++)
                constraintGroups[i] = new List<Constraint>();
            
            UpdatePosData();
        }
        
        private void UpdatePosData()
        {
            for (int i = 0; i < ParticleCount; i++)
            {
                PosData[i * 3 + 0] = particles[i].pos.X;
                PosData[i * 3 + 1] = particles[i].pos.Y;
                PosData[i * 3 + 2] = particles[i].pos.Z;
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
            ClearConstraints();
            EstimatePositions(delta);
            FindCollisionConstraints();
            
            
            
            
            int STABILIZE_ITERATIONS = 2;
            // (16) For solver iterations
            for (int i = 0; i < STABILIZE_ITERATIONS; i++) {

                for (int k = 0; k < constraintGroups[CONTACT].Count; k++) {
                    constraintGroups[CONTACT][k].Project(true);
                }
            }
            
            
            int SOLVER_ITERATIONS = 3;
            // (16) For solver iterations
            for (int i = 0; i < SOLVER_ITERATIONS; i++) {

                // (17) For constraint group
                for (int j = 0; j < (int) NUM_CONSTRAINT_TYPES; j++) {
                    
                    if (j == STABILIZE) continue; // skip stabilisation

                    //  (18, 19, 20) Solve constraints in g and update ep
                    for (int k = 0; k < constraintGroups[j].Count; k++) {
                        constraintGroups[j][k].Project(false);
                    }
                }
            }
            
            
            
            
            
            
            // 9) end for
            
            // 2) damp velocities
            
            // 3) euler integrate position
            // foreach (Particle p in particles) p.posEstimate = p.pos + p.vel * delta;
            
            // 4) generate collision constraints
            
            // 5) iteratively project constraints
            
            // 6) set velocity to position difference, store new position
            foreach (Particle p in particles)
            {
                p.vel = (p.posEstimate - p.pos) / delta;
                p.pos = p.posEstimate;
                
                (p.pos, p.vel) = BoundCollision(1.5f, p.pos, p.vel);
                
            }
            
            
            UpdatePosData();
        }
        
        private void ClearConstraints()
        {
            for (int i = 0; i < constraintGroups.Length; i++)
                constraintGroups[i].Clear();
        }
        
        private void EstimatePositions(float delta)
        {
            foreach (Particle p in particles)
            {
                p.vel.Y += -1 * delta; // apply external force
                p.posEstimate = p.pos + p.vel * delta; // predict position
                p.constraintCount = 0; // clear constraint count
                
                // 4) apply mass scaling
                // needed for stacked rigid bodies, won't implement yet
            }
        }
        
        private void FindCollisionConstraints()
        {
            for (int i = 0; i < particles.Length; i++)
            {
                Particle p1 = particles[i];
                
                for (int j = i + 1; j < particles.Length; j++)
                {
                    Particle p2 = particles[j];
                    
                    // Skip collision between two immovables
                    if (p1.inverseMass == 0 && p2.inverseMass == 0) continue;
                    
                    float dist = (p2.posEstimate - p1.posEstimate).Length;
                    
                    if (dist < PARTICLE_RADIUS * 2)
                    {
                        constraintGroups[CONTACT].Add( new ContactConstraint(p1, p2) );
                    }
                    
                }
                
                // 8) find solid boundary contacts
                
            }
        }
    }
}