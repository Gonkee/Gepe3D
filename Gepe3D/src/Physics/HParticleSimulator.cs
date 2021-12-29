
using System;
using System.IO;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Compute.OpenCL;
using System.Linq;

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
        
        public static float REST_DENSITY = 80f;
        
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
            kCalcLambdas,     // FLUID - calculate lambda at each particle (scalar for position adjustment)
            kAddLambdas,        // FLUID - adjust position estimates using lambda values
            kCorrectFluid,
            kUpdateVel,         // update final position and velocity with bounds collision
            kCalcVorticity,
            kApplyVortVisc,
            kCorrectVel,
            kResetCellParticleCount,
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
            
        private float[] debugArray;
        private int[] sortedIDsDebug;
        private int[] idInCellDebug;
        private int[] startAndEndDebug = new int[8 * 8 * 8 * 2];
        
        public HParticleSimulator(int particleCount)
        {
            sortedIDsDebug = new int[particleCount];
            idInCellDebug = new int[particleCount];
            
            
            this.ParticleCount = particleCount;
            this.cellCount = GridRowsX * GridRowsY * GridRowsZ;
            PosData = new float[particleCount * 3];
            
            
            distanceConstraints = new List<DistanceConstraint>();
            fluidConstraints = new List<FluidConstraint>();
            
            
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
            
            string commonFuncSource   = LoadSource("res/Kernels/common_funcs.cl"); // combine with other source strings to add common functions
            string pbdCommonSource    = LoadSource("res/Kernels/pbd_common.cl");
            string fluidProjectSource = LoadSource("res/Kernels/fluid_project.cl");
            
            string varDefines = GenerateDefines();
            
            CLProgram pbdProgram   = BuildClProgram(context, devices, varDefines + commonFuncSource + pbdCommonSource);
            CLProgram fluidProgram = BuildClProgram(context, devices, varDefines + commonFuncSource + fluidProjectSource);
            
            this.kPredictPos = CL.CreateKernel(pbdProgram, "predict_positions", out result);
            this.kUpdateVel = CL.CreateKernel(pbdProgram, "update_velocity", out result);
            this.kCalcLambdas = CL.CreateKernel(fluidProgram, "calculate_lambdas", out result);
            this.kAddLambdas = CL.CreateKernel(fluidProgram, "add_lambdas", out result);
            this.kCorrectFluid = CL.CreateKernel(fluidProgram, "correct_fluid_positions", out result);
            this.kCalcVorticity = CL.CreateKernel(fluidProgram, "calculate_vorticities", out result);
            this.kApplyVortVisc = CL.CreateKernel(fluidProgram, "apply_vorticity_viscosity", out result);
            this.kCorrectVel = CL.CreateKernel(fluidProgram, "correct_fluid_vel", out result);
            
            
            this.kResetCellParticleCount = CL.CreateKernel(pbdProgram, "reset_cell_particle_count", out result);
            this.kAssignParticlCells  = CL.CreateKernel(pbdProgram, "assign_particle_cells", out result);
            this.kFindCellsStartAndEnd = CL.CreateKernel(pbdProgram, "find_cells_start_and_end", out result);
            this.kSortParticleIDsByCell = CL.CreateKernel(pbdProgram, "sort_particle_ids_by_cell", out result);
            
            // create buffers
            UIntPtr bufferSize3 = new UIntPtr( (uint) particleCount * 3 * sizeof(float) );
            UIntPtr bufferSize1 = new UIntPtr( (uint) particleCount * 1 * sizeof(float) );
            this.pos       = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.vel       = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.epos      = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.imass     = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize1, new IntPtr(), out result);
            this.lambdas   = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize1, new IntPtr(), out result);
            this.corrections= CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.vorticities= CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            this.velCorrect= CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize3, new IntPtr(), out result);
            
            UIntPtr intbufferSizeParticles = new UIntPtr( (uint) particleCount * sizeof(int) );
            UIntPtr intbufferSizeCells     = new UIntPtr( (uint) (GridRowsX * GridRowsY * GridRowsZ) * sizeof(int));
            UIntPtr intbufferSizeCells2     = new UIntPtr( (uint) (GridRowsX * GridRowsY * GridRowsZ) * sizeof(int) * 2 );
            this.sortedParticleIDs = CL.CreateBuffer(context, MemoryFlags.ReadWrite, intbufferSizeParticles, new IntPtr(), out result);
            this.cellStartAndEndIDs = CL.CreateBuffer(context, MemoryFlags.ReadWrite, intbufferSizeCells2, new IntPtr(), out result);
            this.cellIDsOfParticles = CL.CreateBuffer(context, MemoryFlags.ReadWrite, intbufferSizeParticles, new IntPtr(), out result);
            this.numParticlesPerCell = CL.CreateBuffer(context, MemoryFlags.ReadWrite, intbufferSizeCells, new IntPtr(), out result);
            this.particleIDinCell = CL.CreateBuffer(context, MemoryFlags.ReadWrite, intbufferSizeParticles, new IntPtr(), out result);
            
            
            
            
            Random rand = new Random();
            float[] rands = new float[particleCount * 3];
            for (int i = 0; i < rands.Length; i++)
                rands[i] = (float) rand.NextDouble() * 2.5f;
            
            // fill buffers with zeroes
            float[] emptyFloat = new float[] {0};
            CL.EnqueueWriteBuffer<float>(queue, pos, false, new UIntPtr(), rands, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, vel      , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, epos     , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, imass    , new float[] {1}, new UIntPtr(), bufferSize1, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, lambdas  , emptyFloat, new UIntPtr(), bufferSize1, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, corrections  , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, vorticities  , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            CL.EnqueueFillBuffer<float>(queue, velCorrect  , emptyFloat, new UIntPtr(), bufferSize3, null, out @event);
            
            int[] emptyInt = new int[] {0};
            CL.EnqueueFillBuffer<int>(queue, sortedParticleIDs   , emptyInt, new UIntPtr(), intbufferSizeParticles, null, out @event);
            CL.EnqueueFillBuffer<int>(queue, cellStartAndEndIDs  , emptyInt, new UIntPtr(), intbufferSizeCells2, null, out @event);
            CL.EnqueueFillBuffer<int>(queue, cellIDsOfParticles  , emptyInt, new UIntPtr(), intbufferSizeParticles, null, out @event);
            CL.EnqueueFillBuffer<int>(queue, numParticlesPerCell  , emptyInt, new UIntPtr(), intbufferSizeCells, null, out @event);
            CL.EnqueueFillBuffer<int>(queue, particleIDinCell  , emptyInt, new UIntPtr(), intbufferSizeParticles, null, out @event);
            
            
            int debugSize = 27;
            UIntPtr debugBufferSize = new UIntPtr( (uint) debugSize * sizeof(float) );
            this.debugOut = CL.CreateBuffer(context, MemoryFlags.ReadWrite, debugBufferSize, new IntPtr(), out result);
            CL.EnqueueFillBuffer<float>(queue, debugOut  , emptyFloat, new UIntPtr(), debugBufferSize, null, out @event);
            this.debugArray = new float[debugSize];
            
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
        
        public void Update(float delta)
        {
            
            
            CL.EnqueueFillBuffer<float>(queue, debugOut  , emptyFloat, new UIntPtr(), debugBufferSize, null, out @event);
            
            EnqueueKernel(queue,  kPredictPos     , ParticleCount, delta, pos, vel, epos);
            
            EnqueueKernel(queue, kResetCellParticleCount, cellCount, numParticlesPerCell);
            EnqueueKernel(queue, kAssignParticlCells, ParticleCount, epos, numParticlesPerCell,
                cellIDsOfParticles, particleIDinCell, debugOut);
            EnqueueKernel(queue, kFindCellsStartAndEnd, cellCount, numParticlesPerCell, cellStartAndEndIDs);
            EnqueueKernel(queue, kSortParticleIDsByCell, ParticleCount, particleIDinCell,
                cellStartAndEndIDs, cellIDsOfParticles, sortedParticleIDs, debugOut);
            
            EnqueueKernel(queue,  kCalcLambdas    , ParticleCount, epos, imass, lambdas, cellIDsOfParticles, cellStartAndEndIDs, sortedParticleIDs, debugOut);
            EnqueueKernel(queue,  kAddLambdas     , ParticleCount, epos, imass, lambdas, corrections);
            EnqueueKernel(queue,  kCorrectFluid   , ParticleCount, epos, corrections);
            EnqueueKernel(queue,  kUpdateVel      , ParticleCount, delta, pos, vel, epos);
            EnqueueKernel(queue,  kCalcVorticity  , ParticleCount, pos, vel, vorticities);
            EnqueueKernel(queue,  kApplyVortVisc  , ParticleCount, pos, vel, vorticities, velCorrect, imass, delta);
            EnqueueKernel(queue,  kCorrectVel     , ParticleCount, vel, velCorrect);
            
            CL.EnqueueReadBuffer<float>(queue, pos, false, new UIntPtr(), PosData, null, out @event);
            
            CL.EnqueueReadBuffer<float>(queue, debugOut, false, new UIntPtr(), debugArray, null, out @event);
            CL.EnqueueReadBuffer<int>(queue, sortedParticleIDs, false, new UIntPtr(), sortedIDsDebug, null, out @event);
            CL.EnqueueReadBuffer<int>(queue, cellStartAndEndIDs, false, new UIntPtr(), startAndEndDebug, null, out @event);
            CL.EnqueueReadBuffer<int>(queue, particleIDinCell, false, new UIntPtr(), idInCellDebug, null, out @event);
            
            CL.Flush(queue);
            CL.Finish(queue);
            
            yee++;
            // if (yee % 30 == 0) System.Console.WriteLine(string.Join(", ", idInCellDebug));
            // if (yee % 30 == 0) System.Console.WriteLine( idInCellDebug.Count( n => n == 0 ) );
            if (yee % 30 == 0) System.Console.WriteLine(CheckValidSort(sortedIDsDebug));
            // if (yee % 30 == 0) System.Console.WriteLine(string.Join(", ", startAndEndDebug));
            
        }
        
        int yee = 0;
        
        
        //////////////////////
        // Helper Functions //
        //////////////////////
        
        private static int CheckValidSort(int[] sortedIDs) {
            
            return sortedIDs.Length - sortedIDs.Distinct().Count();
        }
        
        
        private static string LoadSource(string filePath)
        {
            filePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), filePath);
            return File.ReadAllText(filePath);
        }
        
        private static CLProgram BuildClProgram(CLContext context, CLDevice[] devices, string source)
        {
            CLResultCode result;
            CLProgram program = CL.CreateProgramWithSource(context, source, out result);
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
        
        private static void EnqueueKernel(CLCommandQueue queue, CLKernel kernel, int numWorkUnits, params object[] args)
        {
            CLResultCode result = CLResultCode.Success;
            uint argID = 0;
            foreach (object arg in args)
            {
                Type argType = arg.GetType();
                if (argType == typeof(float)) {
                    result = CL.SetKernelArg<float>(kernel, argID++, (float) arg);
                } else if (argType == typeof(int)) {
                    result = CL.SetKernelArg<int>(kernel, argID++, (int) arg);
                } else if (argType == typeof(CLBuffer)) {
                    result = CL.SetKernelArg<CLBuffer>(kernel, argID++, (CLBuffer) arg);
                } else {
                    System.Console.WriteLine("Invalid type of kernel argument! Must be float, int or CLBuffer");
                }
                if (result != CLResultCode.Success)
                    System.Console.WriteLine("Kernel argument error: " + result);
            }
            UIntPtr[] workDim = new UIntPtr[] { new UIntPtr( (uint) numWorkUnits) };
            CLEvent @event = new CLEvent();
            CL.EnqueueNDRangeKernel(queue, kernel, 1, null, workDim, null, 0, null, out @event);
        }
        
        
        
    }
}