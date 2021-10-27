
using OpenTK.Graphics.OpenGL4;
using System;

namespace Gepe3D
{
    public class GLUtils
    {
        
        public static int GenVAO()
        {
            return GL.GenVertexArray();
        }
        
        public static int GenVBO(float[] data)
        {
            int vboID = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferData(BufferTarget.ArrayBuffer, data.Length * sizeof(float), data, BufferUsageHint.StaticDraw);
            return vboID;
        }
        
        public static void ReplaceBufferData(int vboID, float[] data)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BufferSubData<float>(BufferTarget.ArrayBuffer, new IntPtr(0), data.Length * sizeof(float), data);
        }
        
        public static int AttachEBO(int vaoID, uint[] indices)
        {
            int eboID = GL.GenBuffer();
            GL.BindVertexArray(vaoID);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboID);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, BufferUsageHint.StaticDraw);
            return eboID;
        }
        
        
        public static void VaoFloatAttrib(int vaoID, int vboID, int attribID, int attribSize, int floatsPerVertex, int startOffset)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BindVertexArray(vaoID);
            GL.VertexAttribPointer(attribID, attribSize, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), startOffset * sizeof(float));
            GL.EnableVertexAttribArray(attribID);
        }
        
        public static void DrawVAO(int vaoID, int vertexCount)
        {
            GL.BindVertexArray(vaoID);
            GL.DrawArrays(PrimitiveType.Triangles, 0, vertexCount);
        }
        
        public static void DrawIndexedVAO(int vaoID, int indexCount)
        {
            GL.BindVertexArray(vaoID);
            GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedInt, 0);
        }
        
        public static void DrawInstancedVAO(int vaoID, int vertexCount, int instanceCount)
        {
            GL.BindVertexArray(vaoID);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, vertexCount, instanceCount);
        }
        
        
        
    }
}