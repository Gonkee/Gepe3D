

using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Gepe3D
{
    public class Renderer
    {
        public Camera Camera;
        public Vector3 LightPos { get; private set; }
        public Vector3 AmbientLight { get; private set; }
        public Vector3 CameraPos { get; private set; }
        public Matrix4 CameraMatrix { get; private set; }
        
        private readonly Dictionary<string, Shader> shaders = new Dictionary<string, Shader>();
        
        public Renderer()
        {
            shaders.Add(
                "skybox",
                new Shader("res/Shaders/skybox.vert", "res/Shaders/skybox.frag")
            );
            
            shaders.Add(
                "entity",
                new Shader("res/Shaders/entity.vert", "res/Shaders/entity.frag")
            );
            
            shaders.Add(
                "point_sphere",
                new Shader("res/Shaders/point_sphere.vert", "res/Shaders/point_sphere.frag")
            );
            
        }
        
        public void Prepare(World world)
        {
            LightPos = world.lightPos;
            AmbientLight = world.ambientLight;
            CameraPos = world.activeCam.Position;
            CameraMatrix = world.activeCam.GetMatrix();
            
            Camera = world.activeCam;
        }
    
        
        public void Render(Renderable renderable)
        {
            renderable.Render(this);
        }
        
        public Shader UseShader(string shaderName)
        {
            shaders[shaderName].Use();
            return shaders[shaderName];
        }
        
        
    }
}