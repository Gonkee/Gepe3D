
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
        
        public static void VaoInstanceFloatAttrib(int vaoID, int vboID, int attribID, int attribSize, int floatsPerVertex, int startOffset)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vboID);
            GL.BindVertexArray(vaoID);
            GL.VertexAttribPointer(attribID, attribSize, VertexAttribPointerType.Float, false, floatsPerVertex * sizeof(float), startOffset * sizeof(float));
            GL.VertexAttribDivisor(attribID, 1);
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
        
        public static int GenFBO()
        {
            return GL.GenFramebuffer();
        }
        
        public static void BindFBO(int fboID)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboID);
        }
        
        public static int FboAddTexture(int fboID, int width, int height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboID);
            
            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, new IntPtr());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.ClampToEdge);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texture, 0);
            
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                throw new Exception("Frame Buffer Error!");
            
            return texture;
        }
        
        public static int FboAddDepthStencilRBO(int fboID, int width, int height)
        {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboID);
            
            int rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, width, height);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);
            
            return rbo;
        }
        
        public static void CopyStencilBuffer(int fromFBO, int toFBO, int width, int height)
        {
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fromFBO);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, toFBO);
            GL.BlitFramebuffer(0, 0, width, height, 0, 0, width, height, ClearBufferMask.StencilBufferBit, BlitFramebufferFilter.Nearest);
        }
        
        public static void DrawPostProcessing(int screenTexture, int quadVAO)
        {
            GL.Disable(EnableCap.DepthTest);
            
            GL.BindVertexArray(quadVAO);
            GL.BindTexture(TextureTarget.Texture2D, screenTexture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            
            GL.Enable(EnableCap.DepthTest);
        }
        
        public static void StencilWriteMode()
        {
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF);
        }
        
        public static void StencilReadMode()
        {
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilMask(0x00);
        }
        
        
        
        
    }
}