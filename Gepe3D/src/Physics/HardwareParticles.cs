
using System;
using System.IO;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Graphics.Wgl;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;


namespace Gepe3D
{
    public class HardwareParticles
    {
        
        private readonly float PARTICLE_RADIUS = 0.15f;
        private readonly Geometry particleShape;
        
        private readonly int _vaoID;
        private readonly int _meshVBO_ID;
        private readonly int _instanceVBO_ID;
        
        private readonly int maxParticles;
        
        
        CLCommandQueue queue;
        CLKernel kernel;
        CLBuffer instanceBufferCL;
        
        UIntPtr[] workDimensions;
        
        float[] posData;
        
        
        public HardwareParticles(int xRes, int yRes, int zRes)
        {
            this.maxParticles = xRes * yRes * zRes;
            
            posData = GetPosData(xRes, yRes, zRes);
            
            particleShape = GeometryGenerator.GenQuad(PARTICLE_RADIUS, PARTICLE_RADIUS);
            
            float[] vertexData = particleShape.GenerateVertexData();
            _vaoID = GLUtils.GenVAO();
            _meshVBO_ID = GLUtils.GenVBO(vertexData);
            _instanceVBO_ID = GLUtils.GenVBO( posData );

            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 0, 3, particleShape.FloatsPerVertex, 0); // vertex positions
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 1, 3, particleShape.FloatsPerVertex, 0); // vertex normals
            GLUtils.VaoInstanceFloatAttrib(_vaoID, _instanceVBO_ID, 2, 3, 3, 0);
            
            
            SetUpCL(_instanceVBO_ID);
        }
        
        
        private float[] GetPosData(int xRes, int yRes, int zRes)
        {
            float x = -0.5f;
            float y = -0.5f;
            float z = -0.5f;
            float xLength = 1f;
            float yLength = 1f;
            float zLength = 1f;
            
            float[] posData = new float[xRes * yRes * zRes * 3];
            
            int pointer = 0;
            for (int px = 0; px < xRes; px++)
            {
                for (int py = 0; py < yRes; py++)
                {
                    for (int pz = 0; pz < zRes; pz++)
                    {
                        posData[pointer * 3 + 0] = MathHelper.Lerp(x, x + xLength, px / (xRes - 1f) );
                        posData[pointer * 3 + 1] = MathHelper.Lerp(y, y + yLength, py / (yRes - 1f) );
                        posData[pointer * 3 + 2] = MathHelper.Lerp(z, z + zLength, pz / (zRes - 1f) );
                        pointer++;
                    }
                }
            }
            return posData;
        }
        
        
        public void Update(float delta)
        {
            
            CLEvent @event = new CLEvent();
            CL.SetKernelArg<CLBuffer>(kernel, 0, instanceBufferCL);
            
            CL.EnqueueNDRangeKernel(queue, kernel, 1, null, workDimensions, null, 0, null, out @event);
            // commandQueue, kernel, 1, null, new UIntPtr[] {globalWorkSize}, null, 0, null, out @event
            
            
            
            CL.EnqueueReadBuffer<float>(queue, instanceBufferCL, false, new UIntPtr(), posData, null, out @event);
            
            CL.Flush(queue);
            CL.Finish(queue);
            
            
        }
        
        public void Render(Renderer renderer)
        {
            GLUtils.ReplaceBufferData(_instanceVBO_ID, posData );
            
            Shader shader = renderer.UseShader("point_sphere_basic");
            shader.SetVector3("lightPos", renderer.LightPos);
            shader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            shader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            shader.SetFloat("particleRadius", PARTICLE_RADIUS);
            
            GLUtils.DrawInstancedVAO(_vaoID, particleShape.TriangleIDs.Count * 3, maxParticles);
            
            
        }
        
        
        private void SetUpCL(int instanceVBO)
        {
            
            CLResultCode result;
            
            CLPlatform[] platforms;
            CLDevice[] devices;
            result = CL.GetPlatformIds(out platforms);
            result = CL.GetDeviceIds(platforms[0], DeviceType.Gpu, out devices);
            
            CLContext context = CL.CreateContext(new IntPtr(), 1, devices, new IntPtr(), new IntPtr(), out result);
            this.queue = CL.CreateCommandQueueWithProperties(context, devices[0], new IntPtr(), out result);
            
            string kernelPath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), "res/Kernels/kernel.cl");
            string kernelSource = File.ReadAllText(kernelPath);
            CLProgram program = CL.CreateProgramWithSource(context, kernelSource, out result);
            
            System.Console.WriteLine(result);
            
            result = CL.BuildProgram(program, 1, devices, null, new IntPtr(), new IntPtr());
            
            
            System.Console.WriteLine(result);
            if (result != CLResultCode.Success) {
                byte[] logParam;
                CL.GetProgramBuildInfo(program, devices[0], ProgramBuildInfo.Log, out logParam);
                string error = System.Text.ASCIIEncoding.Default.GetString(logParam);
                System.Console.WriteLine(error);
            }
            
            this.kernel = CL.CreateKernel(program, "move_particles", out result);
            
            System.Console.WriteLine(result);
            
            UIntPtr bufferSize = new UIntPtr( (uint) maxParticles * 3 * sizeof(float) );
            
            this.instanceBufferCL = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize, new IntPtr(), out result);
            
            CLEvent @event = new CLEvent();
            CL.EnqueueWriteBuffer<float>(queue, instanceBufferCL, false, new UIntPtr(), posData, null, out @event);
            
            
            
            workDimensions = new UIntPtr[] { new UIntPtr( (uint) maxParticles) };
            
            
        }
        
    }
}