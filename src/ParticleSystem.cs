
using System;
using System.Collections.Generic;
using OpenTK.Compute.OpenCL;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class ParticleSystem
    {
        
        // Render
        private readonly int quad_VAO;
        private readonly int quad_VBO;
        private readonly int instancePositions_VBO;
        private readonly int instanceColours_VBO;
        private readonly Shader particleShader;
        
        // Update
        CLCommandQueue queue;
        CLEvent @event = new CLEvent();
        
        public readonly int ParticleCount;
        private readonly int cellCount;
        private readonly float[] posData;
        private readonly float[] ePosData;
        private readonly float[] velData;
        private readonly float[] colourData;
        private readonly int[]   phaseData;
        
        public static Vector3 GRAVITY          = new Vector3(-3, -6, 0);
        public static float   PARTICLE_RADIUS  = 0.2f;
        public static float   GRID_CELL_WIDTH  = 0.6f;
        public static float   KERNEL_SIZE      = 0.6f;
        public static float   REST_DENSITY     = 80f;
        
        public static int
            GridRowsX = 16,
            GridRowsY = 10,
            GridRowsZ = 12;
            
        public static float
            MAX_X = GRID_CELL_WIDTH * GridRowsX,
            MAX_Y = GRID_CELL_WIDTH * GridRowsY,
            MAX_Z = GRID_CELL_WIDTH * GridRowsZ;
        
        public static int
            PHASE_LIQUID = 0,
            PHASE_SOLID = 1,
            PHASE_STATIC = 2;
        
        
        private readonly CLKernel
            k_PredictPos,
            k_CalcLambdas,
            k_FluidCorrect,
            k_CorrectPredictions,
            k_UpdateVel,
            k_CalcVorticity,
            k_ApplyVortVisc,
            k_CorrectVel,
            k_AssignParticleCells,
            k_FindCellsStartAndEnd,
            k_SortParticleIDsByCell,
            k_SolidCorrect;
        
        private readonly CLBuffer
            b_Pos,              // positions
            b_Vel,              // velocities
            b_ePos,             // estimated positions
            b_imass,            // inverse masses
            b_lambdas,          // fluid correction scalar
            b_phase,            // particle phase
            b_Vorticities,      // fluid vorticities
            b_PosCorrection,    // position correction
            b_VelCorrection,    // velocity correction
            
            // buffers for neighbour search
            b_sortedParticleIDs,
            b_cellStartAndEndIDs,
            b_cellIDsOfParticles,
            b_numParticlesPerCell,
            b_particleIDinCell;
        
        
        private bool
            posDirty = false,
            velDirty = false,
            phaseDirty = false,
            colourDirty = false;
    
        List<(int, int, float)> distanceConstraints = new List<(int, int, float)>();
        
        
        
        public ParticleSystem(int particleCount)
        {
            this.ParticleCount = particleCount;
            this.cellCount = GridRowsX * GridRowsY * GridRowsZ;
            posData    = new float[particleCount * 3];
            velData    = new float[particleCount * 3];
            colourData = new float[particleCount * 3];
            ePosData   = new float[particleCount * 3];
            phaseData  = new int  [particleCount];
            
            
            ///////////////////
            // Set up OpenCL //
            ///////////////////
            
            // set up context & queue
            CLResultCode result;
            CLPlatform[] platforms;
            CLDevice[] devices;
            result = CL.GetPlatformIds(out platforms);
            result = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
            if (result == CLResultCode.DeviceNotFound) {
                CL.GetDeviceIds(platforms[0], DeviceType.All, out devices);
                System.Console.WriteLine("Failed to initiate OpenCL using GPU, cannot use hardware acceleration");
            }
            CLContext context = CL.CreateContext(new IntPtr(), 1, devices, new IntPtr(), new IntPtr(), out result);
            this.queue = CL.CreateCommandQueueWithProperties(context, devices[0], new IntPtr(), out result);
            
            // load kernels
            string varDefines            = GenerateDefines(); // combine with other source strings to add common functions
            string commonFuncSource      = CLUtils.LoadSource("res/Kernels/common_funcs.cl");
            string pbdCommonSource       = CLUtils.LoadSource("res/Kernels/pbd_common.cl");
            string fluidProjectSource    = CLUtils.LoadSource("res/Kernels/fluid_project.cl");
            string solidProjectSource    = CLUtils.LoadSource("res/Kernels/solid_project.cl");
            CLProgram pbdProgram         = CLUtils.BuildClProgram(context, devices, varDefines + commonFuncSource + pbdCommonSource   );
            CLProgram fluidProgram       = CLUtils.BuildClProgram(context, devices, varDefines + commonFuncSource + fluidProjectSource);
            CLProgram solidProgram       = CLUtils.BuildClProgram(context, devices, varDefines + commonFuncSource + solidProjectSource);
            
            this.k_AssignParticleCells    = CL.CreateKernel( pbdProgram   , "assign_particle_cells"      , out result);
            this.k_FindCellsStartAndEnd   = CL.CreateKernel( pbdProgram   , "find_cells_start_and_end"   , out result);
            this.k_SortParticleIDsByCell  = CL.CreateKernel( pbdProgram   , "sort_particle_ids_by_cell"  , out result);
            this.k_PredictPos             = CL.CreateKernel( pbdProgram   , "predict_positions"          , out result);
            this.k_CorrectPredictions     = CL.CreateKernel( pbdProgram   , "correct_predictions"        , out result);
            this.k_UpdateVel              = CL.CreateKernel( pbdProgram   , "update_velocity"            , out result);
            this.k_CalcLambdas            = CL.CreateKernel( fluidProgram , "calculate_lambdas"          , out result);
            this.k_FluidCorrect           = CL.CreateKernel( fluidProgram , "calc_fluid_corrections"     , out result);
            this.k_CalcVorticity          = CL.CreateKernel( fluidProgram , "calculate_vorticities"      , out result);
            this.k_ApplyVortVisc          = CL.CreateKernel( fluidProgram , "apply_vorticity_viscosity"  , out result);
            this.k_CorrectVel             = CL.CreateKernel( fluidProgram , "correct_fluid_vel"          , out result);
            this.k_SolidCorrect           = CL.CreateKernel( solidProgram , "calc_solid_corrections"     , out result);
            
            // create buffers
            this.b_Pos                 = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_Vel                 = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_ePos                = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_imass               = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount      , 1);
            this.b_lambdas             = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount      , 0);
            this.b_PosCorrection       = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_Vorticities         = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_VelCorrection       = CLUtils.EnqueueMakeFloatBuffer(context, queue,  particleCount * 3  , 0);
            this.b_phase               = CLUtils.EnqueueMakeIntBuffer  (context, queue,  particleCount      , 0);
            this.b_sortedParticleIDs   = CLUtils.EnqueueMakeIntBuffer  (context, queue,  particleCount      , 0);
            this.b_cellIDsOfParticles  = CLUtils.EnqueueMakeIntBuffer  (context, queue,  particleCount      , 0);
            this.b_particleIDinCell    = CLUtils.EnqueueMakeIntBuffer  (context, queue,  particleCount      , 0);
            this.b_cellStartAndEndIDs  = CLUtils.EnqueueMakeIntBuffer  (context, queue,  cellCount * 2      , 0);
            this.b_numParticlesPerCell = CLUtils.EnqueueMakeIntBuffer  (context, queue,  cellCount          , 0);
            
            // ensure fills are completed
            CL.Flush(queue);
            CL.Finish(queue);
            
            
            /////////////////////////////////
            // Set up OpenGL for rendering //
            /////////////////////////////////
            
            particleShader = new Shader("res/Shaders/point_sphere.vert", "res/Shaders/point_sphere.frag");
            
            float[] vertexData = new float[]
            {
                //  X value                    Y value          Z value
                -PARTICLE_RADIUS / 2,    -PARTICLE_RADIUS / 2,     0,
                 PARTICLE_RADIUS / 2,    -PARTICLE_RADIUS / 2,     0,  // triangle 1
                 PARTICLE_RADIUS / 2,     PARTICLE_RADIUS / 2,     0,
                
                -PARTICLE_RADIUS / 2,    -PARTICLE_RADIUS / 2,     0,
                 PARTICLE_RADIUS / 2,     PARTICLE_RADIUS / 2,     0,  // triangle 2
                -PARTICLE_RADIUS / 2,     PARTICLE_RADIUS / 2,     0,
            };
            
            quad_VAO              = GLUtils.GenVAO();
            quad_VBO              = GLUtils.GenVBO(vertexData);
            instancePositions_VBO = GLUtils.GenVBO( posData );
            instanceColours_VBO   = GLUtils.GenVBO( colourData );
            GLUtils.VaoFloatAttrib        (quad_VAO, quad_VBO             , 0, 3, 3, 0); // vertex positions
            GLUtils.VaoInstanceFloatAttrib(quad_VAO, instancePositions_VBO, 1, 3, 3, 0);
            GLUtils.VaoInstanceFloatAttrib(quad_VAO, instanceColours_VBO  , 2, 3, 3, 0);
        }
        
        // set pos, phase, colour, constraints, 
        
        public void SetPos(int id, float x, float y, float z)
        {
            posData[id * 3 + 0] = x;
            posData[id * 3 + 1] = y;
            posData[id * 3 + 2] = z;
            posDirty = true;
        }
        
        public void AddPos(int id, float x, float y, float z)
        {
            posData[id * 3 + 0] += x;
            posData[id * 3 + 1] += y;
            posData[id * 3 + 2] += z;
            posDirty = true;
        }
        
        public Vector3 GetPos(int id)
        {
            return new Vector3( posData[id * 3 + 0], posData[id * 3 + 1], posData[id * 3 + 2] );
        }
        
        public void SetVel(int id, float x, float y, float z)
        {
            velData[id * 3 + 0] = x;
            velData[id * 3 + 1] = y;
            velData[id * 3 + 2] = z;
            velDirty = true;
        }
        
        public void AddVel(int id, float x, float y, float z)
        {
            velData[id * 3 + 0] += x;
            velData[id * 3 + 1] += y;
            velData[id * 3 + 2] += z;
            velDirty = true;
        }
        
        public Vector3 GetVel(int id)
        {
            return new Vector3( velData[id * 3 + 0], velData[id * 3 + 1], velData[id * 3 + 2] );
        }
        
        public void SetPhase(int id, int phase)
        {
            phaseData[id] = phase;
            phaseDirty = true;
        }
        
        public void SetColour(int id, float r, float g, float b)
        {
            colourData[id * 3 + 0] = r;
            colourData[id * 3 + 1] = g;
            colourData[id * 3 + 2] = b;
            colourDirty = true;
        }
        
        public void AddDistConstraint(int id1, int id2, float dist)
        {
            distanceConstraints.Add( (id1, id2, dist) );
        }
        
        
        private string GenerateDefines()
        {
            string defines = "";
            
            defines += "\n" + "#define MAX_X " + MAX_X.ToString("0.0000") + "f";
            defines += "\n" + "#define MAX_Y " + MAX_Y.ToString("0.0000") + "f";
            defines += "\n" + "#define MAX_Z " + MAX_Z.ToString("0.0000") + "f";
            defines += "\n" + "#define CELLCOUNT_X " + GridRowsX;
            defines += "\n" + "#define CELLCOUNT_Y " + GridRowsY;
            defines += "\n" + "#define CELLCOUNT_Z " + GridRowsZ;
            defines += "\n" + "#define CELL_WIDTH " + GRID_CELL_WIDTH.ToString("0.0000") + "f";
            defines += "\n" + "#define KERNEL_SIZE " + KERNEL_SIZE.ToString("0.0000") + "f";
            defines += "\n" + "#define REST_DENSITY " + REST_DENSITY.ToString("0.0000") + "f";
            defines += "\n" + "#define PHASE_LIQUID " + PHASE_LIQUID;
            defines += "\n" + "#define PHASE_SOLID " + PHASE_SOLID;
            defines += "\n" + "#define PHASE_STATIC " + PHASE_STATIC;
            defines += "\n";
            
            return defines;
        }
        
        
        public void Render(MainWindow world)
        {
            if (colourDirty) {
                GLUtils.ReplaceBufferData(instanceColours_VBO, colourData );
                colourDirty = false;
            }
            GLUtils.ReplaceBufferData(instancePositions_VBO, posData );
            
            particleShader.Use();
            particleShader.SetVector3("lightPos", world.lightPos);
            particleShader.SetMatrix4("viewMatrix", world.character.activeCam.GetViewMatrix());
            particleShader.SetMatrix4("projectionMatrix", world.character.activeCam.GetProjectionMatrix());
            particleShader.SetFloat("particleRadius", PARTICLE_RADIUS);
            particleShader.SetFloat("maxX", MAX_X);
            
            GLUtils.DrawInstancedVAO(quad_VAO, 6, ParticleCount);
        }
        
        
        public void Update(float delta, float shiftX)
        {
            if (posDirty) {
                CL.EnqueueWriteBuffer<float>(queue, b_Pos, false, new UIntPtr(), posData, null, out @event);
                posDirty = false;
            }
            
            if (velDirty) {
                CL.EnqueueWriteBuffer<float>(queue, b_Vel, false, new UIntPtr(), velData, null, out @event);
                velDirty = false;
            }
            
            if (phaseDirty) {
                CL.EnqueueWriteBuffer<int>(queue, b_phase, false, new UIntPtr(), phaseData, null, out @event);
                phaseDirty = false;
            }
            
            CLUtils.EnqueueFillIntBuffer(queue, b_sortedParticleIDs, 0, ParticleCount);
            CLUtils.EnqueueFillIntBuffer(queue, b_numParticlesPerCell, 0, cellCount);
            CLUtils.EnqueueFillFloatBuffer(queue, b_PosCorrection, 0, ParticleCount * 3);
            
            // predict particle positions, then sort particle IDs for neighbour finding accordingly
            CLUtils.EnqueueKernel(queue, k_PredictPos             , ParticleCount  , delta, b_Pos, b_Vel, b_ePos, b_phase, GRAVITY.X, GRAVITY.Y, GRAVITY.Z);
            CLUtils.EnqueueKernel(queue, k_AssignParticleCells     , ParticleCount  , b_ePos, b_numParticlesPerCell, b_cellIDsOfParticles, b_particleIDinCell);
            CLUtils.EnqueueKernel(queue, k_FindCellsStartAndEnd   , cellCount      , b_numParticlesPerCell, b_cellStartAndEndIDs);
            CLUtils.EnqueueKernel(queue, k_SortParticleIDsByCell  , ParticleCount  , b_particleIDinCell, b_cellStartAndEndIDs, b_cellIDsOfParticles, b_sortedParticleIDs);
            
            // correct the predicted positions to satisfy fluid and contact constraints
            CLUtils.EnqueueKernel(queue, k_CalcLambdas            , ParticleCount  , b_ePos, b_imass, b_lambdas, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
            CLUtils.EnqueueKernel(queue, k_FluidCorrect           , ParticleCount  , b_ePos, b_imass, b_lambdas, b_PosCorrection, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
            CLUtils.EnqueueKernel(queue, k_SolidCorrect           , ParticleCount  , b_ePos, b_imass, b_PosCorrection, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
            CLUtils.EnqueueKernel(queue, k_CorrectPredictions     , ParticleCount  , b_Pos, b_ePos, b_PosCorrection, b_phase);
            
            CpuSolveDistConstraints(0.2f, 2); // parameters: stiffness, iterations
            
            // update particle velocities using corrected predictions, then correct fluid velocities for vorticity & viscosity
            CLUtils.EnqueueKernel(queue, k_UpdateVel              , ParticleCount  , delta, b_Pos, b_Vel, b_ePos, b_phase, shiftX);
            CLUtils.EnqueueKernel(queue, k_CalcVorticity          , ParticleCount  , b_Pos, b_Vel, b_Vorticities, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
            CLUtils.EnqueueKernel(queue, k_ApplyVortVisc          , ParticleCount  , b_Pos, b_Vel, b_Vorticities, b_VelCorrection, b_imass, delta, b_cellIDsOfParticles, b_cellStartAndEndIDs, b_sortedParticleIDs, b_phase);
            CLUtils.EnqueueKernel(queue, k_CorrectVel             , ParticleCount  , b_Vel, b_VelCorrection);
            
            // read position and velocity data from GPU
            CL.EnqueueReadBuffer<float>(queue, b_Pos, false, new UIntPtr(), posData, null, out @event);
            CL.EnqueueReadBuffer<float>(queue, b_Vel, false, new UIntPtr(), velData, null, out @event);
            
            CL.Flush(queue);
            CL.Finish(queue);
            
        }
        
        // distant constraint projection is performed on CPU because i can't figure out how to do it in OpenCL
        private void CpuSolveDistConstraints(float stiffness, int iterations)
        {
            stiffness = 1 - MathF.Pow( 1 - stiffness, 1f / (float) iterations );
            
            // read estimated positions from the GPU
            CL.EnqueueReadBuffer<float>(queue, b_ePos, false, new UIntPtr(), ePosData, null, out @event);
            CL.Flush(queue);
            CL.Finish(queue);
            
            for (int i = 0; i < iterations; i++)
            {
                foreach( (int, int, float) constraint in distanceConstraints )
                {
                    int p1 = constraint.Item1;
                    int p2 = constraint.Item2;
                    float restDist = constraint.Item3;
                    float imass1 = 1;
                    float imass2 = 1;
                    if (imass1 == 0 && imass2 == 0) return;

                    Vector3 epos1 = new Vector3(ePosData[p1 * 3 + 0], ePosData[p1 * 3 + 1], ePosData[p1 * 3 + 2]);
                    Vector3 epos2 = new Vector3(ePosData[p2 * 3 + 0], ePosData[p2 * 3 + 1], ePosData[p2 * 3 + 2]);

                    Vector3 dir = epos1 - epos2;
                    float displacement = dir.Length - restDist;
                    dir.Normalize();

                    float w1 = imass1 / (imass1 + imass2);
                    float w2 = imass2 / (imass1 + imass2);

                    Vector3 correction1 = -w1 * displacement * dir;
                    Vector3 correction2 = +w2 * displacement * dir;
                    
                    ePosData[p1 * 3 + 0] += correction1.X * stiffness;
                    ePosData[p1 * 3 + 1] += correction1.Y * stiffness;
                    ePosData[p1 * 3 + 2] += correction1.Z * stiffness;
                    
                    ePosData[p2 * 3 + 0] += correction2.X * stiffness;
                    ePosData[p2 * 3 + 1] += correction2.Y * stiffness;
                    ePosData[p2 * 3 + 2] += correction2.Z * stiffness;
                }
            }
            
            // write estimated positions back to the GPU
            CL.EnqueueWriteBuffer<float>(queue, b_ePos, false, new UIntPtr(), ePosData, null, out @event);
            CL.Flush(queue);
            CL.Finish(queue);
        }
        
    }
}