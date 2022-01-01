
using System;
using OpenTK.Compute.OpenCL;
using System.Linq;
using System.IO;

namespace Gepe3D
{
    public class CLUtils
    {
        
        
        public static string LoadSource(string filePath)
        {
            filePath = Path.Combine(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory), filePath);
            return File.ReadAllText(filePath);
        }
        
        public static CLProgram BuildClProgram(CLContext context, CLDevice[] devices, string source)
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
        
        public static void EnqueueKernel(CLCommandQueue queue, CLKernel kernel, int numWorkUnits, params object[] args)
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
        
        public static CLBuffer EnqueueMakeIntBuffer(CLContext context, CLCommandQueue queue, int numEntries, int defaultValue)
        {
            CLResultCode result;
            CLEvent @event;
            UIntPtr bufferSize = new UIntPtr( (uint) numEntries * sizeof(int) );
            int[] fillArray = new int[] {defaultValue};
            CLBuffer buffer = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize, new IntPtr(), out result);
            CL.EnqueueFillBuffer<int>(queue, buffer, fillArray, new UIntPtr(), bufferSize, null, out @event);
            return buffer;
        }
        
        public static CLBuffer EnqueueMakeFloatBuffer(CLContext context, CLCommandQueue queue, int numEntries, float defaultValue)
        {
            CLResultCode result;
            CLEvent @event;
            UIntPtr bufferSize = new UIntPtr( (uint) numEntries * sizeof(float) );
            float[] fillArray = new float[] {defaultValue};
            CLBuffer buffer = CL.CreateBuffer(context, MemoryFlags.ReadWrite, bufferSize, new IntPtr(), out result);
            CL.EnqueueFillBuffer<float>(queue, buffer, fillArray, new UIntPtr(), bufferSize, null, out @event);
            return buffer;
        }
        
        public static void EnqueueFillIntBuffer(CLCommandQueue queue, CLBuffer buffer, int fillValue, int numEntries)
        {
            CLEvent @event;
            UIntPtr bufferSize = new UIntPtr( (uint) numEntries * sizeof(int) );
            int[] fillArray = new int[] {fillValue};
            CL.EnqueueFillBuffer<int>(queue, buffer, fillArray, new UIntPtr(), bufferSize, null, out @event);
        }
        
        public static void EnqueueFillFloatBuffer(CLCommandQueue queue, CLBuffer buffer, float fillValue, int numEntries)
        {
            CLEvent @event;
            UIntPtr bufferSize = new UIntPtr( (uint) numEntries * sizeof(float) );
            float[] fillArray = new float[] {fillValue};
            CL.EnqueueFillBuffer<float>(queue, buffer, fillArray, new UIntPtr(), bufferSize, null, out @event);
        }
        
        
    }
}