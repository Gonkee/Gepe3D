
using System;
using OpenTK.Mathematics;
using System.Diagnostics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Gepe3D
{
    public class World : Scene
    {
        

        // public Camera activeCam = new Camera( new Vector3(), 16f / 9f);

        public Vector3 ambientLight = new Vector3(0.2f, 0.2f, 0.2f);
        public Vector3 lightPos = new Vector3(0f, 10f, 0f);

        public SkyBox skyBox;
        
        private int centreParticle;
        
        public BallCharacter character;
        
        
        public ParticleSystem particleSystem;
        
        int tickCount = 0;
        long cumulativeFrameTime = 0;
        readonly int AVG_FRAME_COUNT = 20;
        Stopwatch stopwatch;
        
        public World(MainWindow window) : base(window)
        {
            skyBox = new SkyBox();
            
            particleSystem = new ParticleSystem(5000);
            
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
            
            GenSpike(3, 3, 2f, 1f, 800);

            stopwatch = new Stopwatch();
            stopwatch.Start();


            // activeCam.Position = new Vector3( -2, 2f, 0 );
            // activeCam.LookAt(1, 1, 1);
            
            Init();
        }
        

        public override void Update()
        {
            float delta = 0.01f;
            Global.Elapsed += delta;
            
            // activeCam.Update(delta);
            character.Update(delta);
            
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
            // activeCam.MouseInput(window.MouseState.Delta);
            
            character.MouseMovementUpdate(window.MouseState.Delta);
            
            
            skyBox.Render(this);
            
            particleSystem.Render(this);
            
        }
        
        private void Init()
        {
            
        }
        
        
        private void GenSpike(float x, float z, float height, float radius, int startID)
        {
            
            float gap = 0.15f;
            int xzRes = (int) (radius * 2 / gap);
            int  yRes = (int) (height / gap);
            
            
            int currentID = startID;
            
            for (int py = 0; py < yRes; py++)
            {
                for (int px = 0; px < xzRes; px++)
                {
                    for (int pz = 0; pz < xzRes; pz++)
                    {
                        float offsetY = MathHelper.Lerp( 0, height, py / (yRes - 1f) );
                        float offsetX = MathHelper.Lerp( -radius, +radius, px / (xzRes - 1f) );
                        float offsetZ = MathHelper.Lerp( -radius, +radius, pz / (xzRes - 1f) );
                        float horDist = MathF.Sqrt(offsetX * offsetX + offsetZ * offsetZ);
                        
                        if (horDist <= MathHelper.Lerp(radius, 0, offsetY / height)) {
                            
                            particleSystem.SetPhase(currentID, ParticleSystem.PHASE_STATIC);
                            particleSystem.SetColour(currentID, 0.2f, 0.2f, 0.2f);
                            
                            particleSystem.SetPos(
                                currentID,
                                x + offsetX,
                                offsetY,
                                z + offsetZ
                            );
                            
                            currentID++;
                        }
                    }
                }
            }
        }
        
        
    }
}