
using System;
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
        }
        
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
            }
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
            // 1) euler integrate velocity
            foreach (Particle p in particles) p.vel.Y += -1 * delta;
            
            // 2) damp velocities
            
            // 3) euler integrate position
            foreach (Particle p in particles) p.posEstimate = p.pos + p.vel * delta;
            
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
        
        // public float[] GetPositionBuffer()
        // {
        //     return _positions;
        // }

        // public Vector3 GetPos(int id)
        // {
        //     return new Vector3(
        //         _positions[id * 3 + 0],
        //         _positions[id * 3 + 1],
        //         _positions[id * 3 + 2]
        //     );
        // }

        // public void SetPos(int id, float x, float y, float z)
        // {
        //     _positions[id * 3 + 0] = x;
        //     _positions[id * 3 + 1] = y;
        //     _positions[id * 3 + 2] = z;
        // }
        
        // public void SetPos(int id, Vector3 pos)
        // {
        //     _positions[id * 3 + 0] = pos.X;
        //     _positions[id * 3 + 1] = pos.Y;
        //     _positions[id * 3 + 2] = pos.Z;
        // }

        // public void AddPos(int id, float x, float y, float z)
        // {
        //     _positions[id * 3 + 0] += x;
        //     _positions[id * 3 + 1] += y;
        //     _positions[id * 3 + 2] += z;
        // }
        
        // public void AddPos(int id, Vector3 pos)
        // {
        //     _positions[id * 3 + 0] += pos.X;
        //     _positions[id * 3 + 1] += pos.Y;
        //     _positions[id * 3 + 2] += pos.Z;
        // }
        
        // public Vector3 GetVel(int id)
        // {
        //     return new Vector3(
        //         _velocities[id * 3 + 0],
        //         _velocities[id * 3 + 1],
        //         _velocities[id * 3 + 2]
        //     );
        // }

        // public void SetVel(int id, float x, float y, float z)
        // {
        //     _velocities[id * 3 + 0] = x;
        //     _velocities[id * 3 + 1] = y;
        //     _velocities[id * 3 + 2] = z;
        // }
        
        // public void SetVel(int id, Vector3 vel)
        // {
        //     _velocities[id * 3 + 0] = vel.X;
        //     _velocities[id * 3 + 1] = vel.Y;
        //     _velocities[id * 3 + 2] = vel.Z;
        // }

        // public void AddVel(int id, float x, float y, float z)
        // {
        //     _velocities[id * 3 + 0] += x;
        //     _velocities[id * 3 + 1] += y;
        //     _velocities[id * 3 + 2] += z;
        // }
        
        // public void AddVel(int id, Vector3 vel)
        // {
        //     _velocities[id * 3 + 0] += vel.X;
        //     _velocities[id * 3 + 1] += vel.Y;
        //     _velocities[id * 3 + 2] += vel.Z;
        // }
    }
}