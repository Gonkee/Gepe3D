
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
        
        public List<int>[][][] grid;
        
        public static float GRID_CELL_WIDTH = 0.6f; // used for the fluid effect radius
        
        public static int
            GridRowsX = 8,
            GridRowsY = 8,
            GridRowsZ = 8;
            
        public static float
            MAX_X = GRID_CELL_WIDTH * GridRowsX,
            MAX_Y = GRID_CELL_WIDTH * GridRowsY,
            MAX_Z = GRID_CELL_WIDTH * GridRowsZ;
        
        private int NUM_ITERATIONS = 1;
        
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
            
            grid = new List<int>[GridRowsX][][];
            for (int i = 0; i < GridRowsX; i++) {
                grid[i] = new List<int>[GridRowsY][];
                for (int j = 0; j < GridRowsY; j++) {
                    grid[i][j] = new List<int>[GridRowsZ];
                    for(int k = 0; k < GridRowsZ; k++) {
                        grid[i][j][k] = new List<int>();
                    }
                }
                
            }
            
            UpdatePosData();
        }
        
        public int AddParticle(float x, float y, float z)
        {
            if (poolIndex >= particlePool.Length){
                System.Console.WriteLine("Max particle count reached!");
                return -1;
            }
            particlePool[poolIndex].pos = new Vector3(x, y, z);
            particlePool[poolIndex].active = true;
            return poolIndex++;
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
        
        private (Vector3, Vector3) BoundCollision(Vector3 pos, Vector3 vel)
        {
            if      (pos.X <     0) {  pos.X =     0;  vel.X = MathF.Max(0, vel.X);  }
            else if (pos.X > MAX_X) {  pos.X = MAX_X;  vel.X = MathF.Min(0, vel.X);  }
            
            if      (pos.Y <     0) {  pos.Y =     0;  vel.Y = MathF.Max(0, vel.Y);  }
            else if (pos.Y > MAX_Y) {  pos.Y = MAX_Y;  vel.Y = MathF.Min(0, vel.Y);  }
            
            if      (pos.Z <     0) {  pos.Z =     0;  vel.Z = MathF.Max(0, vel.Z);  }
            else if (pos.Z > MAX_Z) {  pos.Z = MAX_Z;  vel.Z = MathF.Min(0, vel.Z);  }
            
            return (pos, vel);
        }
        
        
        
        private void GenerateNeighbours()
        {
            foreach (List<int>[][] i in grid) {
                foreach (List<int>[] j in i) {
                    foreach (List<int> k in j) {
                        k.Clear();
                    }
                }
            }
            
            for (int i = 0; i < particlePool.Length; i++)
            {
                Particle p = particlePool[i];
                
                int x = Math.Clamp( (int) (p.posEstimate.X / GRID_CELL_WIDTH), 0, GridRowsX - 1);
                int y = Math.Clamp( (int) (p.posEstimate.Y / GRID_CELL_WIDTH), 0, GridRowsY - 1);
                int z = Math.Clamp( (int) (p.posEstimate.Z / GRID_CELL_WIDTH), 0, GridRowsZ - 1);
                
                grid[x][y][z].Add(i);
                
                p.gridX = x;
                p.gridY = y;
                p.gridZ = z;
                
                // long x = (long) (p.posEstimate.X / GRID_CELL_WIDTH);
                // long y = (long) (p.posEstimate.Y / GRID_CELL_WIDTH);
                // long z = (long) (p.posEstimate.Z / GRID_CELL_WIDTH);
                
                // long cellID = (x << 32) + (y << 16) + (z);
                
                // generate id for the particle's position
                // store that id in the particle, and store the particle's id in a sorted array according to position ids
                // grid width >= 2 * sample radius, each sample kernal can only cover 8 cells instead of 27
            }
        }
        
        
        public void Update(float delta)
        {
            GenerateNeighbours();
            
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
                p.vel.Y += -1 * delta; // apply external force
                p.posEstimate = p.pos + p.vel * delta; // predict position
            }
            
            
            for (int i = 0; i < NUM_ITERATIONS; i++)
            {
                
                foreach (DistanceConstraint constraint in distanceConstraints) constraint.Project();
                
                // currently also passes inactive particles, must fix
                foreach (FluidConstraint constraint in fluidConstraints) constraint.Project(particlePool, grid);
                
            }
            
            
            foreach (Particle p in particlePool)
            {
                if (!p.active) continue;
                
                if (p.immovable) p.posEstimate = p.pos;
                
                p.vel = (p.posEstimate - p.pos) / delta;
                p.pos = p.posEstimate;
                
                (p.pos, p.vel) = BoundCollision(p.pos, p.vel);
                
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