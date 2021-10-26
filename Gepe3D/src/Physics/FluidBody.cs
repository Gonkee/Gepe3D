
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D
{
    public class FluidBody : PhysicsBody
    {
        private readonly ParticleData state;
        private readonly Geometry particleShape;
        private readonly float PARTICLE_RADIUS = 0.2f;

        private readonly float x, y, z;
        private readonly float xLength, yLength, zLength;
        private readonly float xResolution, yResolution, zResolution;
        
        private readonly int _vaoID;
        private readonly int _meshVBO_ID;
        private readonly int _instanceVBO_ID;
        
        private readonly int _fboID;
        
        int postProcessingVAO;
        int texColorBuffer;

        private readonly float[] particlePositions;

        public FluidBody(
            float x, float y, float z,
            float xLength, float yLength, float zLength,
            int xResolution, int yResolution, int zResolution)
        {
            this.x = x;
            this.y = y;
            this.z = z;

            this.xLength = xLength;
            this.yLength = yLength;
            this.zLength = zLength;

            this.xResolution = xResolution;
            this.yResolution = yResolution;
            this.zResolution = zResolution;

            state = new ParticleData(xResolution * yResolution * zResolution);
            particlePositions = new float[xResolution * yResolution * zResolution * 3];

            int pointer = 0;
            float tx, ty, tz;
            for (int px = 0; px < xResolution; px++)
            {
                for (int py = 0; py < yResolution; py++)
                {
                    for (int pz = 0; pz < zResolution; pz++)
                    {
                        tx = MathHelper.Lerp(x, x + xLength, px / (xResolution - 1f) );
                        ty = MathHelper.Lerp(y, y + yLength, py / (yResolution - 1f) );
                        tz = MathHelper.Lerp(z, z + zLength, pz / (zResolution - 1f) );

                        particlePositions[pointer * 3 + 0] = tx;
                        particlePositions[pointer * 3 + 1] = ty;
                        particlePositions[pointer * 3 + 2] = tz;

                        state.SetPos( pointer++, tx, ty, tz );
                    }
                }
            }

            particleShape = GeometryGenerator.GenQuad(PARTICLE_RADIUS, PARTICLE_RADIUS);
            
            _meshVBO_ID = GL.GenBuffer();
            _instanceVBO_ID = GL.GenBuffer();
            _vaoID = GL.GenVertexArray();
            
            _fboID = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fboID);
            
            texColorBuffer = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            // TODO: change the 1600, 900 (resolution) to be flexible
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 1600, 900, 0, PixelFormat.Rgb, PixelType.UnsignedByte, new IntPtr());
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Linear);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, texColorBuffer, 0);
            
            int rbo = GL.GenRenderbuffer();
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rbo);
            // TODO: change the 1600, 900 (resolution) to be flexible
            GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, 1600, 900);
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
            GL.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthStencilAttachment, RenderbufferTarget.Renderbuffer, rbo);
            
            if (GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != FramebufferErrorCode.FramebufferComplete)
                System.Console.WriteLine("error with frame buffer!");
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            // post processing quad
            this.postProcessingVAO = GL.GenVertexArray();
            GL.BindVertexArray(postProcessingVAO);
            
            float[] vd = new float[]
            {
                // 2d coords     // tex coords
                -1, -1,          0, 0,
                 1, -1,          1, 0,
                 1,  1,          1, 1,
                 
                -1, -1,          0, 0,
                 1,  1,          1, 1,
                -1,  1,          0, 1,
            };
            
            int tempVBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, tempVBO);
            GL.BufferData(BufferTarget.ArrayBuffer, vd.Length * sizeof(float), vd, BufferUsageHint.StaticDraw);
            GL.BindVertexArray(postProcessingVAO);
            // vertex positions
            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // vertex normals
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            
            
            float[] vertexData = particleShape.GenerateVertexData();
            GL.BindBuffer(BufferTarget.ArrayBuffer, _meshVBO_ID);
            GL.BufferData(BufferTarget.ArrayBuffer, vertexData.Length * sizeof(float), vertexData, BufferUsageHint.StaticDraw);

            GL.BindVertexArray(_vaoID);
            // vertex positions
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, particleShape.FloatsPerVertex * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);
            // vertex normals
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, particleShape.FloatsPerVertex * sizeof(float), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            // instance positions
            GL.BindBuffer(BufferTarget.ArrayBuffer, _instanceVBO_ID);
            GL.BufferData(BufferTarget.ArrayBuffer, particlePositions.Length * sizeof(float), particlePositions, BufferUsageHint.StreamDraw);
            GL.VertexAttribPointer(2, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
            GL.VertexAttribDivisor(2, 1);
            GL.EnableVertexAttribArray(2);
        }
        
        public override PhysicsData GetState()
        {
            return new PhysicsData(0);
        }

        public override PhysicsData GetDerivative(PhysicsData state)
        {
            return new PhysicsData(0);
        }

        public override void UpdateState(PhysicsData change, List<PhysicsBody> bodies)
        {
        }

        
        public override float MaxX() { return x + xLength; }
        public override float MinX() { return x; }
        public override float MaxY() { return y + yLength; }
        public override float MinY() { return y; }
        public override float MaxZ() { return z + zLength; }
        public override float MinZ() { return z; }

        public override Mesh GetMesh()
        {
            return null;
        }

        public override void Draw()
        {
        }
        
        public override void Render(Renderer renderer)
        {
            Shader shader = renderer.UseShader("point_sphere");
            // shader.SetVector3("lightPos", renderer.LightPos);
            // shader.SetVector3("ambientLight", renderer.AmbientLight);
            // shader.SetVector3("viewPos", renderer.CameraPos);
            // shader.SetMatrix4("cameraMatrix", renderer.CameraMatrix);
            shader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            shader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            shader.SetVector3("lightPos", renderer.LightPos);
            // shader.SetFloat("sphereRadius", PARTICLE_RADIUS);
            
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _fboID);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            
            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF); // enable stencil writing
            
            // draw particles
            GL.BindVertexArray(_vaoID);
            GL.DrawArraysInstanced(PrimitiveType.Triangles, 0, particleShape.TriangleIDs.Count * 3, state.ParticleCount);
            
            
            // TODO: change the 1600, 900 (resolution) to be flexible
            // copy from FBO's stencil buffer to default buffer's stencil buffer
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, _fboID);
            GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
            GL.BlitFramebuffer(0, 0, 1600, 900, 0, 0, 1600, 900, ClearBufferMask.StencilBufferBit, BlitFramebufferFilter.Nearest);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            
            
            GL.StencilFunc(StencilFunction.Equal, 1, 0xFF);
            GL.StencilMask(0x00); // disable stencil writing
            
            renderer.UseShader("post1");
            GL.BindVertexArray(postProcessingVAO);
            GL.Disable(EnableCap.DepthTest);
            GL.BindTexture(TextureTarget.Texture2D, texColorBuffer);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            GL.Enable(EnableCap.DepthTest);
            
            
            GL.StencilFunc(StencilFunction.Always, 1, 0xFF);
            GL.StencilMask(0xFF); // enable stencil writing
        }
    }
}