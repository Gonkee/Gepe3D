
using System;
using OpenTK.Compute.OpenCL;

namespace Gepe3D
{
    public class HParticleSimulator
    {
        
        CLCommandQueue queue;
        UIntPtr[] workDimensions;
        
        CLEvent @event = new CLEvent();
        
        public readonly int ParticleCount;
        private readonly int cellCount;
        public readonly float[] PosData;
        
        
        public static float GRID_CELL_WIDTH = 0.6f; // used for the fluid effect radius
        
        public static float REST_DENSITY = 120f;
        
        public static int
            GridRowsX = 10,
            GridRowsY = 10,
            GridRowsZ = 10;
            
        public static float
            MAX_X = GRID_CELL_WIDTH * GridRowsX,
            MAX_Y = GRID_CELL_WIDTH * GridRowsY,
            MAX_Z = GRID_CELL_WIDTH * GridRowsZ;
        
        private int NUM_ITERATIONS = 2;
        
        
        private readonly CLKernel
            kPredictPos,        // add external forces then predict positions with euler integration
            kGenNeighbours,     // add particles to bins corresponding to coordinates for easy proximity search
            kDistanceProject,   // project distance constraints (for soft bodies etc)
            kCalcLambdas,       // FLUID - calculate lambda at each particle (scalar for position adjustment)
            kAddLambdas,        // FLUID - adjust position estimates using lambda values
            kCorrectFluid,
            kUpdateVel,         // update final position and velocity with bounds collision
            kCalcVorticity,
            kApplyVortVisc,
            kCorrectVel,
            kAssignParticlCells,
            kFindCellsStartAndEnd,
            kSortParticleIDsByCell;
        
        private readonly CLBuffer
            pos,        // positions
            vel,        // velocities
            epos,       // estimated positions
            imass,      // inverse masses
            lambdas,    // scalar for position adjustment
            corrections,
            vorticities,
            velCorrect,
            sortedParticleIDs,
            cellStartAndEndIDs,
            cellIDsOfParticles,
            numParticlesPerCell,
            particleIDinCell,
            debugOut;
            
        
        public HParticleSimulator(int particleCount)
        {
            sortedBufferSize = new UIntPtr( (uint) particleCount * sizeof(int) );
            sortedBufferSizeSmall = new UIntPtr( (uint) (GridRowsX * GridRowsY * GridRowsZ) * sizeof(int) );
            
            this.ParticleCount = particleCount;
            this.cellCount = GridRowsX * GridRowsY * GridRowsZ;
            PosData = new float[particleCount * 3];
            
            
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
            string varDefines            = GenerateDefines(); // combine with other source strings to add common functions
            string commonFuncSource      = CLUtils.LoadSource("res/Kernels/common_funcs.cl");
            string pbdCommonSource       = CLUtils.LoadSource("res/Kernels/pbd_common.cl");
            string fluidProjectSource    = CLUtils.LoadSource("res/Kernels/fluid_project.cl");
            CLProgram pbdProgram         = CLUtils.BuildClProgram(context, devices, varDefines + commonFuncSource + pbdCommonSource   );
            CLProgram fluidProgram       = CLUtils.BuildClProgram(context, devices, varDefines + commonFuncSource + fluidProjectSource);
            this.kPredictPos             = CL.CreateKernel( pbdProgram   , "predict_positions"          , out result);
            this.kUpdateVel              = CL.CreateKernel( pbdProgram   , "update_velocity"            , out result);
            this.kCalcLambdas            = CL.CreateKernel( fluidProgram , "calculate_lambdas"          , out result);
            this.kAddLambdas             = CL.CreateKernel( fluidProgram , "add_lambdas"                , out result);
            this.kCorrectFluid           = CL.CreateKernel( fluidProgram , "correct_fluid_positions"    , out result);
            this.kCalcVorticity          = CL.CreateKernel( fluidProgram , "calculate_vorticities"      , out result);
            this.kApplyVortVisc          = CL.CreateKernel( fluidProgram , "apply_vorticity_viscosity"  , out result);
            this.kCorrectVel             = CL.CreateKernel( fluidProgram , "correct_fluid_vel"          , out result);
            this.kAssignParticlCells     = CL.CreateKernel( pbdProgram   , "assign_particle_cells"      , out result);
            this.kFindCellsStartAndEnd   = CL.CreateKernel( pbdProgram   , "find_cells_start_and_end"   , out result);
            this.kSortParticleIDsByCell  = CL.CreateKernel( pbdProgram   , "sort_particle_ids_by_cell"  , out result);
            
            // create buffers
            this.pos         = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.vel         = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.epos        = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.imass       = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount     , 1);
            this.lambdas     = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount     , 0);
            this.corrections = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.vorticities = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.velCorrect  = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount * 3 , 0);
            this.debugOut    = CLUtils.EnqueueMakeFloatBuffer(context, queue, particleCount     , 0);
            
            this.sortedParticleIDs   = CLUtils.EnqueueMakeIntBuffer(context, queue, particleCount , 0);
            this.cellIDsOfParticles  = CLUtils.EnqueueMakeIntBuffer(context, queue, particleCount , 0);
            this.particleIDinCell    = CLUtils.EnqueueMakeIntBuffer(context, queue, particleCount , 0);
            this.cellStartAndEndIDs  = CLUtils.EnqueueMakeIntBuffer(context, queue, cellCount * 2 , 0);
            this.numParticlesPerCell = CLUtils.EnqueueMakeIntBuffer(context, queue, cellCount     , 0);
            
            Random rand = new Random();
            float[] rands = new float[particleCount * 3];
            for (int i = 0; i < rands.Length; i++)
                rands[i] = (float) rand.NextDouble() * 2.5f;
            
            CL.EnqueueWriteBuffer<float>(queue, pos, false, new UIntPtr(), rands, null, out @event);
            
            // ensure fills are completed
            CL.Flush(queue);
            CL.Finish(queue);
            
        }
        
        
        // TODO: make this more robust
        private string GenerateDefines()
        {
            string defines = "";
            
            defines += "\n" + "#define CELLCOUNT_X " + GridRowsX;
            defines += "\n" + "#define CELLCOUNT_Y " + GridRowsY;
            defines += "\n" + "#define CELLCOUNT_Z " + GridRowsZ;
            defines += "\n" + "#define CELL_WIDTH " + GRID_CELL_WIDTH.ToString("0.0000") + "f";
            defines += "\n" + "#define MAX_X " + MAX_X.ToString("0.0000") + "f";
            defines += "\n" + "#define MAX_Y " + MAX_Y.ToString("0.0000") + "f";
            defines += "\n" + "#define MAX_Z " + MAX_Z.ToString("0.0000") + "f";
            defines += "\n" + "#define KERNEL_SIZE " + GRID_CELL_WIDTH.ToString("0.0000") + "f";
            defines += "\n" + "#define REST_DENSITY " + REST_DENSITY.ToString("0.0000") + "f";
            defines += "\n";
            
            return defines;
        }
        
        float[] emptyFloat = new float[] {0};
        static int debugSize = 27;
        UIntPtr debugBufferSize = new UIntPtr( (uint) debugSize * sizeof(float) );
            int[] emptyInt = new int[] {0};
            
        UIntPtr sortedBufferSize;
        UIntPtr sortedBufferSizeSmall;
        
        public void Update(float delta)
        {
            CL.EnqueueFillBuffer<int>(queue, sortedParticleIDs  , emptyInt, new UIntPtr(), sortedBufferSize, null, out @event);
            CL.EnqueueFillBuffer<int>(queue, numParticlesPerCell  , emptyInt, new UIntPtr(), sortedBufferSizeSmall, null, out @event);
            
            CLUtils.EnqueueKernel(queue,  kPredictPos     , ParticleCount, delta, pos, vel, epos);
            
            CLUtils.EnqueueKernel(queue, kAssignParticlCells, ParticleCount, epos, numParticlesPerCell,
                cellIDsOfParticles, particleIDinCell, debugOut);
            CLUtils.EnqueueKernel(queue, kFindCellsStartAndEnd, cellCount, numParticlesPerCell, cellStartAndEndIDs);
            CLUtils.EnqueueKernel(queue, kSortParticleIDsByCell, ParticleCount, particleIDinCell,
                cellStartAndEndIDs, cellIDsOfParticles, sortedParticleIDs, debugOut);
            
            CLUtils.EnqueueKernel(queue,  kCalcLambdas    , ParticleCount, epos, imass, lambdas, cellIDsOfParticles, cellStartAndEndIDs, sortedParticleIDs, debugOut);
            CLUtils.EnqueueKernel(queue,  kAddLambdas     , ParticleCount, epos, imass, lambdas, corrections, cellIDsOfParticles, cellStartAndEndIDs, sortedParticleIDs);
            CLUtils.EnqueueKernel(queue,  kCorrectFluid   , ParticleCount, epos, corrections);
            CLUtils.EnqueueKernel(queue,  kUpdateVel      , ParticleCount, delta, pos, vel, epos);
            CLUtils.EnqueueKernel(queue,  kCalcVorticity  , ParticleCount, pos, vel, vorticities, cellIDsOfParticles, cellStartAndEndIDs, sortedParticleIDs);
            CLUtils.EnqueueKernel(queue,  kApplyVortVisc  , ParticleCount, pos, vel, vorticities, velCorrect, imass, delta, cellIDsOfParticles, cellStartAndEndIDs, sortedParticleIDs);
            CLUtils.EnqueueKernel(queue,  kCorrectVel     , ParticleCount, vel, velCorrect);
            
            CL.EnqueueReadBuffer<float>(queue, pos, false, new UIntPtr(), PosData, null, out @event);
            
            CL.Flush(queue);
            CL.Finish(queue);
            
        }
        
        
    }
}