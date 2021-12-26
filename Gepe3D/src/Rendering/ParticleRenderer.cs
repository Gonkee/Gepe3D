

namespace Gepe3D
{
    public class ParticleRenderer
    {
        
        private readonly float PARTICLE_RADIUS = 0.15f;
        private readonly Geometry particleShape;
        
        private readonly int _vaoID;
        private readonly int _meshVBO_ID;
        private readonly int _instanceVBO_ID;
        
        private readonly int maxParticles;
        // private readonly float[] particlePositions;
        
        private readonly HParticleSimulator simulator;
        
        public ParticleRenderer(int maxParticles, HParticleSimulator simulator)
        {
            this.maxParticles = maxParticles;
            // particlePositions = new float[maxParticles * 3];
            
            this.simulator = simulator;
            
            
            particleShape = GeometryGenerator.GenQuad(PARTICLE_RADIUS, PARTICLE_RADIUS);
            
            float[] vertexData = particleShape.GenerateVertexData();
            _vaoID = GLUtils.GenVAO();
            _meshVBO_ID = GLUtils.GenVBO(vertexData);
            _instanceVBO_ID = GLUtils.GenVBO( simulator.PosData );

            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 0, 3, particleShape.FloatsPerVertex, 0); // vertex positions
            GLUtils.VaoFloatAttrib(_vaoID, _meshVBO_ID, 1, 3, particleShape.FloatsPerVertex, 0); // vertex normals
            GLUtils.VaoInstanceFloatAttrib(_vaoID, _instanceVBO_ID, 2, 3, 3, 0);
        }
        
        public void Render(Renderer renderer)
        {
            GLUtils.ReplaceBufferData(_instanceVBO_ID, simulator.PosData );
            
            Shader shader = renderer.UseShader("point_sphere_basic");
            shader.SetVector3("lightPos", renderer.LightPos);
            shader.SetMatrix4("viewMatrix", renderer.Camera.GetViewMatrix());
            shader.SetMatrix4("projectionMatrix", renderer.Camera.GetProjectionMatrix());
            shader.SetFloat("particleRadius", PARTICLE_RADIUS);
            
            GLUtils.DrawInstancedVAO(_vaoID, particleShape.TriangleIDs.Count * 3, simulator.ParticleCount);
            
            
        }
        
        
        
    }
}