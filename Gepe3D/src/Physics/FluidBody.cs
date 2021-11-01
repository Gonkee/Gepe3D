
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
        
        private readonly int fbo1;
        int texColorBuffer1;
        private readonly int fbo2;
        int texColorBuffer2;
        
        int postProcessingVAO;

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
            
            float[] vertexData = particleShape.GenerateVertexData();
            _vaoID = GLUtils.GenVAO();
            _meshVBO_ID = GLUtils.GenVBO(vertexData);
            _instanceVBO_ID = GLUtils.GenVBO(particlePositions);
            
            fbo1 = GLUtils.GenFBO();
            texColorBuffer1 = GLUtils.FboAddTexture(fbo1, 1600, 900);
            GLUtils.FboAddDepthStencilRBO(fbo1, 1600, 900);
            
            fbo2 = GLUtils.GenFBO();
            texColorBuffer2 = GLUtils.FboAddTexture(fbo2, 1600, 900);
            GLUtils.FboAddDepthStencilRBO(fbo2, 1600, 900);
            
            GLUtils.BindFBO(0);
            
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
            
            int tempVBO = GLUtils.GenVBO(vd);
            this.postProcessingVAO = GLUtils.GenVAO();
            GLUtils.VaoFloatAttrib(postProcessingVAO, tempVBO, 0, 2, 4, 0); // vertex positions
            GLUtils.VaoFloatAttrib(postProcessingVAO, tempVBO, 1, 2, 4, 2); // texture coordinates
            
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 0, 3, particleShape.FloatsPerVertex, 0); // vertex positions
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 1, 3, particleShape.FloatsPerVertex, 0); // vertex normals
            GLUtils.VaoInstanceFloatAttrib(_vaoID, _instanceVBO_ID, 2, 3, 3, 0);
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
            shader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            shader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            shader.SetFloat("particleRadius", PARTICLE_RADIUS);
            
            
            GLUtils.BindFBO(fbo1);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            
            GL.Enable(EnableCap.StencilTest);
            GLUtils.StencilWriteMode();
            GLUtils.DrawInstancedVAO(_vaoID, particleShape.TriangleIDs.Count * 3, state.ParticleCount);
            GLUtils.StencilReadMode();
            
            GLUtils.CopyStencilBuffer(fbo1, fbo2, 1600, 900);
            GLUtils.CopyStencilBuffer(fbo1, 0, 1600, 900);
            
            
            GLUtils.BindFBO(fbo2);
            
            Shader p2 = renderer.UseShader("bilateral_filter");
            
            p2.SetFloat("particleRadius", PARTICLE_RADIUS);
            p2.SetBool("blurXaxis", true);
            GLUtils.DrawPostProcessing(texColorBuffer1, postProcessingVAO);
            
            
            
            GLUtils.BindFBO(fbo1);
            
            p2.SetFloat("particleRadius", PARTICLE_RADIUS);
            p2.SetBool("blurXaxis", false);
            GLUtils.DrawPostProcessing(texColorBuffer2, postProcessingVAO);
            
            
            GLUtils.BindFBO(0);
            
            renderer.UseShader("depth_normal").SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            GLUtils.DrawPostProcessing(texColorBuffer1, postProcessingVAO);
            
            // GLUtils.BindFBO(0);
            
            // renderer.UseShader("post3");
            // GLUtils.DrawPostProcessing(texColorBuffer1, postProcessingVAO);
            
            GLUtils.StencilWriteMode();
        }
    }
}