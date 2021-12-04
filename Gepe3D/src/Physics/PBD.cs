
using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Graphics.OpenGL4;

namespace Gepe3D
{
    public class PBD
    {
        
        private readonly float PARTICLE_RADIUS = 0.1f;
        private readonly Geometry particleShape;
        
        private readonly int _vaoID;
        private readonly int _meshVBO_ID;
        private readonly int _instanceVBO_ID;
        
        private readonly PBD_data data;
        
        public PBD()
        {
            float x = -0.5f;
            float y = -0.5f;
            float z = -0.5f;
            float xLength = 1f;
            float yLength = 1f;
            float zLength = 1f;
            int xResolution = 6;
            int yResolution = 6;
            int zResolution = 6;
            
            data = new PBD_data(xResolution * yResolution * zResolution);
            
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

                        data.SetPos(pointer++, tx, ty, tz);
                    }
                }
            }
            
            
            particleShape = GeometryGenerator.GenQuad(PARTICLE_RADIUS, PARTICLE_RADIUS);
            
            float[] vertexData = particleShape.GenerateVertexData();
            _vaoID = GLUtils.GenVAO();
            _meshVBO_ID = GLUtils.GenVBO(vertexData);
            _instanceVBO_ID = GLUtils.GenVBO( data.GetPositionBuffer() );

            
            
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 0, 3, particleShape.FloatsPerVertex, 0); // vertex positions
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 1, 3, particleShape.FloatsPerVertex, 0); // vertex normals
            GLUtils.VaoInstanceFloatAttrib(_vaoID, _instanceVBO_ID, 2, 3, 3, 0);
        }
        
        public void Render(Renderer renderer)
        {
            
            GLUtils.ReplaceBufferData(_instanceVBO_ID, data.GetPositionBuffer());
            
            
            Shader shader = renderer.UseShader("point_sphere_basic");
            shader.SetVector3("lightPos", renderer.LightPos);
            shader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            shader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            shader.SetFloat("particleRadius", PARTICLE_RADIUS);
            
            GLUtils.DrawInstancedVAO(_vaoID, particleShape.TriangleIDs.Count * 3, data.ParticleCount);
            
            
        }
        
        float elapsed = 0;
        
        public void Update(float delta)
        {
            elapsed += delta;
            for (int i = 0; i < data.ParticleCount; i++)
            {
                data.AddPos(i, MathF.Sin(elapsed) * delta, 0, 0);
            }
        }
        
    }
}