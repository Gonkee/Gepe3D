
using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Compute.OpenCL;

namespace Gepe3D
{
    public class HParticleSimulator
    {
        
        CLCommandQueue queue;
        CLKernel kernel;
        UIntPtr[] workDimensions;
        
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
        
        private int NUM_ITERATIONS = 2;
        
        private List<DistanceConstraint> distanceConstraints;
        public List<FluidConstraint> fluidConstraints;
        
        private readonly CLKernel
            kPredictPos,        // add external forces then predict positions with euler integration
            kGenNeighbours,     // add particles to bins corresponding to coordinates for easy proximity search
            kDistanceProject,   // project distance constraints (for soft bodies etc)
            kCalcDensities,     // FLUID - calculate density at each particle
            kCalcLambdas,       // FLUID - calculate lambda at each particle (scalar for position adjustment)
            kAddLambdas;        // FLUID - adjust position estimates using lambda values
        
        private readonly CLBuffer
            pos,        // positions
            vel,        // velocities
            epos,       // estimated positions
            imass;      // inverse masses
        
        public HParticleSimulator(int particleCount)
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
            
            ///////////////////
            // Set up OpenCL //
            ///////////////////
            
            // set up context & queue
            CLResultCode result;
            CLPlatform[] platforms;
            CLDevice[] devices;
            result = CL.GetPlatformIds(out platforms);
            result = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
            CLContext context = CL.CreateContext(new IntPtr(), 1, devices, new IntPtr(), new IntPtr(), out result);
            this.queue = CL.CreateCommandQueueWithProperties(context, devices[0], new IntPtr(), out result);
            this.workDimensions = new UIntPtr[] { new UIntPtr( (uint) particleCount) };
            
            // load kernels
            CLProgram program = BuildClProgram(context, devices, "res/Kernels/kernel.cl");
            this.kernel = CL.CreateKernel(program, "move_particles", out result);
            
            // create buffers
            UIntPtr bufferSize3 = new UIntPtr( (uint) particleCount * 3 * sizeof(float) );
            UIntPtr bufferSize1 = new UIntPtr( (uint) particleCount * 1 * sizeof(float) );
            this.pos   = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.vel   = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.epos  = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.imass = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize1, new IntPtr(), out result);
            
            // fill buffers with zeroes
            CLEvent @event = new CLEvent();
            float[] emptyFloat = new float[] {0};
            CL.EnqueueFillBuffer<float>(queue, pos  , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, vel  , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, epos , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, imass, emptyFloat, new UIntPtr(), bufferSize1, null, out @event);
            
            // ensure fills are completed
            CL.Flush(queue);
            CL.Finish(queue);
            
            
            
            
            
            
            UpdatePosData();
        }
        
        
        private CLProgram BuildClProgram(CLContext context, CLDevice[] devices, string path)
        {
            CLResultCode result;
            path = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), path);
            string kernelSource = File.ReadAllText(path);
            CLProgram program = CL.CreateProgramWithSource(context, kernelSource, out result);
            result = CL.BuildProgram(program, 1, devices, null, new IntPtr(), new IntPtr());
            
            if (result != CLResultCode.Success) {
                System.Console.WriteLine(result);
                byte[] logParam;
                CL.GetProgramBuildInfo(program, devices[0], ProgramBuildInfo.Log, out logParam);
                string error = System.Text.ASCIIEncoding.Default.GetString(logParam);
                System.Console.WriteLine(error);
            }
            return program;
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