
using OpenTK.Graphics.OpenGL4;
using System;

namespace Gepe3D
{
    public class GLUtils
    {

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

        public static void VaoFloatAttrib(int vaoID, int vboID, int attribID, int attribSize, int floatsPerVertex, int startOffset)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BindVertexArray(vaoID);
            GL.VertexAttribPointer(attribID, attribSize, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), startOffset * sizeof(float));
            GL.EnableVertexAttribArray(attribID);
        }

        public static void VaoInstanceFloatAttrib(int vaoID, int vboID, int attribID, int attribSize, int floatsPerVertex, int startOffset)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BindVertexArray(vaoID);
            GL.VertexAttribPointer(attribID, attribSize, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), startOffset * sizeof(float));
            GL.VertexAttribDivisor(attribID, 1);
            GL.EnableVertexAttribArray(attribID);
        }

    }
}
