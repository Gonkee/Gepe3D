
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
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);

        public SkyBox skyBox;
        
        private readonly Renderer renderer;
        
        
        // private HardwareParticles hparticles;
        
        public ParticleRenderer prenderer;
        // public ParticleSimulator simulator;
        public HParticleSimulator hsimulator;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            renderer = new Renderer();
            
            hsimulator = new HParticleSimulator(1000);
            prenderer = new ParticleRenderer(1000, hsimulator);
            
            // hparticles = new HardwareParticles(7, 7, 7);

            stopwatch = new Stopwatch();
            stopwatch.Start();
            
            //CubeGenerator.AddCube(
            //    simulator,
            //    -0.5f, -0.5f, -0.5f,
            //    1, 1, 1,
            //    10, 10, 10
            //);

            // CubeLiquidGenerator.AddCube(
            //     simulator,
            //     0, 0, 0,
            //     1, 1, 1,
            //     10, 10, 10
            // );


            // long x = (11 << 32);
            // long y = (13 << 16);
            // long z = (-6 <<  0);
            // long cellID = (x << 32) + (y << 16) + (z);
            // System.Console.WriteLine(cellID);



            //ClothGenerator.AddCloth(
            //    simulator,
            //    -0.5f, 0.5f, -0.5f,
            //    1, 1,
            //    6, 6
            //);

            _entityShader = new Shader("res/Shaders/entity.vert", "res/Shaders/entity.frag");
            _entityShader.Use();
            _entityShader.SetVector3("lightPos", lightPos);

            activeCam.Position = new Vector3( -2, 2f, 0 );
            activeCam.LookAt(1, 1, 1);
            
            Init();
        }
        

        public override void Update()
        {
            float delta = 0.01f;
            Global.Elapsed += delta;
            
            activeCam.Update(delta);
            


            long time = stopwatch.ElapsedMilliseconds;

            hsimulator.Update(delta);
            // hparticles.Update(delta);

            cumulativeFrameTime += stopwatch.ElapsedMilliseconds - time;
            tickCount++;
            if (tickCount >= AVG_FRAME_COUNT)
            { 
                System.Console.WriteLine("avg frame time (" + AVG_FRAME_COUNT + " frames) : " +
                    ((float) cumulativeFrameTime / tickCount) + " ms");
                tickCount = 0;
                cumulativeFrameTime = 0;
            }
        }
        
        public override void Render()
        {
            activeCam.MouseInput(window.MouseState.Delta);

            renderer.Prepare(this);
            renderer.Render(skyBox);
            
            prenderer.Render(renderer);
            // hparticles.Render(renderer);
            
        }
        
        private void Init()
        {
        }
        
    }
}