
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
        private readonly float PARTICLE_RADIUS;
        
        // particle data
        private readonly float particleMass;
        private readonly float[] pDensities;
        private readonly float[] pPressures;

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


        float maxEffectDistance;
        float poly6coeff;
        float spikyGradCoeff;
        float viscosityLaplacianCoeff;
        float viscosityConstant;
        float maxAccel;

        public FluidBody(
            float x, float y, float z,
            float xLength, float yLength, float zLength,
            int xResolution, int yResolution, int zResolution,
            float particleRadius)
        {
            
            maxEffectDistance = 0.5f;
            poly6coeff = 315 / (64 * MathF.PI * MathF.Pow(maxEffectDistance, 9) );
            spikyGradCoeff =         -45 / (MathF.PI * MathF.Pow(maxEffectDistance, 6) );
            viscosityLaplacianCoeff = 15 / (2 * MathF.PI * MathF.Pow(maxEffectDistance, 3) );
            viscosityConstant = 30.0f;
            maxAccel = 10f;
            particleMass = 0.3f;
            
            this.PARTICLE_RADIUS = particleRadius;
            
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
            
            pDensities = new float[xResolution * yResolution * zResolution];
            pPressures = new float[xResolution * yResolution * zResolution];

            int pointer = 0;
            float tx, ty, tz;
            for (int px = 0; px < xResolution; px++)
            {
                for (int py = 0; py < yResolution; py++)
                {
                    for (int pz = 0; pz < zResolution; pz++)
                    {
                        tx = MathHelper.Lerp(x, x + xLength, px / (xResolution - 1f) ) + py * 0.1f;
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
        
        
        
        private float WeightPoly6(float dist)
        {
            
            dist = MathHelper.Clamp(dist, 0, maxEffectDistance);
            
            float diff = (maxEffectDistance * maxEffectDistance - dist * dist);
            return poly6coeff * diff * diff * diff;
        }
        
        private float WeightSpikyGrad(float dist)
        {
            dist = MathHelper.Clamp(dist, 0, maxEffectDistance);
            
            // jank af clean up later
            float diff = (maxEffectDistance - dist);
            return spikyGradCoeff * diff * diff;
        }
        
        private float WeightViscosity(float dist)
        {
            
            dist = MathHelper.Clamp(dist, 0, maxEffectDistance);
            
            float t1 = (- dist * dist * dist) / (2 * maxEffectDistance * maxEffectDistance * maxEffectDistance);
            float t2 = (dist * dist) / (maxEffectDistance * maxEffectDistance);
            float t3 = maxEffectDistance / (2 * dist);
            
            return viscosityLaplacianCoeff * (t1 + t2 + t3 - 1);
        }
        
        
        public override PhysicsData GetState()
        {
            return state;
        }

        int count = 0;

        public override PhysicsData GetDerivative(PhysicsData state)
        {
            ParticleData pstate = new ParticleData(state);
            ParticleData derivative = new ParticleData(pstate.ParticleCount);
            
            
            // update fluid density and pressure
            for (int i = 0; i < pstate.ParticleCount; i++)
            {
                Vector3 p1 = pstate.GetPos(i);
                float density = 0;
                
                for (int j = 0; j < pstate.ParticleCount; j++)
                {
                    
                    Vector3 p2 = pstate.GetPos(j);
                    
                    float dist = (p2 - p1).Length;
                    
                    if (dist > maxEffectDistance) continue;
                    
                    float weight = WeightPoly6(dist);
                    
                    density += particleMass * weight;
                    
                    
                }
                
                
                
                // pressure = k * (p - p0) where k is gas constant and p0 is environmental pressure
                // trial and error values i guess
                float pressure = 10f * (density - 5) ;
                pressure = MathF.Max(pressure, 0);
                
                pDensities[i] = density;
                pPressures[i] = pressure;
                
                // count++;
                // if (count % 30 == 0)
                // System.Console.WriteLine(density + ", " + pressure);
                
                
            }
            
            
            
            
            
            
            // update fluid acceleration
            for (int i = 0; i < pstate.ParticleCount; i++)
            {
                Vector3 p1 = pstate.GetPos(i);
                
                Vector3 acceleration = new Vector3();
                
                for (int j = 0; j < pstate.ParticleCount; j++)
                {
                    
                    
                    Vector3 p2 = pstate.GetPos(j);
                    
                    Vector3 diff = p1 - p2;
                    float dist = diff.Length;
                    Vector3 dir = diff.Normalized();
                    
                    if (dist > maxEffectDistance) continue;
                    
                    if (dist == 0) continue;
                    
                    
                    
                    
                    // supposed to multiply by a mass ratio that is m2/m1, but all masses are the same
                    float weight = WeightSpikyGrad(dist);
                    float pressureTerm = ( pPressures[i] + pPressures[j] ) / ( 2 * pDensities[i] * pDensities[j] );
                    acceleration -= dir * weight * pressureTerm;
                    
                    
                    // viscosity
                    // supposed to multiply by a mass ratio that is m2/m1, but all masses are the same
                    dist = MathHelper.Clamp(dist, 0, maxEffectDistance);
                    
                    // dunno which one to use
                    float lap = MathF.Max(viscosityLaplacianCoeff * (maxEffectDistance - dist), 0 );
                    lap = MathF.Max( WeightViscosity(dist), 0 );
                    
                    Vector3 vDiff = pstate.GetVel(j) - pstate.GetVel(i);
                    acceleration += ( viscosityConstant / pDensities[j] ) * lap * vDiff;
                    
                    
                }
                
                acceleration += new Vector3(0, -9.8f, 0);
                
                
                // damping
                Vector3 damp = pstate.GetVel(i) * 0.1f;
                if (damp.Length < acceleration.Length) acceleration -= damp;
                else acceleration = new Vector3();
                
                
                if (acceleration.Length > maxAccel) {
                    acceleration = acceleration.Normalized() * maxAccel;
                }
                
                
                derivative.SetVel(i, acceleration);
                derivative.SetPos(i, pstate.GetVel(i));
                
                
                
            }
            
            
            
            
            
            
            return derivative;
        }

        public override void UpdateState(PhysicsData pchange, List<PhysicsBody> bodies)
        {
            
            ParticleData change = new ParticleData(pchange);
            
            for (int i = 0; i < state.DataLength; i++)
            {
                state.Set(i, state.Get(i) + change.Get(i));
            }
            
            float BOUNDING_RADIUS = 1.2f;
            
            for (int i = 0; i < state.ParticleCount; i++)
            {
                Vector3 pos = state.GetPos(i);
                Vector3 vel = state.GetVel(i);
                
                // basic bounding box constraint collision
                
                if (pos.X < -BOUNDING_RADIUS)
                {
                    pos.X = -BOUNDING_RADIUS;
                    vel.X = MathF.Max(0, vel.X);
                }
                if (pos.X > BOUNDING_RADIUS)
                {
                    pos.X = BOUNDING_RADIUS;
                    vel.X = MathF.Min(0, vel.X);
                }
                
                
                if (pos.Y < -BOUNDING_RADIUS)
                {
                    pos.Y = -BOUNDING_RADIUS;
                    vel.Y = MathF.Max(0, vel.Y);
                }
                if (pos.Y > BOUNDING_RADIUS)
                {
                    pos.Y = BOUNDING_RADIUS;
                    vel.Y = MathF.Min(0, vel.Y);
                }
                
                
                if (pos.Z < -BOUNDING_RADIUS)
                {
                    pos.Z = -BOUNDING_RADIUS;
                    vel.Z = MathF.Max(0, vel.Z);
                }
                if (pos.Z > BOUNDING_RADIUS)
                {
                    pos.Z = BOUNDING_RADIUS;
                    vel.Z = MathF.Min(0, vel.Z);
                }
                
                state.SetPos(i, pos);
                // System.Console.WriteLine(pos);
                state.SetVel(i, vel);
                
                
                particlePositions[i * 3 + 0] = pos.X;
                particlePositions[i * 3 + 1] = pos.Y;
                particlePositions[i * 3 + 2] = pos.Z;
            }
            
            
            
            
            
            
            
            
            
            
            
            
            GLUtils.ReplaceBufferData(_instanceVBO_ID, particlePositions);
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
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            Shader p2 = renderer.UseShader("bilateral_filter");
            
            p2.SetFloat("screenWidth", 1600);
            p2.SetFloat("screenHeight", 900);
            // p2.SetFloat("particleRadius", PARTICLE_RADIUS);
            p2.SetBool("blurXaxis", true);
            GLUtils.DrawPostProcessing(texColorBuffer1, postProcessingVAO);
            
            
            
            GLUtils.BindFBO(fbo1);
            
            // p2.SetFloat("particleRadius", PARTICLE_RADIUS);
            p2.SetBool("blurXaxis", false);
            GLUtils.DrawPostProcessing(texColorBuffer2, postProcessingVAO);
            
            
            GLUtils.BindFBO(fbo2);
            
            renderer.UseShader("depth_normal").SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            GLUtils.DrawPostProcessing(texColorBuffer1, postProcessingVAO);
            
            GLUtils.BindFBO(0);
            
            Shader fluidShader = renderer.UseShader("fluid_shading");
            fluidShader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            fluidShader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            fluidShader.SetVector3("lightPos", renderer.LightPos);
            fluidShader.SetVector3("ambientLight", renderer.AmbientLight);
            fluidShader.SetVector3("fillColor", new Vector3(1, 0.8f, 0));
            GLUtils.DrawPostProcessing(texColorBuffer2, postProcessingVAO);
            
            GLUtils.StencilWriteMode();
        }
    }
}