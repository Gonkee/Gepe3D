
using System;
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
        
        
        public ParticleSystem particleSystem;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            renderer = new Renderer();
            
            particleSystem = new ParticleSystem(5000);

            stopwatch = new Stopwatch();
            stopwatch.Start();

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

            particleSystem.Update(delta);

            cumulativeFrameTime += stopwatch.ElapsedMilliseconds - time;
            tickCount++;
            if (tickCount >= AVG_FRAME_COUNT)
            { 
                // System.Console.WriteLine("avg frame time (" + AVG_FRAME_COUNT + " frames) : " +
                //     ((float) cumulativeFrameTime / tickCount) + " ms");
                tickCount = 0;
                cumulativeFrameTime = 0;
            }
        }
        
        public override void Render()
        {
            activeCam.MouseInput(window.MouseState.Delta);

            renderer.Prepare(this);
            
            skyBox.Render(renderer);
            
            particleSystem.Render(renderer);
            
        }
        
        private void Init()
        {
            Random rand = new Random();
            for (int i = 0; i < particleSystem.ParticleCount; i++) {
                float x = (float) rand.NextDouble() * ParticleSystem.MAX_X;
                float y = (float) rand.NextDouble() * ParticleSystem.MAX_Y * 0.7f;
                float z = (float) rand.NextDouble() * ParticleSystem.MAX_Z * 0.3f + ParticleSystem.MAX_Z * 0.7f;
                particleSystem.SetPos(i, x, y, z);
                particleSystem.SetPhase(i, ParticleSystem.PHASE_LIQUID);
            }
            
            int solidParticleCount = BallGen.GenBall(
                particleSystem,
                ParticleSystem.MAX_X * 0.5f,
                1.1f,
                ParticleSystem.MAX_Z * 0.3f,
                1, 12
            );
                
            
        }
        
    }
}