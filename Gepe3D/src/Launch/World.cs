
using System;
using OpenTK.Mathematics;
using System.Diagnostics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class World : Scene
    {
        


        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);

        public SkyBox skyBox;
        
        
        public BallCharacter character;
        
        public Spike[] spikes;
        
        
        public ParticleSystem particleSystem;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            
            particleSystem = new ParticleSystem(7000);
            
            Random rand = new Random();
            for (int i = 0; i < particleSystem.ParticleCount; i++) {
                float x = (float) rand.NextDouble() * ParticleSystem.MAX_X;
                float y = (float) rand.NextDouble() * ParticleSystem.MAX_Y * 0.7f;
                float z = (float) rand.NextDouble() * ParticleSystem.MAX_Z * 0.3f + ParticleSystem.MAX_Z * 0.7f;
                particleSystem.SetPos(i, x, y, z);
                particleSystem.SetPhase(i, ParticleSystem.PHASE_LIQUID);
                
                particleSystem.SetColour( i, 0, 0.5f, 1 );
            }
            
            character = new BallCharacter(
                particleSystem,
                ParticleSystem.MAX_X * 0.5f,
                1.1f,
                ParticleSystem.MAX_Z * 0.3f,
                1, 12
            );
            
            spikes = new Spike[3];
            
            float radius = 1f;
            float x1 = radius - (ParticleSystem.MAX_X + radius * 2) * 1.333f;
            float x2 = radius - (ParticleSystem.MAX_X + radius * 2) * 1.667f;
            float x3 = radius - (ParticleSystem.MAX_X + radius * 2) * 2.000f;
            spikes[0] = new Spike(particleSystem, x1, Spike.RandZ(radius), 2f, radius, 800);
            spikes[1] = new Spike(particleSystem, x2, Spike.RandZ(radius), 2f, radius, 2000);
            spikes[2] = new Spike(particleSystem, x3, Spike.RandZ(radius), 2f, radius, 3000);
            

            stopwatch = new Stopwatch();
            stopwatch.Start();

            
            Init();
        }
        

        public override void Update()
        {
            float delta = 0.01f;
            Global.Elapsed += delta;
            
            character.Update(delta);
            foreach (Spike s in spikes) s.Update();
            
            float shiftX = 4.5f * delta;
            
            
            long time = stopwatch.ElapsedMilliseconds;

            particleSystem.Update(delta, shiftX);

            cumulativeFrameTime += stopwatch.ElapsedMilliseconds - time;
            
            tickCount++;
            if (tickCount >= AVG_FRAME_COUNT)
            { 
                // System.Console.WriteLine("avg frame time (" + AVG_FRAME_COUNT + " frames) : " +
                //     ((float) cumulativeFrameTime / tickCount) + " ms");
                tickCount = 0;
                cumulativeFrameTime = 0;
            }
            
            if ( Global.IsKeyDown(Keys.J) )
            {
                
                for (int i = 0; i < 500; i++) {
                    particleSystem.AddVel(i, 0, 0.1f, 0);
                }
                
            }
            
        }
        
        public override void Render()
        {
            
            character.MouseMovementUpdate(window.MouseState.Delta);
            
            
            skyBox.Render(this);
            
            particleSystem.Render(this);
            
        }
        
        private void Init()
        {
            
        }
        
        
        
        
    }
}