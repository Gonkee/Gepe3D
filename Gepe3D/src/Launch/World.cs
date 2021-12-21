
using System.Collections.Generic;
using OpenTK.Mathematics;
using System.Diagnostics;

namespace Gepe3D
{
    public class World : Scene
    {
        
        private Shader _entityShader;

        public Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(1f, 0f, 0f);

        public SkyBox skyBox;
        
        private readonly Renderer renderer;
        
        
        public ParticleRenderer prenderer;
        public ParticleSimulator simulator;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            renderer = new Renderer();
            
            simulator = new ParticleSimulator(2000);
            prenderer = new ParticleRenderer(2000, simulator);

            stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //CubeGenerator.AddCube(
            //    simulator,
            //    -0.5f, -0.5f, -0.5f,
            //    1, 1, 1,
            //    10, 10, 10
            //);

            CubeLiquidGenerator.AddCube(
                simulator,
                -1f, -0.5f, -0.5f,
                2, 1, 1,
                20, 10, 10
            );





            //ClothGenerator.AddCloth(
            //    simulator,
            //    -0.5f, 0.5f, -0.5f,
            //    1, 1,
            //    6, 6
            //);

            _entityShader = new Shader("res/Shaders/entity.vert", "res/Shaders/entity.frag");
            _entityShader.Use();
            _entityShader.SetVector3("lightPos", lightPos);

            activeCam.Position = new Vector3( 2.3f, 1f, 2.3f );
            activeCam.LookAt(0, -0.5f, 0);
            
            Init();
        }
        

        public override void Update()
        {
            float delta = 0.01f;
            Global.Elapsed += delta;
            
            activeCam.Update(delta);
            // foreach (PhysicsBody body in _bodies)
            // {
            //     PhysicsSolver.IntegrateExplicitEuler(body, delta, _bodies); // fixed timestep
            
            // }
            


            long time = stopwatch.ElapsedMilliseconds;

            simulator.Update(delta);

            cumulativeFrameTime += stopwatch.ElapsedMilliseconds - time;
            tickCount++;
            if (tickCount >= AVG_FRAME_COUNT)
            { 
                System.Console.WriteLine("avg frame time (" + AVG_FRAME_COUNT + " frames) : " +
                    ((float) cumulativeFrameTime / tickCount) + " ms");
                tickCount = 0;
                cumulativeFrameTime = 0;
            }
            
            // pbd.Update(delta);
        }
        
        public override void Render()
        {
            activeCam.MouseInput(window.MouseState.Delta);

            renderer.Prepare(this);
            renderer.Render(skyBox);
            // foreach (PhysicsBody body in _bodies) renderer.Render(body);


            // _entityShader.Use();
            // _entityShader.SetMatrix4("cameraMatrix", activeCam.GetMatrix());
            // // _entityShader.SetMatrix4("viewMatrix", activeCam.GetViewMatrix());
            // // _entityShader.SetMatrix4("projectionMatrix", activeCam.GetProjectionMatrix());
            // _entityShader.SetVector3("viewPos", activeCam.Position);
            // _entityShader.SetVector3("ambientLight", ambientLight);

            // foreach (PhysicsBody body in _bodies)
            // {
            //     Mesh bodyMesh = body.GetMesh();
            //     if (body is FluidBody)
            //     {
            //         body.Draw();
            //         continue;
            //     }
            //     if (bodyMesh == null) continue;

            //     if (!bodyMesh.Visible) continue;
            //     _entityShader.SetVector3("fillColor", bodyMesh.Material.color);
            //     // drawStyle
            //     // 0 = fill
            //     // 1 = line
            //     // 2 = point

            //     _entityShader.SetInt("drawStyle", 0);
            //     GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            //     body.Draw();

            //     if (bodyMesh.DrawWireframe)
            //     {
            //         _entityShader.SetInt("drawStyle", 1);
            //         GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Line);
            //         GL.Enable(EnableCap.PolygonOffsetLine);
            //         GL.PolygonOffset(-1, -1);
            //         GL.LineWidth(4f);
            //         body.Draw();
            //         GL.Disable(EnableCap.PolygonOffsetLine);
                    
            //         _entityShader.SetInt("drawStyle", 2);
            //         GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Point);
            //         GL.Enable(EnableCap.PolygonOffsetPoint);
            //         GL.PolygonOffset(-2, -2);
            //         GL.PointSize(10f);
            //         body.Draw();
            //         GL.Disable(EnableCap.PolygonOffsetPoint);
            //     }
            // }
            
            prenderer.Render(renderer);
            
        }
        
        private void Init()
        {
            
                
        }
        
    }
}