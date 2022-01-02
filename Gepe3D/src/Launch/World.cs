
using System;
using OpenTK.Mathematics;
using System.Diagnostics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class World : Scene
    {
        

        public Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);

        public SkyBox skyBox;
        
        private int centreParticle;
        
        
        public ParticleSystem particleSystem;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            
            particleSystem = new ParticleSystem(5000);

            stopwatch = new Stopwatch();
            stopwatch.Start();


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
            
            // Vector3 ballPos = particleSystem.GetPos(centreParticle);
            // activeCam.LookAt(ballPos);
            // activeCam.SetPos(ballPos + new Vector3(5f, 4, 0));
            if ( Global.IsKeyDown(Keys.J) )
            {
                
                for (int i = 0; i < 500; i++) {
                    particleSystem.AddVel(i, 0, 0.1f, 0);
                }
                
            }
            
        }
        
        public override void Render()
        {
            activeCam.MouseInput(window.MouseState.Delta);
            
            
            skyBox.Render(this);
            
            particleSystem.Render(this);
            
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
                
                particleSystem.SetColour( i, 0, 0.5f, 1 );
                
            }
            
            centreParticle = BallGen.GenBall(
                particleSystem,
                ParticleSystem.MAX_X * 0.5f,
                1.1f,
                ParticleSystem.MAX_Z * 0.3f,
                1, 12
            );
                
            
        }
        
    }
}